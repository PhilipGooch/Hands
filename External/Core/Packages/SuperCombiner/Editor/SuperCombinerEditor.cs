using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.SuperCombiner
{
    [CustomEditor(typeof(NBG.SuperCombiner.SuperCombiner))]
    internal class SuperCombinerEditor : Editor
    {
        private const int MAX_VERTEX_COUNT = 65536;
        public const string mergeRootName = "MergeRoot(Generated)";

        private Dictionary<Material, List<GameObject>> objectsContainer;
        private bool searchDone = false;

        private bool showInternalData = false;

        public override void OnInspectorGUI()
        {
            showInternalData = EditorGUILayout.Foldout(showInternalData, "InternalData");
            if (showInternalData)
            {
                GUI.enabled = false;
                base.OnInspectorGUI();
                GUI.enabled = true;
            }
            SuperCombiner combiner = (SuperCombiner)target;
            Transform transform = combiner.transform;

            if (combiner.applied)
            {
                if (GUILayout.Button("Revert"))
                {
                    Revert(combiner, transform);
                    combiner.applied = false;
                    serializedObject.Update(); // Update UI representation
                }

                if (GUILayout.Button("Apply lightmap index"))
                {
                    combiner.ApplyLightmapIndices();
                    serializedObject.Update(); // Update UI representation
                }
            }
            else
            {
                if (GUILayout.Button("Search"))
                {
                    if (objectsContainer == null)
                        objectsContainer = new Dictionary<Material, List<GameObject>>();
                    else
                        objectsContainer.Clear();

                    Search(transform, objectsContainer);
                    searchDone = true;
                    serializedObject.Update(); // Update UI representation
                }

                if (searchDone)
                {
                    if (GUILayout.Button("Apply merge"))
                    {
                        if (!ApplyMerge(transform, objectsContainer, combiner))
                            Destroy(combiner);
                        serializedObject.Update(); // Update UI representation
                    }

                    foreach (var pair in objectsContainer)
                    {
                        GUILayout.Label("Material = " + pair.Key.name + "[" + pair.Value.Count + "]");
                    }
                }
            }
        }

        public static bool SearchAndMerge(SuperCombiner superCombiner)
        {
            Transform transform = superCombiner.transform;
            var objectsContainer = new Dictionary<Material, List<GameObject>>();
            Search(transform, objectsContainer, true);
            return ApplyMerge(transform, objectsContainer, superCombiner);
        }

        private static void Search(Transform transform, Dictionary<Material, List<GameObject>> objectsContainer, bool onlySiblings = false, int depth = 0)
        {

            depth += 1;

            MeshRenderer mr = transform.GetComponent<MeshRenderer>();
            if (IsStaticMeshRendererOptimizable(mr))
            {
                if (!objectsContainer.TryGetValue(mr.sharedMaterial, out List<GameObject> list))
                {
                    list = new List<GameObject>();
                    objectsContainer[mr.sharedMaterial] = list;
                }

                list.Add(transform.gameObject);
            }

            if (onlySiblings && depth > 1)
            {
                return;
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Search(transform.GetChild(i), objectsContainer, onlySiblings, depth);
                }
            }
        }

        public static bool IsStaticMeshRendererOptimizable(MeshRenderer mr)
        {
            if (mr == null)
                return false;

            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf.sharedMesh == null)
                return false;

            Transform t = mr.transform;
            Transform parent = t.parent;
            Material mat = mr.sharedMaterial;

            bool isSingleMaterial = mr.sharedMaterials.Length == 1;
            bool isMergeRoot = t.name == SuperCombinerEditor.mergeRootName;


            return isSingleMaterial && !isMergeRoot && parent != null && mr.enabled && t.gameObject.activeInHierarchy && t.gameObject.isStatic;
        }

        private static bool ApplyMerge(Transform parent, Dictionary<Material, List<GameObject>> objectsContainer, SuperCombiner combiner)
        {
            bool canMergeIn16Bits = CanMergeWithInt16(objectsContainer);

            Transform mergeRoot = FindOrCreateMergeRoot(parent);
            combiner.root = mergeRoot;

            for (int i = 0; i < mergeRoot.childCount; i++)
            {
                DestroyImmediate(mergeRoot.GetChild(i).gameObject);
            }

            if (combiner.mergedObjects == null)
                combiner.mergedObjects = new List<SuperCombiner.RevertRendererData>();
            else
                combiner.mergedObjects.Clear();


            foreach (var pair in objectsContainer)
            {
                List<GameObject> combinedMeshesList = pair.Value;

                GameObject newGameObject = new GameObject(pair.Key.name + " Merged");
                SetStaticFlags(newGameObject);
                newGameObject.transform.parent = mergeRoot;
                newGameObject.transform.position = combinedMeshesList[0].transform.position;
                newGameObject.transform.rotation = combinedMeshesList[0].transform.rotation;
                newGameObject.transform.localScale = Vector3.one;

                MeshRenderer newMeshRenderer = newGameObject.AddComponent<MeshRenderer>();
                newMeshRenderer.material = pair.Key;
                newMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;


                CombineInstance[] instances = new CombineInstance[combinedMeshesList.Count];

                Mesh combinedMesh = new Mesh();
                combinedMesh.indexFormat = canMergeIn16Bits ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;


                Matrix4x4 worldToLocal = newGameObject.transform.worldToLocalMatrix;


                for (int i = 0; i < combinedMeshesList.Count; i++)
                {
                    MeshRenderer elementMeshRenderer = combinedMeshesList[i].GetComponent<MeshRenderer>();
                    MeshFilter elementMeshFilter = combinedMeshesList[i].GetComponent<MeshFilter>();
                    MeshCollider elementMeshCollider = combinedMeshesList[i].GetComponent<MeshCollider>();

                    instances[i].mesh = elementMeshFilter.sharedMesh;
                    instances[i].lightmapScaleOffset = elementMeshRenderer.lightmapScaleOffset;
                    instances[i].transform = worldToLocal * combinedMeshesList[i].transform.localToWorldMatrix;
                    elementMeshRenderer.enabled = false;

                    SuperCombiner.RevertRendererData revertData = CreateRevert(elementMeshFilter);
                    combiner.mergedObjects.Add(revertData);
                }

                combinedMesh.CombineMeshes(instances, true, true, true);

                AssetDatabase.CreateAsset(combinedMesh, GetMeshPath(parent.name));

                newGameObject.AddComponent<MeshFilter>().mesh = combinedMesh;

                newMeshRenderer.lightmapIndex = combinedMeshesList[0].GetComponent<MeshRenderer>().lightmapIndex;
                combiner.lighmapIndices.Add(newMeshRenderer.lightmapIndex);

                EditorUtility.SetDirty(newMeshRenderer);

                combiner.applied = true;
            }

            return true;
        }

        private static bool CanMergeWithInt16(Dictionary<Material, List<GameObject>> objectsContainer)
        {
            foreach (var pair in objectsContainer)
            {
                List<GameObject> combinedMeshesList = pair.Value;

                int vertexCount = 0;

                for (int i = 0; i < combinedMeshesList.Count; i++)
                {
                    vertexCount += combinedMeshesList[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                }

                if (vertexCount >= MAX_VERTEX_COUNT)
                {
                    Debug.LogWarning("32 bit triangles index format is going to be used for this mesh combine");
                    return false;
                }

            }

            return true;
        }


        private static Transform FindOrCreateMergeRoot(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).gameObject.name.Equals(mergeRootName))
                    return parent.GetChild(i);
            }

            GameObject newGO = new GameObject();
            newGO.transform.parent = parent;
            newGO.transform.localPosition = Vector3.zero;
            newGO.name = mergeRootName;

            SetStaticFlags(newGO);

            return newGO.transform;
        }

        private static void Revert(SuperCombiner combiner, Transform parent)
        {
            Transform root = FindOrCreateMergeRoot(parent);

            for (int i = 0; i < root.childCount; i++)
            {
                var mergedMesh = root.GetChild(i).GetComponent<MeshFilter>().sharedMesh;
                string path = AssetDatabase.GetAssetPath(mergedMesh);
                AssetDatabase.DeleteAsset(path);
            }

            DestroyImmediate(root.gameObject);

            int arraySize = combiner.mergedObjects.Count;
            for (int i = 0; i < arraySize; i++)
            {
                var revertData = combiner.mergedObjects[i];
                revertData.meshFilter.GetComponent<MeshRenderer>().enabled = true;
                if (revertData.meshGUID != "")
                {
                    string pathToMesh = AssetDatabase.GUIDToAssetPath(revertData.meshGUID);
                    revertData.meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathToMesh);
                }
            }

            combiner.mergedObjects.Clear();
        }


        public static void RevertAndDestroy(SuperCombiner combiner)
        {
            Transform parent = combiner.transform;
            Transform root = FindOrCreateMergeRoot(parent);

            for (int i = 0; i < root.childCount; i++)
            {
                var mergedMesh = root.GetChild(i).GetComponent<MeshFilter>().sharedMesh;
                string path = AssetDatabase.GetAssetPath(mergedMesh);
                AssetDatabase.DeleteAsset(path);
            }

            DestroyImmediate(root.gameObject);

            int arraySize = combiner.mergedObjects.Count;
            for (int i = 0; i < arraySize; i++)
            {
                var revertData = combiner.mergedObjects[i];
                revertData.meshFilter.GetComponent<MeshRenderer>().enabled = true;
                if (revertData.meshGUID != "")
                {
                    string pathToMesh = AssetDatabase.GUIDToAssetPath(revertData.meshGUID);
                    revertData.meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathToMesh);
                }
            }

            DestroyImmediate(combiner);
        }


        private static void SetStaticFlags(GameObject go)
        {

            var flags =
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.ReflectionProbeStatic |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.OffMeshLinkGeneration;

            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        private const string superCombinerPath = "Assets/Models/SuperCombinerMeshes";
        private const string assetFileType = ".asset";

        private static string GetMeshPath(string meshName)
        {
            if (!AssetDatabase.IsValidFolder(superCombinerPath))
                AssetDatabase.CreateFolder("Assets/Models", "SuperCombinerMeshes");

            string pathPrefix = superCombinerPath + "/" + meshName;
            string path = pathPrefix + assetFileType;
            int i = 1;

            while (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                path = pathPrefix + "_" + i + assetFileType;
                i++;
            }

            return path;
        }

        private static SuperCombiner.RevertRendererData CreateRevert(MeshFilter mf)
        {
            SuperCombiner.RevertRendererData revert = new SuperCombiner.RevertRendererData();
            revert.meshFilter = mf;
            revert.meshGUID = "";
            if (!IsMeshPrimitive(mf.sharedMesh))
            {
                bool assetAlreadyExists = AssetDatabase.Contains(mf.sharedMesh);

                if (assetAlreadyExists)
                {
                    string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                    revert.meshGUID = AssetDatabase.AssetPathToGUID(path);
                }

                mf.sharedMesh = null;
            }

            return revert;
        }

        private const string primitiveCubeName = "Cube";
        private const string primitiveSphereName = "Sphere";
        private const string primitiveCylinderName = "Cylinder";
        private const string primitiveCapsuleName = "Capsule";
        private const string primitivePlaneName = "Plane";
        private const string primitiveQuadName = "Quad";

        private static bool IsMeshPrimitive(Mesh mesh)
        {
            string meshName = mesh.name;

            bool isPrimitive =
                meshName.Equals(primitiveCubeName) ||
                meshName.Equals(primitiveSphereName) ||
                meshName.Equals(primitiveCylinderName) ||
                meshName.Equals(primitiveCapsuleName) ||
                meshName.Equals(primitivePlaneName) ||
                meshName.Equals(primitiveQuadName);

            return isPrimitive;
        }
    }
}
