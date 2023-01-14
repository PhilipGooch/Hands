using NBG.Core.GameSystems;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace NBG.Net.Transport
{
    public interface ITransportLayerUpdatable
    {
        void OnUpdate();
    }

    [UpdateInGroup(typeof(NetTransportsSystemGroup))]
    public class TransportSystem : GameSystem
    {
        [Preserve]
        public TransportSystem()
        {
        }

        private List<ITransportLayerUpdatable> updateTargets = new List<ITransportLayerUpdatable>();

        public override void DebugPrint(StringBuilder sb, int indent)
        {
            base.DebugPrint(sb, indent);
            indent++;
            for (int i = 0; i < updateTargets.Count; i++)
            {
                var desc = new string(' ', indent * 4);
                desc += $"[Custom subitem] {updateTargets[i].GetType().FullName}";
                sb.AppendLine(desc);
            }
        }

        public void Register(ITransportLayerUpdatable transportToUpdate)
        {
            if (updateTargets.Contains(transportToUpdate))
            {
                Debug.LogError($"{transportToUpdate} is already registered");
                return;
            }

            updateTargets.Add(transportToUpdate);
        }

        public void Unregister(ITransportLayerUpdatable transportToUpdate)
        {
            Debug.Assert(updateTargets.Contains(transportToUpdate), $"Trying to remove {nameof(ITransportLayerUpdatable)} that isn't on the list");

            updateTargets.Remove(transportToUpdate);
        }

        protected override void OnUpdate()
        {
            for (int i = 0; i < updateTargets.Count; i++)
            {
                updateTargets[i].OnUpdate();
            }
        }

        protected override void OnDestroy()
        {
            Debug.Assert(updateTargets.Count == 0, $"{nameof(TransportSystem)} still has {updateTargets.Count} lingering targets.");
        }
    }
}
