using NBG.Core;

namespace NBG.LogicGraph
{
    interface INodeVariant
    {
        SerializableGuid VariantBacking { get; }
        string VariantName { get; }
        System.Type VariantHandler { get; }
    }
}
