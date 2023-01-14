using NBG.Core;
using System.Collections.Generic;
using System.Diagnostics;

namespace NBG.LogicGraph
{
    /// <summary>
    /// LogicGraph stack frame.
    /// One for every node on the callstack.
    /// </summary>
    internal readonly struct StackFrame
    {
        public INode Entry { get; }
        public ILogicGraph Graph { get; }
        public int StackBottom { get; }

        public StackFrame(INode entryNode, int stackBottom)
        {
            Entry = entryNode;
            Graph = ((Node)entryNode).Owner;
            StackBottom = stackBottom;
        }
    }

    static class ExecutionContextBindings
    {
        [ClearOnReload(newInstance: true)]
        static ExecutionContext _globalContext = new ExecutionContext(); // This could be ThreadStatic
        public static ExecutionContext GetForCurrentThread()
        {
            return _globalContext;
        }
    }

    internal class ExecutionContext
    {
        public Stack Stack { get; } = new Stack();

        List<StackFrame> _frames = new List<StackFrame>();
        public IReadOnlyList<StackFrame> Frames => _frames;

        private NodeAPIScope _executionScope = NodeAPIScope.Sim;
        public NodeAPIScope Scope
        {
            get => _executionScope;
            set
            {
                if (_executionScope != value)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    if (LogicGraphPlayer.EnableScopes)
                    {
                        if (_frames.Count != 0)
                            throw new System.InvalidOperationException("ExecutionContext can't change execution scope with StackFrames present.");
                    }
#pragma warning restore CS0162 // Unreachable code detected
                    _executionScope = value;
                }
            }
        }
        
        public StackFrame Push(INode node)
        {
            var newNode = (Node)node;
            var newScope = newNode.Scope;

#pragma warning disable CS0162 // Unreachable code detected
            if (LogicGraphPlayer.EnableScopes)
            {
                if (_frames.Count == 0)
                {
                    Scope = newScope;
                }
                else
                {
                    if (newScope != NodeAPIScope.Generic && newScope != _executionScope)
                        throw new System.Exception($"LogicGraph execution context was in {_executionScope} scope, however the next node '{node.Name}' is in {newScope} scope.");
                }
            }
            else
            {
                Scope = newScope;
            }
#pragma warning restore CS0162 // Unreachable code detected

            var ctx = new StackFrame(node, Stack.Count);
            _frames.Add(ctx);
            return ctx;
        }

        public void Pop(bool cleanupStack = true)
        {
            var lastIndex = (_frames.Count - 1);
            if (cleanupStack)
            {
                var frame = _frames[lastIndex];
                while (Stack.Count > frame.StackBottom)
                    Stack.Pop();
            }
            _frames.RemoveAt(lastIndex);
        }

        public StackFrame Peek()
        {
            var lastIndex = (_frames.Count - 1);
            var ctx = _frames[lastIndex];
            return ctx;
        }

        public StackFrame Last(INode node)
        {
            var lastIndex = _frames.FindLastIndex(x => x.Entry == node);
            var ctx = _frames[lastIndex];
            return ctx;
        }

        public void Clear()
        {
            while (_frames.Count > 0)
                Pop(true);
            while (Stack.Count > 0)
                Stack.Pop(); // Clean up values which might be behind the first stack frame, when originating in generated bindings
        }

        public void Duplicate(ExecutionContext source)
        {
            Debug.Assert(Stack.Count == 0);
            Debug.Assert(_frames.Count == 0);

            Scope = source.Scope;

            var srcStack = source.Stack;
            for (int i = 0; i < srcStack.Count; ++i)
            {
                Stack.PushCopy(srcStack, i);
            }

            IReadOnlyList<StackFrame> sourceFrames = source._frames;
            for (int i = 0; i < sourceFrames.Count; ++i)
            {
                _frames.Add(sourceFrames[i]);
            }
        }
    }
}
