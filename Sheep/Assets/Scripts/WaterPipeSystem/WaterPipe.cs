using NBG.LogicGraph;
using UnityEngine;

namespace WaterPipeSystem
{
    public class WaterPipe : Block
    {
        [Tooltip("Determines how high water must go above bottom of the pipe before max water flow.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float resistence = 1f;
        public float Resistence => resistence;
        public float WaterFlow { get; set; }
        [SerializeField]
        float radius;   
        public float Radius => radius;
        
        // Restriction indicates how much the pipe is restricted by the valve attached to it (if there is one).
        [NodeAPI("Restriction")]
        public float Restriction { get; set; }

        protected override void Start()
        {
            base.Start();
            WaterPipeManager.Instance.AddPipe(this);
        }
        
        private void OnDestroy()
        {
            WaterPipeManager.Instance.RemovePipe(this);
        }
        
        public WaterSocket GetOtherSocket(WaterSocket socket)
        {
            foreach (WaterSocket otherSocket in Sockets)
            {
                if (otherSocket != socket) return otherSocket;
            }
            return null;
        }
    }
}
