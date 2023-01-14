using NBG.Core;
using System.Globalization;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerUnityObject : IVarSerializer, IVarSerializerTyped<UnityEngine.Object>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<UnityEngine.Object>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, UnityEngine.Object value)
        {
            var guid = (value == null) ? SerializableGuid.empty : ctx.ReferenceUnityObject(value);
            var low = guid.Low.ToString(CultureInfo.InvariantCulture);
            var high = guid.High.ToString(CultureInfo.InvariantCulture);
            return $"{low},{high}";
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<UnityEngine.Object>(Deserialize(ctx, data));
        }

        public UnityEngine.Object Deserialize(IDeserializationContext ctx, string data)
        {
            var components = data.Split(',');
            var low = ulong.Parse(components[0], CultureInfo.InvariantCulture);
            var high = ulong.Parse(components[1], CultureInfo.InvariantCulture);
            var guid = new SerializableGuid(low, high);
            var obj = ctx.GetUnityObject(guid);
            return obj;
        }
    }
}
