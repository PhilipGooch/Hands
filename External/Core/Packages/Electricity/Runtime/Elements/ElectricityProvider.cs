using UnityEngine;

namespace NBG.Electricity
{
    /// <summary>
    /// This is used a power source for Electricity
    /// </summary>
    public class ElectricityProvider : ElectricityComponent, IProvider, IElectricityNode
    {
        [SerializeField] internal float power;
        public float Output { get => power; set => power = value; }
        protected virtual void Awake()
        {
            this.Register();
        }
        public virtual void Tick()
        {
        }
    }
}
