namespace NBG.LogicGraph.Serialization
{
    class VarSerializerString : IVarSerializer, IVarSerializerTyped<string>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<string>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, string value)
        {
            return value;
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<string>(Deserialize(ctx, data));
        }

        public string Deserialize(IDeserializationContext ctx, string data)
        {
            return data;
        }
    }
}
