using System.Collections.Generic;
using UnityEngine;

namespace WaterPipeSystem
{
    [RequireComponent(typeof(MeshRenderer))]
    public class WaterPool : MonoBehaviour
    {
        [Tooltip("Determines if the water level of the pool is essentially infinite. Causing water to always flow from the pool.")]
        [SerializeField]
        private bool infinite;
        public bool Infinite => infinite;
        
        [Tooltip("Determines how fast the pool will fill up with water.")]
        [Range(0.0f, 10.0f)]
        [SerializeField]
        private float fillSpeed = 1.0f;
        
        [SerializeField]
        List<WaterSocket> sockets = new List<WaterSocket>();
        public IReadOnlyList<WaterSocket> Sockets => sockets;
        
        public float Depth { get; private set; }
        public float HeightAtBottomOfPool { get; private set; }
        
        private MeshRenderer meshRenderer;
        private float height;
        
        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            HeightAtBottomOfPool = transform.position.y - transform.lossyScale.y / 2;
            height = transform.lossyScale.y;
        }
        
        private void Start()
        {
            WaterPipeManager.Instance.AddPool(this);
            if (infinite)
            {
                Depth = float.MaxValue;
            }
        }
        
        private void OnDestroy()
        {
            WaterPipeManager.Instance.RemovePool(this);
        }
        
        private void FixedUpdate()
        {
            if (infinite) return;
        
            foreach (WaterSocket socket in sockets)
            {
                Depth += socket.Pipe.WaterFlow * socket.WaterFlowDirection * fillSpeed * Time.deltaTime;
            }
            Depth = Mathf.Clamp(Depth, 0.0f, height);
        }
        
        private void Update()
        {
            if (infinite) return;

            TransformPoolMesh();
        }
        
        private void TransformPoolMesh()
        {
            if (Depth == 0.0f)
            {
                meshRenderer.enabled = false;
            }
            else
            {
                meshRenderer.enabled = true;
            }
            transform.position = new Vector3(transform.position.x, HeightAtBottomOfPool + Depth / 2, transform.position.z);
            transform.localScale = new Vector3(transform.lossyScale.x, Depth, transform.lossyScale.z);
        }
    }
}
