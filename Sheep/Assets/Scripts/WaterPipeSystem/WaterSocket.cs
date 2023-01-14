using Recoil;
using UnityEngine;

namespace WaterPipeSystem
{
    [RequireComponent(typeof(SphereCollider))]
    public class WaterSocket : BlockSocket
    {
        public WaterPipe Pipe { get; private set; }
        public int WaterFlowDirection { get; set; }
        public float WaterPressure { get; set; }
    
        private WaterFlowVisuals water;
    
        protected override void Awake()
        {
            base.Awake();
            Pipe = GetComponentInParent<WaterPipe>();
            water = GetComponentInChildren<WaterFlowVisuals>(true);
            water.gameObject.SetActive(true);
        }
    
        private void Update()
        {
            if (!TargetSocket && WaterFlowDirection == 1)
            {
                water.Flow = Pipe.WaterFlow;
            }
            else
            {
                water.Flow = 0.0f;
            }
        }
    }
}
