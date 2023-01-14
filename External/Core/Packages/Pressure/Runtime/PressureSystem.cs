using NBG.Core.GameSystems;
using System.Collections.Generic;

namespace NBG.Pressure
{
    [UpdateInGroup(typeof(UpdateSystemGroup))]
    public class PressureSystem : GameSystem
    {
        private HashSet<PressureNode> passiveNodes = new HashSet<PressureNode>();
        private HashSet<PressureSource> rootNodes = new HashSet<PressureSource>();

        protected override void OnUpdate()
        {
            foreach (var node in passiveNodes)
            {
                node.ResetNode();
            }

            foreach (var node in rootNodes)
            {
                node.Rebuild();
            }
        }

        public void RegisterNode(PressureNode steamNode)
        {
            if (steamNode is PressureSource steamSource)
            {
                rootNodes.Add(steamSource);
            }
            else
            {
                passiveNodes.Add(steamNode);
            }
        }

        public void UnregisterNode(PressureNode steamNode)
        {
            if (steamNode is PressureSource steamSource)
            {
                rootNodes.Remove(steamSource);
            }
            else
            {
                passiveNodes.Remove(steamNode);
            }
        }
    }
}
