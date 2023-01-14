using NBG.LogicGraph;
using UnityEngine;

namespace Sample.LogicGraph
{
    public class Lamp : MonoBehaviour
    {
        public Light controlledLight;
        public bool powered;
        public Color color;

        [NodeAPI("Color", scope: NodeAPIScope.View)]
        public Color Color { get { return color; } set { color = value; } }

        [NodeAPI("Turn power on/off")]
        public bool Powered
        {
            get
            {
                return powered;
            }

            set
            {
                powered = value;
                controlledLight.enabled = powered;
            }
        }

        [NodeAPI("Turn power on/off")]
        public void Toggle(bool powered)
        {
            Powered = powered;
        }

        private void Awake()
        {
            controlledLight.enabled = powered;
        }
    }
}
