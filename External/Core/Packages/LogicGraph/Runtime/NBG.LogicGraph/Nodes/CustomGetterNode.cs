using NBG.Core;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.Getter)]
    [NodeSerialization("CustomGetter")]
    class CustomGetterNode : Node, INodeOnInitialize, INodeOnAddToGraph, INodeCustomIO, INodeVariant
    {
        public override string Name
        {
            get => "Getter (custom)";
        }

        SerializableGuid INodeVariant.VariantBacking => ((LogicGraph)Owner).GetNodeId(this);
        string INodeVariant.VariantName => OutputName;
        System.Type INodeVariant.VariantHandler => typeof(HandleCustomGetterNode);

        public string OutputName
        {
            get => _properties[0].variable.Get(0).Get<string>();
            private set => _properties[0].variable.Get(0).Set<string>(value);
        }

        public override bool UserDefinedStackInputs { get { return true; } }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);

            var p = new NodeProperty();
            p.Name = "OutputName";
            p.Type = VariableType.String;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);
        }

        void INodeOnAddToGraph.OnAddToGraph()
        {
            Debug.Assert(_stackInputs.Count == 0);

            // Default output
            var si = new StackInput();
            si.Name = "out";
            si.Type = VariableType.Bool;
            si.variable = VarHandleContainers.Create(si.Type);
            si.debugLastValue = VarHandleContainers.Create(si.Type);
            _stackInputs.Add(si);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            // Nothing to do
            return 0;
        }

        public void SetOutputName(string outputName)
        {
            OutputName = outputName;
        }

        public void AddCustomIO(string name, VariableType type)
        {
            throw new System.InvalidOperationException($"{nameof(CustomGetterNode)} only has 1 i/o");
        }

        public void RemoveCustomIO(int index)
        {
            throw new System.InvalidOperationException($"{nameof(CustomGetterNode)} only has 1 i/o");
        }

        public void UpdateCustomIO(int index, string name, VariableType type)
        {
            if (index != 0)
                throw new System.InvalidOperationException($"{nameof(CustomGetterNode)} only has 1 i/o");

            var si = _stackInputs[index];

            if (si.Type != type)
            {
                si = new StackInput();
                si.Name = name;
                si.Type = type;
                si.variable = VarHandleContainers.Create(type);
                si.debugLastValue = VarHandleContainers.Create(type);
                _stackInputs[index] = si;
            }
            else
            {
                si.Name = name;
            }
        }

        public string GetCustomIOName(int index)
        {
            return _stackInputs[index].Name;
        }

        public VariableType GetCustomIOType(int index)
        {
            return _stackInputs[index].Type;
        }

        bool INodeCustomIO.CanAddAndRemove => false;
        public INodeCustomIO.Type CustomIOType => INodeCustomIO.Type.Inputs;
    }
}
