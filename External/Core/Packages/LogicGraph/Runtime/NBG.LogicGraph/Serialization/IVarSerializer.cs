namespace NBG.LogicGraph.Serialization
{
    interface IVarSerializer
    {
        string Serialize(ISerializationContext ctx, VarHandle handle);
        void Deserialize(IDeserializationContext ctx, ref VarHandle handle, string data);
    }

    interface IVarSerializerTyped<T>
    {
        string Serialize(ISerializationContext ctx, T data);
        T Deserialize(IDeserializationContext ctx, string data);
    }

    static class VariableTypeSerializers
    {
        static IVarSerializer[] _serializers = new IVarSerializer[] // Order must match VariableType
        {
            new VarSerializerBool(),
            new VarSerializerInt32(),
            new VarSerializerSingle(),
            new VarSerializerString(),
            new VarSerializerUnityVector3(),
            new VarSerializerUnityObject(),
            new VarSerializerGuid(),
            new VarSerializerUnityQuaternion(),
            new VarSerializerUnityColor(),
        };

        public static IVarSerializer GetSerializer(VariableType type)
        {
            return _serializers[(int)type];
        }
    }
}
