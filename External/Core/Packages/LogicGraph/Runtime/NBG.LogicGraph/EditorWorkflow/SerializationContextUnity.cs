using NBG.Core;
using System;
using System.Collections.Generic;

namespace NBG.LogicGraph
{
    class SerializationContextUnity : ISerializationContext
    {
        internal List<LogicGraphPlayer.UnityObjectReferenceContainer> existingUnityObjectIds = new List<LogicGraphPlayer.UnityObjectReferenceContainer>();

        internal Dictionary<UnityEngine.Object, SerializableGuid> unityObjectIds = new Dictionary<UnityEngine.Object, SerializableGuid>();
        internal List<LogicGraphPlayer.EntryContainer> entries;
        internal List<LogicGraphPlayer.VariableContainer> variables;

        void ISerializationContext.OnWriteNodeEntry(SerializableGuid id, SerializableNodeEntry entry)
        {
            var se = new LogicGraphPlayer.EntryContainer()
            {
                id = id,
                entry = entry
            };
            entries.Add(se);
        }

        SerializableGuid ISerializationContext.ReferenceUnityObject(UnityEngine.Object value)
        {
            // Record UnityEngine.Object reference
            var id = SerializableGuid.empty;
            if (!unityObjectIds.ContainsKey(value))
            {
                var entry = existingUnityObjectIds.Find(x => x.obj == value);
                if (entry.id != SerializableGuid.empty)
                {
                    id = entry.id; // Reuse the existing ids
                }
                else
                {
                    id = SerializableGuid.Create(Guid.NewGuid()); // Allocate a new id
                }
                unityObjectIds.Add(value, id);
            }
            else
            {
                id = unityObjectIds[value];
            }
            return id;
        }

        void ISerializationContext.OnWriteVariableEntry(SerializableGuid id, SerializableVariableEntry entry)
        {
            var se = new LogicGraphPlayer.VariableContainer()
            {
                id = id,
                entry = entry
            };
            variables.Add(se);
        }
    }
}
