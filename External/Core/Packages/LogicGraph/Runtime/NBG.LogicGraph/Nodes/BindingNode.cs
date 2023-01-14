using NBG.Core;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace NBG.LogicGraph.Nodes
{
    abstract class BindingNode : Node, INodeObjectContext, INodeValidation
    {
        protected UserlandBinding binding;
        private object bindingTargetInstance;

        public UserlandBinding Binding => binding;

        public UnityEngine.Object ObjectContext
        {
            get => (UnityEngine.Object)bindingTargetInstance;
            set => bindingTargetInstance = value;
        }

        protected override void OnSerialize(ISerializationContext ctx, SerializableNodeEntry entry)
        {
            entry.BindingName = binding.Name;
            entry.BindingType = SerializationUtils.GetSerializableTypeName(binding.TargetType);
            entry.BindingStatic = binding.IsStatic;

            var obj = (UnityEngine.Object)bindingTargetInstance;
            var id = (obj != null) ? ctx.ReferenceUnityObject(obj) : SerializableGuid.empty;
            entry.Target = id;
        }

        protected override string OnDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            // Upgrade binding type name
            var bindingTypeName = SerializationUtils.UpgradeSerializableTypeName(entry.BindingType);
            if (bindingTypeName != entry.BindingType)
                Debug.Log($"{nameof(FunctionNode)} binding type name upgraded from '{entry.BindingType}' to '{bindingTypeName}'");

            var bindingType = Type.GetType(bindingTypeName);
            Assert.IsNotNull(bindingType, $"Failed to determine type for '{bindingTypeName}'");

            // Upgrade binding name
            var bindingName = SerializationUtils.UpgradeSerializableBindingName(bindingType, entry.BindingName);
            if (bindingName != entry.BindingName)
                Debug.Log($"{nameof(FunctionNode)} binding name upgraded from '{entry.BindingName}' to '{bindingName}'");

            // Get binding
            var bindings = UserlandBindings.GetWithAncestors(bindingType);
            //var exists = UserlandBindings.Bindings.TryGetValue(bindingType, out List<UserlandBinding> bindings);
            //Assert.IsTrue(exists, $"Failed to find bindings for '{bindingType.FullName}'");
            var binding = bindings.SingleOrDefault(x => x.Name == bindingName);
            if (binding != null)
            {
                ((INodeBinding)this).OnDeserializedBinding(binding);
                return null;
            }
            else
            {
                return $"Failed to find binding for '{bindingType.FullName}.{bindingName}'";
            }
        }

        string INodeValidation.CheckForErrors()
        {
            if (bindingTargetInstance == null && !binding.IsStatic)
                return $"Target is missing";

            return null;
        }
    }
}
