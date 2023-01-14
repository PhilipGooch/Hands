using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NBG.SuperCombiner
{
    internal class SuperCombinerEditorWindow : EditorWindow
    {
        private enum State
        {
            Start,
            SearchComplete,
            AnalyzingMaterial
        }

        private State state = State.Start;
        private IOrderedEnumerable<KeyValuePair<Material, int>> orderedMaterials;
        private IOrderedEnumerable<KeyValuePair<Mesh, int>> orderedByMesh;
        private IOrderedEnumerable<KeyValuePair<Transform, int>> orderedByParent;

        private GUIStyle red, green, yellow;

        private Material analyzedMaterial;

        private bool showInstancedOptions, showSiblingsMerging;

        [MenuItem("No Brakes Games/Mesh Batching Optimizer (Super Combiner)...")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<SuperCombinerEditorWindow>("Mesh Batching Optimizer (Super Combiner)");

            window.Show();
            window.minSize = new Vector2(400, 200);
        }

        private void OnEnable()
        {
            red = new GUIStyle();
            red.normal.textColor = Color.red;

            green = new GUIStyle();
            green.normal.textColor = Color.green;

            yellow = new GUIStyle();
            yellow.normal.textColor = Color.yellow;
        }

        private void OnGUI()
        {
            if (state == State.Start)
            {
                if (GUILayout.Button("Search"))
                    Search();
            }
            else if (state == State.SearchComplete)
            {
                if (GUILayout.Button("Refresh"))
                    Search();

                {
                    if (orderedMaterials == null)
                    {
                        state = State.Start;
                        return;
                    }

                    if (GUILayout.Button("Solve everything"))
                        SolveEverything();
                    if (GUILayout.Button("Revert everything"))
                        RevertEverything();

                    GUILayout.BeginVertical();
                    foreach (var x in orderedMaterials)
                    {

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Solve", GUILayout.Width(200.0f)))
                            Analyze(x.Key);


                        GUILayout.Label(x.Key.name, GUILayout.Width(200.0f));

                        int val = x.Value;
                        if (val > 100)
                            GUILayout.Label("" + val, red, GUILayout.Width(200.0f));
                        else if (val > 30)
                            GUILayout.Label("" + val, yellow, GUILayout.Width(200.0f));
                        else
                            GUILayout.Label("" + val, green, GUILayout.Width(200.0f));

                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            else
            {

                if (GUILayout.Button("Back"))
                    Search();

                if (orderedMaterials == null)
                {
                    state = State.Start;
                    return;
                }

                GUILayout.Label(analyzedMaterial.name);

                GUILayout.BeginVertical();

                showInstancedOptions = EditorGUILayout.Foldout(showInstancedOptions, "GPU instancing solutions");

                if (showInstancedOptions)
                {
                    if (GUILayout.Button("Create and apply instancing material"))
                        CreateInstancedMaterialAndApply();


                    foreach (var x in orderedByMesh)
                    {

                        GUILayout.BeginHorizontal();

                        GUILayout.Label(x.Key.name, GUILayout.Width(200.0f));

                        int val = x.Value;
                        if (val > 100)
                            GUILayout.Label("" + val, red, GUILayout.Width(200.0f));
                        else if (val > 30)
                            GUILayout.Label("" + val, yellow, GUILayout.Width(200.0f));
                        else
                            GUILayout.Label("" + val, green, GUILayout.Width(200.0f));

                        GUILayout.EndHorizontal();
                    }
                }

                showSiblingsMerging = EditorGUILayout.Foldout(showSiblingsMerging, "Siblings merging");

                if (showSiblingsMerging)
                {
                    foreach (var x in orderedByParent)
                    {

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Select", GUILayout.Width(100.0f)))
                            Selection.activeGameObject = x.Key.gameObject;
                        if (GUILayout.Button("Combine", GUILayout.Width(100.0f)))
                        {
                            Selection.activeGameObject = x.Key.gameObject;
                            if (Combine(x.Key.gameObject))
                                Analyze(analyzedMaterial);
                        }

                        GUILayout.Label(x.Key.name, GUILayout.Width(200.0f));

                        int val = x.Value;
                        if (val > 100)
                            GUILayout.Label("" + val, red, GUILayout.Width(200.0f));
                        else if (val > 30)
                            GUILayout.Label("" + val, yellow, GUILayout.Width(200.0f));
                        else
                            GUILayout.Label("" + val, green, GUILayout.Width(200.0f));

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();

            }
        }

        private void Search()
        {

            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

            Dictionary<Material, int> materialCounts = new Dictionary<Material, int>();

            for (int i = 0; i < renderers.Length; i++)
                if (SuperCombinerEditor.IsStaticMeshRendererOptimizable(renderers[i]))
                {
                    Material mat = renderers[i].sharedMaterial;
                    if (materialCounts.ContainsKey(mat))
                        materialCounts[mat]++;
                    else
                        materialCounts.Add(mat, 1);
                }


            orderedMaterials = materialCounts.OrderByDescending(x => x.Value);

            state = State.SearchComplete;
        }

        private void Analyze(Material material)
        {
            //Analyze instancing
            analyzedMaterial = material;
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

            Dictionary<Mesh, int> meshCounts = new Dictionary<Mesh, int>();

            if (analyzedMaterial.enableInstancing == false)
                for (int i = 0; i < renderers.Length; i++)
                {
                    Transform t = renderers[i].transform;
                    Material mat = renderers[i].sharedMaterial;
                    bool isSingleMaterial = renderers[i].sharedMaterials.Length == 1;

                    if (isSingleMaterial && renderers[i].enabled && mat == analyzedMaterial && t.gameObject.activeInHierarchy && !t.gameObject.isStatic)
                    {
                        Mesh sharedMesh = t.GetComponent<MeshFilter>().sharedMesh;
                        if (meshCounts.ContainsKey(sharedMesh))
                            meshCounts[sharedMesh]++;
                        else
                            meshCounts.Add(sharedMesh, 1);
                    }
                }

            orderedByMesh = meshCounts.OrderByDescending(x => x.Value);

            //Classify per parent
            Dictionary<Transform, int> byParent = new Dictionary<Transform, int>();

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                if (SuperCombinerEditor.IsStaticMeshRendererOptimizable(mr))
                {
                    if (mr.sharedMaterial == analyzedMaterial)
                    {
                        Transform parent = mr.transform.parent;
                        if (byParent.ContainsKey(parent))
                            byParent[parent]++;
                        else
                            byParent.Add(parent, 1);
                    }
                }
            }            

            orderedByParent = byParent.Where(x => x.Value != 1).OrderByDescending(x => x.Value);

            state = State.AnalyzingMaterial;
        }



        private bool Combine(GameObject gameObject)
        {
            SuperCombiner combiner = gameObject.GetComponent<SuperCombiner>();
            if (combiner == null)
                combiner = gameObject.AddComponent<SuperCombiner>();

            return SuperCombinerEditor.SearchAndMerge(combiner);
        }

        private void CreateInstancedMaterialAndApply()
        {
            string materialPath = AssetDatabase.GetAssetPath(analyzedMaterial);
            if (materialPath.StartsWith("Packages/"))
                return;

            string copyPath = Path.GetDirectoryName(materialPath);
            string fileName = Path.GetFileNameWithoutExtension(materialPath);
            copyPath = Path.Combine(copyPath,fileName + "_InstancedCopy.mat");

            if (AssetDatabase.LoadAssetAtPath<Material>(copyPath) == null)
                AssetDatabase.CopyAsset(materialPath, copyPath);

            Material instancedMaterial = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
            instancedMaterial.enableInstancing = true;

            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                Transform t = renderers[i].transform;
                Material mat = renderers[i].sharedMaterial;



                if (renderers[i].enabled && mat == analyzedMaterial && t.gameObject.activeInHierarchy && !t.gameObject.isStatic)
                {
                    if (mat.enableInstancing)
                        continue;
                    renderers[i].sharedMaterial = instancedMaterial;
                }
            }

            Analyze(analyzedMaterial);

            Debug.Log("New instanced version of the material generated at : " + copyPath);
        }

        private void SolveEverything()
        {
            foreach (var x in orderedMaterials)
            {
                GUILayout.BeginHorizontal();
                Analyze(x.Key);
                CreateInstancedMaterialAndApply();
                foreach (var y in orderedByParent)
                {
                    Combine(y.Key.gameObject);
                }
            }
        }

        private void RevertEverything()
        {
            foreach (var x in FindObjectsOfType<SuperCombiner>())
            {
                SuperCombinerEditor.RevertAndDestroy(x);
            }
        }
    }
}
