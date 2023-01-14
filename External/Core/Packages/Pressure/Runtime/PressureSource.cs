using NBG.LogicGraph;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Pressure
{
    public class PressureSource : PressureNode
    {
        [SerializeField]
        private float generatedPressure = 0;
        [NodeAPI("GeneratedPressure")]
        public float GeneratedPressure
        {
            get => generatedPressure;
            set => generatedPressure = value;
        }

        public override void UpdatePreassure(float addedValue)
        {
        }

        public void Rebuild()
        {
            List<PressureNode> checkedNodes = new List<PressureNode>();
            List<PressureNode> reachableNodes = new List<PressureNode>();
            Queue<PressureNode> nodesToCheck = new Queue<PressureNode>();

            int openPortsCount = 0;

            nodesToCheck.Enqueue(this);
            //do to the nature of the system, breadth first search is used
            while (nodesToCheck.Count > 0)
            {
                var element = nodesToCheck.Dequeue();

                if (checkedNodes.Contains(element))
                    continue;
                else
                    checkedNodes.Add(element);

                if (element is IBlockableNode valve && valve.Blocked)
                {
                    continue;
                }
                else
                    reachableNodes.Add(element);

                foreach (var item in element.ports)
                {
                    if (item.Connection == null)
                    {
                        if (item.Open)
                        {
                            openPortsCount++;
                        }
                    }

                    if (item.Connection != null)
                        nodesToCheck.Enqueue(item.Connection.Parent);
                }
            }

            foreach (var node in reachableNodes)
            {
                node.UpdatePreassure(generatedPressure / Mathf.Max(1, openPortsCount));
            }
        }
    }
}
