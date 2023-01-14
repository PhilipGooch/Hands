namespace NBG.LogicGraph
{
    interface IVarHandlePoolContainer
    {
        int Alloc();
        void Free(ref VarHandle handle);

        VarHandle AllocCopy(VarHandle sourceHandle);
    }

    class VarHandlePoolContainer<T> : IVarHandleContainer, IVarHandlePoolContainer, IVarHandleContainerTyped<T>
    {
        readonly int _capacity;
        readonly T[] _vars;
        readonly bool[] _allocated;

        public VariableType Type => VariableTypes.FromSystemType(typeof(T));

        public VarHandlePoolContainer(int capacity)
        {
            _capacity = capacity;
            _vars = new T[capacity];
            _allocated = new bool[capacity];
        }

        public VarHandle Get(int index)
        {
            var handle = new VarHandle();
            handle.Container = this;
            handle.Index = index;
            return handle;
        }

        public string GetAsString(int index)
        {
            return _vars[index]?.ToString();
        }

        public T GetValue(int index)
        {
            return _vars[index];
        }

        public void SetValue(int index, T value)
        {
            _vars[index] = value;
        }

        public void ValidateHandle(VarHandle handle)
        {
            if (handle.Container != this)
                throw new System.Exception($"Handle is not from this {nameof(VarHandleContainer<T>)}.");
            if (handle.Index < 0 || handle.Index >= _capacity)
                throw new System.Exception($"{nameof(VarHandleContainer<T>)} handle index is out of range: {handle.Index}");
            if (!_allocated[handle.Index])
                throw new System.Exception($"{nameof(VarHandleContainer<T>)} handle is not allocated: {handle.Index}");
        }

        public int Alloc()
        {
            for (int i = 0; i < _capacity; ++i)
            {
                if (_allocated[i])
                    continue;

                _allocated[i] = true;
                return i;
            }

            throw new System.Exception($"VarHandlePoolContainer<{Type}> is out of space!");
        }

        public void Free(ref VarHandle handle)
        {
            ValidateHandle(handle);
            _allocated[handle.Index] = false;
            _vars[handle.Index] = default;

            handle = VarHandle.Invalid;
        }

        public VarHandle AllocCopy(VarHandle sourceHandle)
        {
            var handle = new VarHandle();
            handle.Container = this;
            handle.Index = Alloc();
            _vars[handle.Index] = sourceHandle.Get<T>();
            return handle;
        }

        public bool CompareHandleValues(VarHandle lhs, VarHandle rhs) // Only for tests
        {
            var lhsValue = lhs.Get<T>();
            var rhsValue = rhs.Get<T>();
            return lhsValue.Equals(rhsValue);
        }
    }

    class VarHandlePoolContainers
    {
        VarHandlePoolContainer<bool> _bools;
        VarHandlePoolContainer<int> _ints;
        VarHandlePoolContainer<float> _floats;
        VarHandlePoolContainer<string> _strings;
        VarHandlePoolContainer<UnityEngine.Vector3> _vectors;
        VarHandlePoolContainer<UnityEngine.Object> _objects;
        VarHandlePoolContainer<UnityEngine.Quaternion> _quaternions;
        VarHandlePoolContainer<UnityEngine.Color> _colors;

        IVarHandlePoolContainer[] _containers;

        public VarHandlePoolContainers(int capacity)
        {
            _bools = new VarHandlePoolContainer<bool>(capacity);
            _ints = new VarHandlePoolContainer<int>(capacity);
            _floats = new VarHandlePoolContainer<float>(capacity);
            _strings = new VarHandlePoolContainer<string>(capacity);
            _vectors = new VarHandlePoolContainer<UnityEngine.Vector3>(capacity);
            _objects = new VarHandlePoolContainer<UnityEngine.Object>(capacity);
            _quaternions = new VarHandlePoolContainer<UnityEngine.Quaternion>(capacity);
            _colors = new VarHandlePoolContainer<UnityEngine.Color>(capacity);

            _containers = new IVarHandlePoolContainer[] // Order must match VariableType
            {
                _bools,
                _ints,
                _floats,
                _strings,
                _vectors,
                _objects,
                null,
                _quaternions,
                _colors,
            };
        }

        public IVarHandlePoolContainer GetPool(System.Type type)
        {
            if (type == typeof(bool))
                return _bools;
            else if (type == typeof(int))
                return _ints;
            if (type == typeof(float))
                return _floats;
            if (type == typeof(string))
                return _strings;
            if (type == typeof(UnityEngine.Vector3))
                return _vectors;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) //TODO: maybe IsSubclassOf?
                return _objects;
            if (type == typeof(UnityEngine.Quaternion))
                return _quaternions;
            if (type == typeof(UnityEngine.Color))
                return _colors;
            throw new System.NotSupportedException();
        }

        public IVarHandlePoolContainer GetPool(VariableType type)
        {
            return _containers[(int)type];
        }
    }
}
