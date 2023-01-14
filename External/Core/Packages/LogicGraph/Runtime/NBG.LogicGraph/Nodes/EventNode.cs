using NBG.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    [NodeSerialization("Event")]
    class EventNode : BindingNode, INodeOnEnable, INodeOnDisable, INodeBinding
    {
        private UserlandEventBinding EventBinding => (UserlandEventBinding)binding;
        private Delegate bindingDelegate;

        public override string Name => binding.Description;
        public override NodeAPIScope Scope => EventBinding.Scope;

        public class Listener //TODO: improve and optimize
        {
            public object sender;
            public long senderEventId;
            public ILogicGraph graph;
            public EventNode target;
        }
        [ClearOnReload(newInstance: true)]
        internal static List<Listener> _listeners = new List<Listener>();
        private Listener bindingListener;

        void INodeBinding.OnDeserializedBinding(UserlandBinding binding_)
        {
            var binding = (UserlandEventBinding)binding_;

            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_stackInputs.Count == 0);
            Debug.Assert(_stackOutputs.Count == 0);

            this.binding = binding;

            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);

            var parameters = binding.Source.EventHandlerType.GetMethod("Invoke").GetParameters(); //TODO: optimize - extract during UserlandBinding initialization
            foreach (var p in parameters)
            {
                var so = new StackOutput();
                so.Name = p.Name;
                so.Type = VariableTypes.FromSystemType(p.ParameterType);
                //p.Position //TODO: can't assume parameter list is ordered?
                _stackOutputs.Add(so);
            }

            // Create a delegate
            bindingDelegate = Delegate.CreateDelegate(binding.Source.EventHandlerType, this.ObjectContext, binding.HandlerName);

            // Create a listener
            bindingListener = new Listener();
            bindingListener.sender = this.ObjectContext;
            bindingListener.senderEventId = binding.EventId;
            bindingListener.graph = this.Owner;
            bindingListener.target = this;
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            // Verify stack has args
            var frame = ctx.Peek();
            for (int i = 0; i < _stackOutputs.Count; ++i)
            {
                var reverseIndex = _stackOutputs.Count - i - 1;
                var so = _stackOutputs[reverseIndex];
                var index = frame.StackBottom - 1 - i;
                var handle = ctx.Stack.Peek(index);
                Debug.Assert(handle.Container.Type == so.Type);
            }

            // Nothing to do
            return 0;
        }

        protected override void PlaceOutputOntoStack(ExecutionContext ctx, VariableType type, int refIndex)
        {
            var frame = ctx.Last(this);
            if (frame.Entry != this)
                throw new System.InvalidOperationException($"Trying to get outputs of {this.Name} which is not the entry point of the current execution context.");

            // EventNode outputs come from the binding function and are stack-owned.
            // Lifetime: entire execution of the node chain, originating at this EventNode.
            // Duplicate them on top without taking extra ownership.
            var reverseIndex = _stackOutputs.Count - refIndex - 1;
            var index = frame.StackBottom - 1 - reverseIndex;
            var handle = ctx.Stack.Peek(index);
            ctx.Stack.Place(type, handle);
        }

        void INodeOnEnable.OnEnable()
        {
            // Bind
            Assert.IsNotNull(bindingDelegate);
            // Only one event handler is required, as it is global.
            EventBinding.RemoveMethod.Invoke(this.ObjectContext, new object[] { bindingDelegate });  //TODO: optimize
            EventBinding.AddMethod.Invoke(this.ObjectContext, new object[] { bindingDelegate }); //TODO: optimize

            Assert.IsNotNull(bindingListener);
            _listeners.Add(bindingListener);
        }

        void INodeOnDisable.OnDisable()
        {
            Assert.IsNotNull(bindingListener);
            _listeners.Remove(bindingListener);

            // Unbind
            Assert.IsNotNull(bindingDelegate);
            var stillInUse = _listeners.Any(x => ((UnityEngine.Object)x.sender == this.ObjectContext) && (x.senderEventId == bindingListener.senderEventId));
            if (!stillInUse)
                EventBinding.RemoveMethod.Invoke(this.ObjectContext, new object[] { bindingDelegate }); //TODO: optimize
        }
    }
}
