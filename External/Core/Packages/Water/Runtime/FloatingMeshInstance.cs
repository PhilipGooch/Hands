using NBG.Core;
using NBG.Core.GameSystems;
using System;
using UnityEngine;

namespace NBG.Water
{
    [Serializable]
    public sealed class FloatingMeshInstance : IFloatingMesh
    {
        [Tooltip("GameObject with the hull (MeshFilter and a Collider) of the floatable mesh. Defines the collision surfaces that connect to the water surface.")]
        [SerializeField, ReadOnlyInPlayModeField]
        GameObject hullGameObject;

        [Tooltip("The mesh used for floatation calculations. Normally automatically acquired from Hull GameObject MeshFilter.")]
        [SerializeField, ReadOnlyInPlayModeField]
        Mesh meshOverride;

        [SerializeField]
        FloatingMeshInstanceData settings = FloatingMeshInstanceData.CreateDefault(); // Networking: setting this on client will do nothing. Changes on server are not transmitted to client.
        ref FloatingMeshInstanceData IFloatingMesh.InstanceData => ref settings;

        GameObject _go;
        Rigidbody _rb;
        IWaterSensor _waterSensor;
        Mesh _mesh;
        bool _registered;
        bool _lastFrameSubmerged;

        internal IFloatingMeshBackend Backend { get; private set; }
        public bool Submerged => (_waterSensor != null ? _waterSensor.Submerged : false);

        Rigidbody IFloatingMesh.Rigidbody => this._rb;
        GameObject IFloatingMesh.FloatingGameObject => this._go;
        GameObject IFloatingMesh.HullGameObject => this.hullGameObject;
        IWaterSensor IFloatingMesh.HullWaterSensor => this._waterSensor;
        Mesh IFloatingMesh.HullMesh => this._mesh;

        public Mesh GetFinalMesh()
        {
            if (meshOverride != null)
                return meshOverride;

            if (hullGameObject != null)
            {
                var mf = hullGameObject.GetComponent<MeshFilter>();
                if (mf != null)
                    return mf.sharedMesh;
            }

            return null;
        }

        public void Initialize(GameObject gameObject, IFloatingMeshSettings settings)
        {
            _go = gameObject;

            _rb = gameObject.GetComponent<Rigidbody>();
            if (_rb == null)
                _rb = gameObject.GetComponentInParent<Rigidbody>();

            // Hull setup
            if (hullGameObject == null)
            {
                Debug.LogError($"{nameof(FloatingMesh)} does not have hull gameobject assigned: {gameObject.GetFullPath()}", gameObject);
                return;
            }

            var hullCollider = hullGameObject.GetComponent<Collider>();
            if (hullCollider == null)
            {
                // Hull requires a collider to do overlap checks with bodies of water
                Debug.LogError($"{nameof(FloatingMesh)} hull collider is missing: {gameObject.GetFullPath()}", gameObject);
                return;
            }

            _waterSensor = hullGameObject.GetComponent<IWaterSensor>();
            if (_waterSensor == null)
                _waterSensor = hullGameObject.AddComponent<WaterSensor>();

            // Mesh
            _mesh = GetFinalMesh();
            if (_mesh == null)
            {
                Debug.LogError($"{nameof(FloatingMesh)} could not determine the Mesh to use : {gameObject.GetFullPath()}", gameObject);
                return;
            }

            var waterSystem = GameSystemWorldDefault.Instance.GetExistingSystem<WaterSystem>();
            Backend = waterSystem.Register(this, settings);
            _registered = true;
        }

        public void Shutdown()
        {
            if (_registered)
            {
                var waterSystem = GameSystemWorldDefault.Instance.GetExistingSystem<WaterSystem>();
                waterSystem.Unregister(this);
                Backend = null;
                _registered = false;
            }
        }

        public bool IsPointInsideWater(Vector3 worldPos)
        {
            var waterBodies = _waterSensor.BodiesOfWater;
            if (waterBodies.Count == 0)
                return false;

            foreach (var body in waterBodies)
            {
                if (body.IsPointInsideWater(worldPos))
                    return true;
            }

            return false;
        }
    }
}
