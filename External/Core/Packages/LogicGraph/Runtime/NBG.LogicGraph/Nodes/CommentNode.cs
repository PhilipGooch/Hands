using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeSerialization("Comment")]
    class CommentNode : Node, INodeOnInitialize, INodeResettable
    {
        public string Header
        {
            get => _properties[0].variable.Get(0).Get<string>();
            set => _properties[0].variable.Get(0).Set<string>(value);
        }

        public string Body
        {
            get => _properties[1].variable.Get(0).Get<string>();
            set => _properties[1].variable.Get(0).Set<string>(value);
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);

            // Header
            {
                var p = new NodeProperty();
                p.Name = "Header";
                p.Type = VariableType.String;
                p.variable = VarHandleContainers.Create(p.Type);
                _properties.Add(p);
            }

            // Body
            {
                var p = new NodeProperty();
                p.Name = "Body";
                p.Type = VariableType.String;
                p.variable = VarHandleContainers.Create(p.Type);
                _properties.Add(p);
            }
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            throw new System.NotSupportedException();
        }
    }
}
