using NBG.Core;
using System;
using System.Collections.Generic;

namespace NBG.LogicGraph
{
    [Flags]
    enum StackEntryFlags
    {
        None = 0,
        StackAllocated = (1 << 0),
    }

    class StackEntry
    {
        public StackEntryFlags Flags;
        public VarHandle Handle;
    }

    public static class StackBindings
    {
        public static IStack GetForCurrentThread()
        {
            return ExecutionContextBindings.GetForCurrentThread().Stack;
        }
    }

    internal class Stack : IStack
    {
        const int kContainerSize = 512;
        static VarHandlePoolContainers _vars = new VarHandlePoolContainers(kContainerSize);
        [ExecuteOnReload]
        static void OnReload()
        {
            _vars = new VarHandlePoolContainers(kContainerSize);
        }

        List<StackEntry> _stack = new List<StackEntry>();

        public int Count { get { return _stack.Count; } }

        public void PushCopy(Stack source, int index)
        {
            var srcHandle = source.Peek(index);
            var varType = srcHandle.Container.Type;
            var pool = _vars.GetPool(varType);
            var handle = pool.AllocCopy(srcHandle);
            _stack.Add(new StackEntry() { Flags = StackEntryFlags.StackAllocated, Handle = handle });
        }

        // Allocates a variable handle with the given value and puts it on stack.
        // Stack now has ownership of the handle.
        public void Push<T>(T value)
        {
            var pool = _vars.GetPool(typeof(T));
            var handle = new VarHandle();
            handle.Alloc(pool);
            handle.Set(value);
            _stack.Add(new StackEntry() { Flags = StackEntryFlags.StackAllocated, Handle = handle });
        }

        // Returns the value of the top variable handle on stack, and removes that handle from stack.
        // Deallocates the handle if it owned by stack.
        public T Pop<T>()
        {
            var entry = _stack[_stack.Count - 1];
            var handle = entry.Handle;
            var value = handle.Get<T>();
            _stack.RemoveAt(_stack.Count - 1);
            if (entry.Flags.HasFlag(StackEntryFlags.StackAllocated))
                handle.Free();
            return value;
        }

        public void Pop() => Pop(0);

        // Removes a variable handle from stack at a given offset from stack top.
        // Deallocates the handle if it owned by stack.
        public void Pop(int offset)
        {
            var index = _stack.Count - 1 - offset;
            var entry = _stack[index];
            var handle = entry.Handle;
            _stack.RemoveAt(index);
            if (entry.Flags.HasFlag(StackEntryFlags.StackAllocated))
                handle.Free();
        }

        // Puts a variable handle on top of the stack.
        // Stack does not get ownership of the handle.
        public void Place(VariableType type, VarHandle handle)
        {
            UnityEngine.Assertions.Assert.IsTrue(handle.Container.Type == type, $"Trying to Place a {nameof(VarHandle)} of {handle.Container.Type} as a {type} container.");
            _stack.Add(new StackEntry() { Flags = StackEntryFlags.None, Handle = handle });
        }

        // Gets the handle at absolute stack index, without modifying the stack.
        public VarHandle Peek(int index)
        {
            return _stack[index].Handle;
        }

        public void PushBool(bool value) => Push<bool>(value);
        public bool PopBool() => Pop<bool>();
        public void PushInt(int value) => Push<int>(value);
        public int PopInt() => Pop<int>();
        public void PushFloat(float value) => Push<float>(value);
        public float PopFloat() => Pop<float>();
        public void PushString(string value) => Push<string>(value);
        public string PopString() => Pop<string>();
        public void PushVector3(UnityEngine.Vector3 value) => Push<UnityEngine.Vector3>(value);
        public UnityEngine.Vector3 PopVector3() => Pop<UnityEngine.Vector3>();
        public void PushObject(UnityEngine.Object value) => Push<UnityEngine.Object>(value);
        public UnityEngine.Object PopObject() => Pop<UnityEngine.Object>();

        public void PushQuaternion(UnityEngine.Quaternion value) => Push<UnityEngine.Quaternion>(value);
        public UnityEngine.Quaternion PopQuaternion() => Pop<UnityEngine.Quaternion>();

        public void PushColor(UnityEngine.Color value) => Push<UnityEngine.Color>(value);
        public UnityEngine.Color PopColor() => Pop<UnityEngine.Color>();
    }
}
