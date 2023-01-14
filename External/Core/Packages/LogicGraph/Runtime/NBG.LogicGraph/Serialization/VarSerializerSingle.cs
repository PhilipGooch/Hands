using System.Globalization;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerSingle : IVarSerializer, IVarSerializerTyped<float>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<float>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<float>(Deserialize(ctx, data));
        }

        public float Deserialize(IDeserializationContext ctx, string data)
        {
            var value = float.Parse(data, CultureInfo.InvariantCulture);
            return value;
        }
    }
}
