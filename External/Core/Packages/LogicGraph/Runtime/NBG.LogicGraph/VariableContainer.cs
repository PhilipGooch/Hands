using NBG.Core;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Generic access to a variable container.
    /// </summary>
    internal interface IVarHandleContainer
    {
        VariableType Type { get; }
        VarHandle Get(int index);
        void ValidateHandle(VarHandle handle);
        bool CompareHandleValues(VarHandle lhs, VarHandle rhs);

        string GetAsString(int index);
    }

    /// <summary>
    /// Typed access to a variable container.
    /// </summary>
    /// <typeparam name="T">System.Type of <c>VariableType</c>.</typeparam>
    internal interface IVarHandleContainerTyped<T>
    {
        T GetValue(int index);
        void SetValue(int index, T value);
    }

    class VarHandleContainer<T> : IVarHandleContainer, IVarHandleContainerTyped<T>
    {
        public T Value { get; internal set; }
        public VariableType Type => VariableTypes.FromSystemType(typeof(T));

        public VarHandle Get(int index)
        {
            if (index != 0)
                throw new System.InvalidOperationException("IVarHandleContainer only tracks one handle.");
            var handle = new VarHandle();
            handle.Container = this;
            handle.Index = 0;
            return handle;
        }

        public string GetAsString(int index)
        {
            return Value?.ToString();
        }

        public T GetValue(int index)
        {
            if (index != 0)
                throw new System.InvalidOperationException("IVarHandleContainer only tracks one handle.");
            return Value;
        }

        public void SetValue(int index, T value)
        {
            if (index != 0)
                throw new System.InvalidOperationException("IVarHandleContainer only tracks one handle.");
            Value = value;
        }

        public void ValidateHandle(VarHandle handle)
        {
            if (handle.Container != this)
                throw new System.Exception($"Handle is not from this {nameof(VarHandleContainer<T>)}.");
            if (handle.Index != 0)
                throw new System.Exception($"{nameof(VarHandleContainer<T>)} handle index is out of range: {handle.Index}");
        }

        public bool CompareHandleValues(VarHandle lhs, VarHandle rhs) // Only for tests
        {
            var lhsValue = lhs.Get<T>();
            var rhsValue = rhs.Get<T>();
            return lhsValue.Equals(rhsValue);
        }
    }

    /// <summary>
    /// Variable container utilities.
    /// </summary>
    static class VarHandleContainers
    {
        public static IVarHandleContainer Create(VariableType type)
        {
            switch (type)
            {
                case VariableType.Bool:
                    return new VarHandleContainer<bool>();
                case VariableType.Int:
                    return new VarHandleContainer<int>();
                case VariableType.Float:
                    return new VarHandleContainer<float>();
                case VariableType.String:
                    return new VarHandleContainer<string>();
                case VariableType.UnityVector3:
                    return new VarHandleContainer<UnityEngine.Vector3>();
                case VariableType.UnityObject:
                    return new VarHandleContainer<UnityEngine.Object>();
                case VariableType.Guid:
                    return new VarHandleContainer<SerializableGuid>();
                case VariableType.Quaternion:
                    return new VarHandleContainer<UnityEngine.Quaternion>();
                case VariableType.Color:
                    return new VarHandleContainer<UnityEngine.Color>();
                default:
                    throw new System.NotSupportedException();
            }
        }

        public static void Copy(VarHandle from, VarHandle to)
        {
            switch (to.Container.Type)
            {
                case VariableType.Bool:
                    Copy<bool>(from, to);
                    break;
                case VariableType.Int:
                    Copy<int>(from, to);
                    break;
                case VariableType.Float:
                    Copy<float>(from, to);
                    break;
                case VariableType.String:
                    Copy<string>(from, to);
                    break;
                case VariableType.UnityVector3:
                    Copy<UnityEngine.Vector3>(from, to);
                    break;
                case VariableType.UnityObject:
                    Copy<UnityEngine.Object>(from, to);
                    break;
                case VariableType.Guid:
                    Copy<SerializableGuid>(from, to);
                    break;
                case VariableType.Quaternion:
                    Copy<UnityEngine.Quaternion>(from, to);
                    break;
                case VariableType.Color:
                    Copy<UnityEngine.Color>(from, to);
                    break;
                default:
                    throw new System.NotSupportedException();
            }
        }

        static void Copy<T>(VarHandle from, VarHandle to)
        {
            var cFrom = ((IVarHandleContainerTyped<T>)from.Container);
            var cTo = ((IVarHandleContainerTyped<T>)to.Container);
            cTo.SetValue(to.Index, cFrom.GetValue(from.Index));
        }
    }
}
