using NBG.Core;
using System.Collections.Generic;
using System.Linq;

namespace NBG.LogicGraph
{
    class DeserializationContextUnity : IDeserializationContext
    {
        internal List<LogicGraphPlayer.EntryContainer> _entries;
        internal List<LogicGraphPlayer.VariableContainer> _variables;
        internal List<LogicGraphPlayer.UnityObjectReferenceContainer> _unityObjectReferences = new List<LogicGraphPlayer.UnityObjectReferenceContainer>();

        public int NodeCount => _entries.Count;
        public int VariableCount => _variables.Count;

        void IDeserializationContext.GetNodeEntry(int index, out SerializableGuid id, out SerializableNodeEntry entry)
        {
            var container = _entries[index];
            id = container.id;
            entry = container.entry;
        }

        UnityEngine.Object IDeserializationContext.GetUnityObject(SerializableGuid id)
        {
            var obj = _unityObjectReferences.Where(x => x.id == id).Select(x => x.obj).SingleOrDefault();
            return obj;
        }

        void IDeserializationContext.GetVariableEntry(int index, out SerializableGuid id, out SerializableVariableEntry entry)
        {
            var container = _variables[index];
            id = container.id;
            entry = container.entry;
        }
    }
}
