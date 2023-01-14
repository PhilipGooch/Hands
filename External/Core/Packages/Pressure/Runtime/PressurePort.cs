using NBG.Core;
using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Pressure
{
    public class PressurePort : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        [SerializeField]
        private PressurePort connection;
        public PressurePort Connection => connection;

        [SerializeField]
        private PressureNode parent;
        public PressureNode Parent
        {
            get
            {
                if (parent == null)
                    parent = GetComponentInParent<PressureNode>();

                return parent;
            }
        }

        [SerializeField]
        private bool open;
        [NodeAPI("IsOpen")]
        public bool Open
        {
            get => open;
            set => open = value;
        }

        [SerializeField]
        private float lerpSpeed;

        private float portPressure;
        [NodeAPI("PortPressure")]
        public float PortPressure => portPressure;

        /// <summary>
        /// Allows for a smooth pressure transition.
        /// </summary>
        private float lerpedPortPressure;
        [NodeAPI("LerpedPortPressure")]
        public float LerpedPortPressure => lerpedPortPressure;

        [NodeAPI("OnPortPressureChanged")]
        public event Action<float> onPortPressureChanged;
        /// <summary>
        /// Allows for a smooth pressure transition.
        /// </summary>
        [NodeAPI("OnPortLerpedPressureChanged")]
        public event Action<float> onPortLerpedPressureChanged;

        private float pressureLerpStart;
        private float pressureLerpGoal;
        private float currentPressureLerpProgress;
        private float lerpedPressure;
        public bool Enabled => isActiveAndEnabled;

        void IManagedBehaviour.OnLevelLoaded() { }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        void IOnFixedUpdate.OnFixedUpdate()
        {
            var pressure = Open && Connection == null && Parent != null ? Parent.Pressure : 0;

            if (pressure != portPressure)
            {
                pressureLerpStart = portPressure;
                pressureLerpGoal = pressure;
                currentPressureLerpProgress = 0;
                lerpedPressure = pressureLerpStart;

                portPressure = pressure;
                onPortPressureChanged?.Invoke(portPressure);
            }

            if (currentPressureLerpProgress < 1)
            {
                currentPressureLerpProgress = Mathf.Clamp01(currentPressureLerpProgress + (lerpSpeed * Time.fixedDeltaTime));
                lerpedPressure = Mathf.Lerp(pressureLerpStart, pressureLerpGoal, currentPressureLerpProgress);

                if (currentPressureLerpProgress >= 1)
                    lerpedPressure = pressureLerpGoal;

                onPortLerpedPressureChanged?.Invoke(lerpedPressure);
            }
        }

        public void Connect(PressurePort other)
        {
            if (other != this)
                connection = other;
        }

        public void Disconnect()
        {
            connection = null;
        }

        public void RegisterNewNodeParent(PressureNode toRegister)
        {
            if (parent != null)
                parent.UnregisterPort(this);

            parent = toRegister;
            parent.RegisterNewPort(this);
        }

        public void UnregisterFromParent()
        {
            if (parent != null)
                parent.UnregisterPort(this);

            parent = null;
        }
    }
}
