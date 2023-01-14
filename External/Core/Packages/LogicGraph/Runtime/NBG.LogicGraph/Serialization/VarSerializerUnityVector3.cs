using System.Globalization;
using UnityEngine;

namespace NBG.LogicGraph.Serialization
{
    class VarSerializerUnityVector3 : IVarSerializer, IVarSerializerTyped<UnityEngine.Vector3>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<UnityEngine.Vector3>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, UnityEngine.Vector3 value)
        {
            var x = value.x.ToString(CultureInfo.InvariantCulture);
            var y = value.y.ToString(CultureInfo.InvariantCulture);
            var z = value.z.ToString(CultureInfo.InvariantCulture);
            return $"{x},{y},{z}";
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<UnityEngine.Vector3>(Deserialize(ctx, data));
        }

        public Vector3 Deserialize(IDeserializationContext ctx, string data)
        {
            var components = data.Split(',');
            var x = float.Parse(components[0], CultureInfo.InvariantCulture);
            var y = float.Parse(components[1], CultureInfo.InvariantCulture);
            var z = float.Parse(components[2], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }
    }
}
