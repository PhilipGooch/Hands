
using UnityEngine;

namespace NBG.Electricity
{
    public class LogicGate : MonoBehaviour, IProvider, IElectricityNode
    {
        public LogicGateMode mode = LogicGateMode.And;

        [SerializeField]
        private readonly ReceiverSocket A, B;
        public enum LogicGateMode
        {
            And,
            Or
        }

        private float hA;
        public float Output => hA;

        public float WaterRange => throw new System.NotImplementedException();

        public void Awake()
        {
            Electricity.Instance.Register(this);

            Electricity.CreateDependency(this, A);
            Electricity.CreateDependency(this, B);
        }
        public void Tick()
        {
            switch (mode)
            {
                case LogicGateMode.And:
                    hA = A.Input > 0 && B.Input > 0 ? A.Input + B.Input : 0.0f;
                    break;
                case LogicGateMode.Or:
                    hA = A.Input + B.Input;
                    break;
            }
        }
    }
}
