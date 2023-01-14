namespace NBG.LogicGraph.Serialization
{
    class VarSerializerBool : IVarSerializer, IVarSerializerTyped<bool>
    {
        public string Serialize(ISerializationContext ctx, VarHandle handle)
        {
            var value = handle.Get<bool>();
            return Serialize(ctx, value);
        }

        public string Serialize(ISerializationContext ctx, bool value)
        {
            return value ? "1" : "0";
        }

        public void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data)
        {
            handle.Set<bool>(Deserialize(ctx, data));
        }

        public bool Deserialize(IDeserializationContext ctx, string data)
        {
            //TODO: validate check data
            if (data == "0")
                return false;
            else
                return true;
        }
    }
}
