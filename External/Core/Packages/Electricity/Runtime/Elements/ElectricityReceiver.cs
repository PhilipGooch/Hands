using UnityEngine;

namespace NBG.Electricity
{
    /// <summary>
    /// Base class for any power consumer on Electricty
    /// </summary>
    public class ElectricityReceiver : ElectricityComponent, IReceiver, IElectricityNode
    {
        public float maxAmps = 10;
        public float minAmps = 0;
        protected float currentAmps;

        public float Input { get => currentAmps; set => currentAmps = value; }
        protected virtual void Awake()
        {
            this.Register();
        }
        public virtual void Tick()
        {
        }
    }
}
