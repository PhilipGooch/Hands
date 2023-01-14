using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.Pressure
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PressurePort))]
    public class PressurePortInspector : Editor
    {
        PressurePort port;

        private void OnEnable()
        {
            port = target as PressurePort;
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            if (GUILayout.Button("Connect To Nearest Port"))
            {
                ConnectToNearest();
                EditorUtility.SetDirty(port);
            }
        }

        public void ConnectToNearest()
        {
            const float searchRadius = 0.3f;
            Collider[] castResults = new Collider[32];
            List<PressurePort> nearbyPorts = new List<PressurePort>();

            int c = Physics.OverlapSphereNonAlloc(port.transform.position, searchRadius, castResults, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < c; i++)
            {
                var port = castResults[i].GetComponent<PressurePort>();

                if (port != null && port != this && port.Parent != this.port.Parent)
                    nearbyPorts.Add(port);
            }

            PressurePort closesPort = null;
            foreach (var item in nearbyPorts)
            {
                if (closesPort == null)
                    closesPort = item;
                else
                {
                    if (Vector3.SqrMagnitude(port.transform.position - item.transform.position) < Vector3.SqrMagnitude(port.transform.position - closesPort.transform.position))
                        closesPort = item;
                }
            }

            if (closesPort != null)
            {
                port.Connect(closesPort);
                closesPort.Connect(port);
            }
        }
    }
}
