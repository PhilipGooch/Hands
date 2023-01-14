using NBG.Core;
using NBG.Core.GameSystems;
using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Pressure
{
    public class PressureNode : MonoBehaviour, IManagedBehaviour
    {
        [NodeAPI("Pressure")]
        public float Pressure => pressure;
        private float pressure;

        [NodeAPI("OnPressureChanged")]
        public event Action<float> onPressureChanged;

        internal List<PressurePort> ports = new List<PressurePort>();

        private PressureSystem steamSystem;
        private PressureSystem SteamSystem
        {
            get
            {
                if (steamSystem == null)
                    steamSystem = GameSystemWorldDefault.Instance?.GetExistingSystem<PressureSystem>();
                return steamSystem;
            }
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            GetComponentsInChildren(ports);
            SteamSystem?.RegisterNode(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded() { }

        void IManagedBehaviour.OnLevelUnloaded() { }

        private void OnEnable()
        {
            SteamSystem?.RegisterNode(this);
        }

        private void OnDisable()
        {
            SteamSystem?.UnregisterNode(this);
        }

        internal void RegisterNewPort(PressurePort toRegister)
        {
            if (!ports.Contains(toRegister))
                ports.Add(toRegister);
        }

        internal void UnregisterPort(PressurePort toUnregister)
        {
            ports.Remove(toUnregister);
        }

        public virtual void UpdatePreassure(float addedValue)
        {
            pressure += addedValue;
            onPressureChanged?.Invoke(Pressure);
        }

        public void ResetNode()
        {
            pressure = 0;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawSphere(transform.position, 0.15f);
            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                if (port != null && port.Connection != null)
                    Gizmos.DrawLine(transform.position, port.Connection.Parent.transform.position);
            }
        }
    }
}
