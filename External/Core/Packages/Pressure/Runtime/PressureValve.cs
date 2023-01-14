using NBG.LogicGraph;
using UnityEngine;

namespace NBG.Pressure
{
    public class PressureValve : PressureNode, IBlockableNode
    {
        [SerializeField]
        private bool blocked;

        [NodeAPI("Blocked")]
        public bool Blocked
        {
            get => blocked;
            set => blocked = value;
        }
    }

    interface IBlockableNode
    {
        bool Blocked { get; set; }
    }
}
