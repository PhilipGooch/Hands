using NBG.LogicGraph;
using System;
using UnityEngine;

namespace Sample.LogicGraph
{
    public class Button : MonoBehaviour
    {
        [NodeAPI("Button was pressed", scope: NodeAPIScope.Sim)]
        public event Action OnPressed;

        public uint interval = 50;
        public uint initial = 0;

        uint counter;

        private void Awake()
        {
            counter = initial;
        }

        private void FixedUpdate()
        {
            if (counter < interval)
            {
                ++counter;
                return;
            }

            counter = 0;
            OnPressed?.Invoke(); // Simulate occasional event
        }
    }
}
