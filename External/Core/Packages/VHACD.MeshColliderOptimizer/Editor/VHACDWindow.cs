using System.Collections.Generic;
using System.IO;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;


namespace MeshProcess
{
    internal class VHACDWindow : EditorWindow
    {
        bool additionalOptions = false;
        bool deleteExistingColliders = false;
        bool deletePreviouslyGeneratedColliders = true;

        const int kLabelWidth = 250;
        const string CollisionMeshIndicator = "-CollisionMesh-";
        const string GameObjectPrefix = "GeneratedCollider-";

        VHACD vhacd;
        VHACD Vhacd
        {
            get
            {
                if (vhacd == null)
                    vhacd = new VHACD();
                return vhacd;
            }
            set
            {
                vhacd = value;
            }
        }

        [MenuItem("No Brakes Games/Mesh Collider Optimizer...")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<VHACDWindow>("Mesh Collider Optimizer");

            window.Show();
            window.minSize = new Vector2(400, 200);
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private MeshFilter GetSelectedMeshFilter()
        {
            var go = Selection.activeObject as GameObject;
            if (go != null && go.scene.IsValid())
            {
                var meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();
                return meshFilter;
            }

            return null;
        }

   
        void OnGUI()
        {
            var meshFilter = GetSelectedMeshFilter();
            var valid = (meshFilter != null && meshFilter.sharedMesh != null);
            if (!valid)
            {
                EditorGUILayout.LabelField("Select an editable object with a MeshFilter component.");
            }

            EditorGUI.BeginDisabledGroup(!valid);

            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = kLabelWidth;

            VHACD.Parameters parameters = Vhacd.m_parameters;
            PrefabData prefabData = new PrefabData();
            // Primary settings
            deleteExistingColliders = EditorGUILayout.Toggle("Delete original colliders", deleteExistingColliders);
            deletePreviouslyGeneratedColliders = EditorGUILayout.Toggle("Delete previously generated colliders", deletePreviouslyGeneratedColliders);

            parameters.m_maxNumVerticesPerCH = (uint)EditorGUILayout.IntSlider("Max number of vertices per collider", (int)parameters.m_maxNumVerticesPerCH, 4, 1024);
            parameters.m_maxConvexHulls = (uint)EditorGUILayout.IntSlider("Max colliders", (int)parameters.m_maxConvexHulls, 1, 1024);

            var buttonSuffix = string.Empty;
            if (valid)
            {
                prefabData = GetPrefabInformation(meshFilter.gameObject);
                buttonSuffix = prefabData.isPrefab ? " on asset" : " in scene";
                if (PrefabUtility.IsPartOfModelPrefab(meshFilter.gameObject))
                    buttonSuffix += " (can't apply to model prefab)";
            }

            if (GUILayout.Button("Generate Convex Collider" + (Vhacd.m_parameters.m_maxConvexHulls > 1 ? "s" : "") + buttonSuffix))
            {
                Generate(meshFilter, vhacd, prefabData, deleteExistingColliders, deletePreviouslyGeneratedColliders);
            }

            // Additional settings
            additionalOptions = EditorGUILayout.Foldout(additionalOptions, "Additional options");
            if (additionalOptions)
            {
                parameters.m_concavity = EditorGUILayout.Slider("Concavity", (float)parameters.m_concavity, 0, 1);
                parameters.m_alpha = EditorGUILayout.Slider("Alpha", (float)parameters.m_alpha, 0, 1);
                parameters.m_beta = EditorGUILayout.Slider("Beta", (float)parameters.m_beta, 0, 1);
                parameters.m_minVolumePerCH = EditorGUILayout.Slider("Min Volume Per Convex Hull", (float)parameters.m_minVolumePerCH, 0f, 0.01f);
                parameters.m_resolution = (uint)EditorGUILayout.IntSlider("Resolution", (int)parameters.m_resolution, 10000, 64000000);
                parameters.m_planeDownsampling = (uint)EditorGUILayout.IntSlider("Plane Downsampling", (int)parameters.m_planeDownsampling, 1, 16);
                parameters.m_convexhullDownsampling = (uint)EditorGUILayout.IntSlider("Convex Hull Downsampling", (int)parameters.m_convexhullDownsampling, 1, 16);
                parameters.m_pca = (uint)EditorGUILayout.IntSlider("Pca", (int)parameters.m_pca, 0, 1);
                parameters.m_mode = (uint)EditorGUILayout.IntSlider("Mode", (int)parameters.m_mode, 0, 1);
                parameters.m_convexhullApproximation = (uint)EditorGUILayout.IntSlider("Convex Hull Approximation", (int)parameters.m_convexhullApproximation, 0, 1);
                parameters.m_oclAcceleration = (uint)EditorGUILayout.IntSlider("Ocl Acceleration", (int)parameters.m_oclAcceleration, 0, 1);
                parameters.m_projectHullVertices = EditorGUILayout.Toggle("Project Hull Vertices", parameters.m_projectHullVertices);
            }

            Vhacd.m_parameters = parameters;

            EditorGUIUtility.labelWidth = prevLabelWidth;

            EditorGUI.EndDisabledGroup();
        }

        static void Generate(MeshFilter meshFilter, VHACD vhacd, PrefabData prefabData, bool deleteObjectColliders, bool deletePreviouslyGeneratedColliders)
        {
            var obj = meshFilter.gameObject;

            string assetPath = null;
            string meshNamePrefix = string.Empty;
            if (prefabData.isPrefab)
            {
                assetPath = $"{prefabData.dir}/{prefabData.name}.{obj.name}.generatedcolliders.asset";
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                meshNamePrefix = $"{prefabData.name}.{obj.name}{CollisionMeshIndicator}";
            }
            else
            {
                meshNamePrefix = $"{obj.name}{CollisionMeshIndicator}";
            }

            if (deletePreviouslyGeneratedColliders)
            {
                List<GameObject> generatedColliderObjects = GetPreviouslyGeneratedColliders(meshFilter.transform);
                foreach (var item in generatedColliderObjects)
                {
                    DestroyImmediate(item);
                }
            }

            var meshes = vhacd.GenerateConvexMeshes(meshFilter.sharedMesh);
            var validIndex = 0;
            for (int i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                if (mesh == null)
                    continue;

                mesh.name = $"{meshNamePrefix}{validIndex}";
                var go = new GameObject($"{GameObjectPrefix}{validIndex}");
                go.transform.parent = meshFilter.transform;

                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;

                go.transform.position = meshFilter.transform.position;
                go.transform.rotation = meshFilter.transform.rotation;
                go.transform.localScale = Vector3.one;

                if (assetPath != null)
                {
                    if (validIndex == 0)
                    {
                        CreateNewAsset(assetPath, meshFilter);
                    }
                    AssetDatabase.AddObjectToAsset(mesh, assetPath);
                }

                EditorUtility.SetDirty(meshFilter);

                ++validIndex;
            }

            if (deleteObjectColliders && validIndex > 0)
            {
                var existingColliders = meshFilter.GetComponents<Collider>();

                foreach (var collider in existingColliders)
                {
                    DestroyImmediate(collider);
                }
            }

            if (assetPath != null)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        static void CreateNewAsset(string path, MeshFilter meshFilter)
        {
            VHACDParameters asset = ScriptableObject.CreateInstance<VHACDParameters>();
            asset.sourceMeshReference = meshFilter.sharedMesh;
            AssetDatabase.CreateAsset(asset, path);

            Debug.Log($"Created mesh colliders asset: {asset.name}", asset);
        }


        static List<GameObject> GetPreviouslyGeneratedColliders(Transform parent)
        {
            List<GameObject> generatedColliderObjects = new List<GameObject>();
            foreach (Transform item in parent)
            {
                var filter = item.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name.Contains(CollisionMeshIndicator))
                    generatedColliderObjects.Add(filter.gameObject);
            }

            return generatedColliderObjects;
        }

        PrefabData GetPrefabInformation(GameObject go)
        {
            if (EditorUtility.IsPersistent(go) || PrefabUtility.IsPartOfModelPrefab(go))
            {
                return new PrefabData();
            }

            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(go);
            //The GameObject is part of the Prefab contents of the Prefab Asset
            if (prefabStage != null)
            {
                GameObject openPrefabThatContentsIsPartOf = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
                return new PrefabData(true, AssetDatabase.GetAssetPath(openPrefabThatContentsIsPartOf));
            }
            //The GameObject is a plain GameObject (not part of a Prefab instance)
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
            {
                return new PrefabData();
            }
            else
            {
                // This is the Prefab Asset that determines the icon that is shown in the Hierarchy for the nearest root.
                GameObject nearestRootPrefabAssetObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go));
                return new PrefabData(true, AssetDatabase.GetAssetPath(nearestRootPrefabAssetObject));

            }

        }

        class PrefabData
        {
            public bool isPrefab;
            public string dir;
            public string name;

            public PrefabData()
            {
                isPrefab = false;
                dir = "";
                name = "";
            }
            public PrefabData(bool isPrefab, string fullPath)
            {
                this.isPrefab = isPrefab;
                dir = Path.GetDirectoryName(fullPath);
                name = Path.GetFileName(fullPath);
            }
        }
    }
}
