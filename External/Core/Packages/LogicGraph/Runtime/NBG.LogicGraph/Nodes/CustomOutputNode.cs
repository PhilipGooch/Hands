using NBG.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.Function)]
    [NodeSerialization("CustomOutput")]
    class CustomOutputNode : Node, INodeOnInitialize, INodeCustomIO, INodeVariant
    {
        public override string Name
        {
            get => "Output (Custom)";
        }

        SerializableGuid INodeVariant.VariantBacking => ((LogicGraph)Owner).GetNodeId(this);
        string INodeVariant.VariantName => OutputName;
        System.Type INodeVariant.VariantHandler => typeof(HandleCustomOutputNode);

        public string OutputName
        {
            get => _properties[0].variable.Get(0).Get<string>();
            private set => _properties[0].variable.Get(0).Set<string>(value);
        }

        public override bool HasFlowInput { get { return true; } }
        public override bool UserDefinedStackInputs { get { return true; } }

        public class Listener //TODO: improve and optimize
        {
            public ILogicGraph source;
            public SerializableGuid variant;
            public HandleCustomOutputNode target;
        }
        [ClearOnReload(newInstance: true)]
        internal static List<Listener> _listeners = new List<Listener>();

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);

            var p = new NodeProperty();
            p.Name = "OutputName";
            p.Type = VariableType.String;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            foreach (var handler in _listeners.Where(x => x.source == Owner && x.variant == ((INodeVariant)this).VariantBacking))
            {
                LogicGraph.Traverse(handler.target, ctx.Scope);
            }

            // Cleanup stack
            foreach (var _ in _stackInputs)
            {
                ctx.Stack.Pop();
            }

            return 0;
        }

        public void SetOutputName(string outputName)
        {
            OutputName = outputName;
        }

        public void AddCustomIO(string name, VariableType type)
        {
            var si = new StackInput();
            si.Name = name;
            si.Type = type;
            si.variable = VarHandleContainers.Create(type);
            si.debugLastValue = VarHandleContainers.Create(type);
            _stackInputs.Add(si);
        }

        public void RemoveCustomIO(int index)
        {
            _stackInputs.RemoveAt(index);
        }

        public void UpdateCustomIO(int index, string name, VariableType type)
        {
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

        bool INodeCustomIO.CanAddAndRemove => true;
        public INodeCustomIO.Type CustomIOType => INodeCustomIO.Type.Inputs;
    }
}
