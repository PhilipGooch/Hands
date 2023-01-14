using NBG.Core;
using NBG.LogicGraph.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeSerialization("Group")]
    class GroupNode : Node, INodeOnInitialize, INodeResettable
    {
        const string kHeaderProperty = "Header";
        const string kChildProperty = "Child";

        public string Header
        {
            get => _properties[0].variable.Get(0).Get<string>();
            set => _properties[0].variable.Get(0).Set<string>(value);
        }

        public IEnumerable<SerializableGuid> Children
        {
            get
            {
                foreach (var prop in _properties.Where(x => x.Name == kChildProperty))
                {
                    var id = prop.variable.Get(0).Get<SerializableGuid>();
                    yield return id;
                }
            }
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);

            // First property is expected to be the header
            var p = new NodeProperty();
            p.Name = kHeaderProperty;
            p.Type = VariableType.String;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            throw new System.NotSupportedException();
        }

        protected override string OnDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            var serializer = (IVarSerializerTyped<string>)VariableTypeSerializers.GetSerializer(VariableType.String);

            // Populate all children
            var props = entry.Properties.Where(x => x.Name == kChildProperty);
            foreach (var prop in props)
            {
                var p = new NodeProperty();
                p.Name = prop.Name;
                p.Type = prop.Type;
                p.variable = VarHandleContainers.Create(prop.Type);
                p.variable.Get(0).Set<SerializableGuid>(VarSerializerGuid.Deserialize(prop.Value));
                _properties.Add(p);
            }

            return null;
        }

        protected override void OnDeserializeProperties(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            var pEntry = entry.Properties.Find(x => x.Name == kHeaderProperty);
            _properties[0].variable.Get(0).Deserialize(ctx, pEntry.Value);
            
            // Other properties are handled as part of the main deserialization
        }

        public void AddChild(SerializableGuid id)
        {
            var ownId = ((LogicGraph)Owner).GetNodeId(this);
            if (ownId == id)
                throw new System.Exception($"Adding {nameof(GroupNode)} to itself.");

            foreach (var prop in _properties.Where(x => x.Name == kChildProperty))
            {
                var existingId = prop.variable.Get(0).Get<SerializableGuid>();
                if (existingId == id)
                    throw new System.Exception($"Adding a duplicate child to {nameof(GroupNode)}.");
            }

            var p = new NodeProperty();
            p.Name = kChildProperty;
            p.Type = VariableType.Guid;
            p.variable = VarHandleContainers.Create(p.Type);
            p.variable.Get(0).Set<SerializableGuid>(id);
            _properties.Add(p);
        }

        public void RemoveChild(SerializableGuid id)
        {
            _properties.RemoveAll(x => x.Name == kChildProperty && x.variable.Get(0).Get<SerializableGuid>() == id);
        }
    }
}
