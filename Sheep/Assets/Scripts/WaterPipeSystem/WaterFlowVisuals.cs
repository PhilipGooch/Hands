using UnityEngine;

namespace WaterPipeSystem
{
    public class WaterFlowVisuals : MonoBehaviour // ActivatableNode
    {
        public float Flow { get; set; }
        private float minimumWaterFlowForRendering = 0.05f;
        private MeshRenderer[] meshRenderers;

        private void Awake()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        private void Start()
        {
            Update();
        }

        void Update()
        {
            transform.localScale = new Vector3(Flow, 1, 1);

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (Flow > minimumWaterFlowForRendering)
                {
                    meshRenderer.enabled = true;
                }
                else
                {
                    meshRenderer.enabled = false;
                }
            }
        }
    }
}
