using System.Globalization;
using UnityEngine;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerUnityColor : IVarSerializer, IVarSerializerTyped<UnityEngine.Color>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<UnityEngine.Color>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, UnityEngine.Color value)
        {
            var r = value.r.ToString(CultureInfo.InvariantCulture);
            var g = value.g.ToString(CultureInfo.InvariantCulture);
            var b = value.b.ToString(CultureInfo.InvariantCulture);
            var a = value.a.ToString(CultureInfo.InvariantCulture);
            return $"{r},{g},{b},{a}";
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<UnityEngine.Color>(Deserialize(ctx, data));
        }

        public UnityEngine.Color Deserialize(IDeserializationContext ctx, string data)
        {
            var components = data.Split(',');
            var r = float.Parse(components[0], CultureInfo.InvariantCulture);
            var g = float.Parse(components[1], CultureInfo.InvariantCulture);
            var b = float.Parse(components[2], CultureInfo.InvariantCulture);
            var a = float.Parse(components[3], CultureInfo.InvariantCulture);
            return new UnityEngine.Color(r, g, b, a);
        }
    }
}
