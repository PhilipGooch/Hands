using NBG.Core;
using NBG.LogicGraph.Serialization;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Variable types supported by the LogicGraph.
    /// </summary>
    public enum VariableType
    {
        Bool,
        Int,
        Float,
        String,
        UnityVector3,
        UnityObject,
        Guid,
        Quaternion,
        Color,
    }

    static class VariableTypes
    {
        public static VariableType FromSystemType(System.Type type)
        {
            if (type == typeof(bool))
                return VariableType.Bool;
            else if (type == typeof(int))
                return VariableType.Int;
            if (type == typeof(float))
                return VariableType.Float;
            if (type == typeof(string))
                return VariableType.String;
            if (type == typeof(UnityEngine.Vector3))
                return VariableType.UnityVector3;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) //TODO: maybe IsSubclassOf?
                return VariableType.UnityObject;
            if (type == typeof(SerializableGuid))
                return VariableType.Guid;
            if (type == typeof(UnityEngine.Quaternion))
                return VariableType.Quaternion;
            if (type == typeof(UnityEngine.Color))
                return VariableType.Color;
            throw new System.NotSupportedException($"Variable type '{type.FullName}' is not supported.");
        }
    }

    /// <summary>
    /// Handle to a variable stored in a container.
    /// </summary>
    internal struct VarHandle
    {
        const int InvalidIndex = -1;

        /// <summary>
        /// Owning container.
        /// </summary>
        public IVarHandleContainer Container { get; internal set; }

        public int Index { get; internal set; }

        /// <summary>
        /// Null handle.
        /// </summary>
        public static VarHandle Invalid
        {
            get
            {
                var handle = new VarHandle();
                handle.Index = InvalidIndex;
                return handle;
            }
        }

        internal void Alloc(IVarHandlePoolContainer pool)
        {
            if (Container != null)
                throw new System.InvalidOperationException("Trying to allocate a handle which is already allocated.");
            Container = (IVarHandleContainer)pool;
            Index = pool.Alloc();
        }

        public T Get<T>()
        {
            try
            {
                var typed = (IVarHandleContainerTyped<T>)Container;
                return typed.GetValue(Index);
            }
            catch (System.InvalidCastException)
            {
                throw new InvalidVariableTypeException($"Trying to get a {nameof(VarHandle)} value of {typeof(T)} from a {Container.Type} container.");
            }
        }

        public void Set<T>(T value)
        {
            try
            {
                var typed = (IVarHandleContainerTyped<T>)Container;
                typed.SetValue(Index, value);
            }
            catch (System.InvalidCastException)
            {
                throw new InvalidVariableTypeException($"Trying to set a {nameof(VarHandle)} value of {typeof(T)} in a {Container.Type} container.");
            }
        }

        public void Free()
        {
            if (Container != null)
                ((IVarHandlePoolContainer)Container).Free(ref this);
        }

        internal string Serialize(ISerializationContext ctx)
        {
            Container.ValidateHandle(this);
            var serializer = VariableTypeSerializers.GetSerializer(Container.Type);
            return serializer.Serialize(ctx, this);
        }

        internal void Deserialize(IDeserializationContext ctx, string data)
        {
            var serializer = VariableTypeSerializers.GetSerializer(Container.Type);
            serializer.Deserialize(ctx, ref this, data);
        }
    }
}
