using NBG.Core;
using NBG.LogicGraph;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Water
{
    /// <summary>
    /// Defines an area where water depth can be sampled.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BodyOfWater : MonoBehaviour
    {
        [Tooltip("Reference to the direction the water is moving in")]
        [SerializeField] private Vector3 flow;

        [SerializeField, HideInInspector]
        BoxCollider boxCollider;

        List<WaterSensor> sensors = new List<WaterSensor>();
        float3 globalFlow;
        BoxBounds box;
        Vector3 scale;

        public BoxBounds GlobalBox => box;
        public float3 GlobalFlow => globalFlow;

        public delegate void OnTriggerDelegate(Collider collider);
        /// <summary>
        /// Fires when a collider enters this body of water.
        /// Networking: fires on server and on client based on local object positions. //TODO: figure out how to do Sim/View for networking.
        /// </summary>
        [NodeAPI("On collider enter water", scope: NodeAPIScope.Sim)]
        public event OnTriggerDelegate OnColliderEnterWater;
        /// <summary>
        /// Fires when a collider exits this body of water.
        /// Networking: fires on server and on client based on local object positions. //TODO: figure out how to do Sim/View for networking.
        /// </summary>
        [NodeAPI("On collider exit water", scope: NodeAPIScope.Sim)]
        public event OnTriggerDelegate OnColliderExitWater;

        void Awake()
        {
            globalFlow = transform.TransformDirection(flow);
            UpdateBox();
            scale = transform.lossyScale;
        }

        void OnValidate()
        {
            boxCollider = GetComponent<BoxCollider>();
            if (!boxCollider.isTrigger)
                Debug.LogError($"WaterBox requires BoxCollider to be a trigger");
        }

        void FixedUpdate()
        {
#if UNITY_EDITOR
            globalFlow = transform.TransformDirection(flow);
#endif
            if (scale != transform.lossyScale)
            {
                scale = transform.lossyScale;
                UpdateBox();
            }
        }

        void UpdateBox()
        {
            box = new BoxBounds(boxCollider);
        }

        private void OnDrawGizmos()
        {
            var gizmoBox = new BoxBounds(boxCollider);
            Gizmos.color = new Color(0, 0, 0.3f, 0.15f);
            gizmoBox.DrawGizmos(false);
        }

        public void OnTriggerEnter(Collider other)
        {
            OnColliderEnterWater?.Invoke(other);

            var sensor = other.gameObject.GetComponentInParent<WaterSensor>();
            if (sensor != null)
            {
                sensor.OnEnterBody(this);
                sensors.Add(sensor);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            OnColliderExitWater?.Invoke(other);

            var sensor = other.gameObject.GetComponentInParent<WaterSensor>();
            if (sensor != null)
            {
                sensor.OnLeaveBody(this);
                sensors.Remove(sensor);
            }
        }

        public void OnDisable() //TODO: use proximity tools?
        {
            for (int i = 0; i < sensors.Count; i++)
                sensors[i].OnLeaveBody(this);
            sensors.Clear();
        }

        public bool IsPointInsideWater(Vector3 worldPos)
        {
            return box.Contains(worldPos);
        }
    }
}
