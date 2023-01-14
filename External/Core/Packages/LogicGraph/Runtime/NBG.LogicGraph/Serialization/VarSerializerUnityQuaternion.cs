using System.Globalization;
using UnityEngine;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerUnityQuaternion : IVarSerializer, IVarSerializerTyped<UnityEngine.Quaternion>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<UnityEngine.Quaternion>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, UnityEngine.Quaternion value)
        {
            var x = value.x.ToString(CultureInfo.InvariantCulture);
            var y = value.y.ToString(CultureInfo.InvariantCulture);
            var z = value.z.ToString(CultureInfo.InvariantCulture);
            var w = value.w.ToString(CultureInfo.InvariantCulture);
            return $"{x},{y},{z},{w}";
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<UnityEngine.Quaternion>(Deserialize(ctx, data));
        }

        public UnityEngine.Quaternion Deserialize(IDeserializationContext ctx, string data)
        {
            var components = data.Split(',');
            var x = float.Parse(components[0], CultureInfo.InvariantCulture);
            var y = float.Parse(components[1], CultureInfo.InvariantCulture);
            var z = float.Parse(components[2], CultureInfo.InvariantCulture);
            var w = float.Parse(components[3], CultureInfo.InvariantCulture);
            return new Quaternion(x, y, z, w);
        }
    }
}
