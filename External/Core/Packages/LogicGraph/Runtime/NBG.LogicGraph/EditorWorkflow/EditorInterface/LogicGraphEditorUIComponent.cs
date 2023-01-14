using System;

namespace NBG.LogicGraph.EditorInterface
{
    class Component : IComponent
    {
        public INodeContainer Owner { get; internal set; }
        public ulong InstanceId { get; private set; }
        public ComponentDirection Direction { get; internal set; }
        public ComponentLink Link { get; internal set; }
        public string Name { get; internal set; }
        public bool Hidden { get; internal set; }
        public bool IsFlowInput => (Direction == ComponentDirection.Input && Link == ComponentLink.Flow);
        public bool IsLinkOwner => GetIsLinkOwner();
        public ComponentDataType DataType { get; internal set; }
        public Type BackingType { get; internal set; }
        public IVariantProvider VariantProvider { get; internal set; }
        public IComponent Target { get; internal set; }
        public bool SupportsMultipleConnections { get; internal set; }

        public IVarHandleContainer Value { get; internal set; }
        public IVarHandleContainer DebugLastValue { get; internal set; }

        bool GetIsLinkOwner()
        {
            return (Direction == ComponentDirection.Input && Link == ComponentLink.Data) ||
                (Direction == ComponentDirection.Output && Link == ComponentLink.Flow);
        }

        public Component()
        {
            InstanceId = ++_instanceIdGenerator;
        }

        internal object _userData;
        internal Func<int> _getUserDataSlotIndex; // Determines the slot position for the type of component in user data
        internal int _userDataRefIndex = -1; // Only used for stack output (so that stack input knows what to link)

        static ulong _instanceIdGenerator = 0;
    }
}
