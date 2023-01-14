using System.Globalization;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerInt32 : IVarSerializer, IVarSerializerTyped<int>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<int>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<int>(Deserialize(ctx, data));
        }

        public int Deserialize(IDeserializationContext ctx, string data)
        {
            var value = int.Parse(data, CultureInfo.InvariantCulture);
            return value;
        }
    }
}
