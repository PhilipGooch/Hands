using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.SuperCombiner
{
    public class SuperCombiner : MonoBehaviour
    {
        [System.Serializable]
        public class RevertRendererData
        {
            public MeshFilter meshFilter;
            public string meshGUID;
        }

        public List<RevertRendererData> mergedObjects = new List<RevertRendererData>();

        public bool applied = false;
        public List<int> lighmapIndices = new List<int>();
        public Transform root;


        private static bool levelLoadSetup = false;

        private void RegisterInitialization()
        {
            var interfaceType = typeof(ISuperCombinerInitialization);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => !p.IsInterface && p.GetInterfaces().Contains(interfaceType));

            foreach (var t in types)
            {
                var initialization = (ISuperCombinerInitialization)Activator.CreateInstance(t);
                initialization.Initialize();
            }
        }
        private void Awake()
        {
            if (!levelLoadSetup)
            {
                levelLoadSetup = true;
                RegisterInitialization();
                ApplyAllLighmapIndices();
            }
        }

        public void ApplyLightmapIndices()
        {
            int childCount = root.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = root.GetChild(i);
                child.GetComponent<MeshRenderer>().lightmapIndex = lighmapIndices[i];
            }
        }

        public static void ApplyAllLighmapIndices()
        {
            var combiners = GameObject.FindObjectsOfType<SuperCombiner>();
            if (combiners != null)
                foreach (var x in combiners)
                    x.ApplyLightmapIndices();
        }
    }

}
