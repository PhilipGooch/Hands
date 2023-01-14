using NBG.Core;
using System.Globalization;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerGuid : IVarSerializer, IVarSerializerTyped<SerializableGuid>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<SerializableGuid>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, SerializableGuid value)
        {
            return Serialize(value);
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<SerializableGuid>(Deserialize(ctx, data));
        }

        public SerializableGuid Deserialize(IDeserializationContext ctx, string data)
        {
            return Deserialize(data);
        }



        public static string Serialize(SerializableGuid value)
        {
            var low = value.Low.ToString(CultureInfo.InvariantCulture);
            var high = value.High.ToString(CultureInfo.InvariantCulture);
            return $"{low},{high}";
        }

        public static SerializableGuid Deserialize(string data)
        {
            var components = data.Split(',');
            var low = ulong.Parse(components[0], CultureInfo.InvariantCulture);
            var high = ulong.Parse(components[1], CultureInfo.InvariantCulture);
            var guid = new SerializableGuid(low, high);
            return guid;
        }
    }
}
