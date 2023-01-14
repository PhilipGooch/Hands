using UnityEngine;

namespace NBG.Electricity
{
    public class ReceiverSocket : MonoBehaviour, IReceiver, IElectricityNode
    {
        private float hA;
        public float Input { get => hA; set => hA = value; }

        public bool IsEnd => false;

        public ReceiverSocket()
        {
            this.Register();
        }
        public void Tick()
        {
        }
    }
}
