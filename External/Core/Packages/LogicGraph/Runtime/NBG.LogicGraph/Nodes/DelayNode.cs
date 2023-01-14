using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeCategoryPath("Flow")]
    [NodeSerialization("Delay")]
    class DelayNode : FlowControlNode, INodeOnInitialize, INodeOnFixedUpdate, INodeOnUpdate, INodeStateful
    {
        public override string Name
        {
            get
            {
                if (_running)
                {
                    return $"Delay ({_timer.ToString("0.000")} s.)";
                }
                else
                {
                    return "Delay (idle)";
                }
            }
        }

        bool _running = false;
        bool _inFixed;
        float _timer = 0.0f;

        ExecutionContext _ownedCtx = new ExecutionContext();

        internal bool IsRunning => _running; // Only for tests
        internal ExecutionContext OwnedContext => _ownedCtx; // Only for tests

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_stackInputs.Count == 0);

            var fo0 = new FlowOutput();
            fo0.Name = "Started";
            _flowOutputs.Add(fo0);

            var fo1 = new FlowOutput();
            fo1.Name = "Completed";
            _flowOutputs.Add(fo1);

            var si = new StackInput();
            si.Name = "Duration";
            si.Type = VariableType.Float;
            si.variable = VarHandleContainers.Create(VariableType.Float);
            si.debugLastValue = VarHandleContainers.Create(VariableType.Float);
            _stackInputs.Add(si);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            if (!_running)
            {
                _running = true;
                _timer = 0.0f;
                _inFixed = Time.inFixedTimeStep;

                _ownedCtx.Duplicate(ctx);

                // Exit via "Started"
                ctx.Stack.PushInt(0);
                return 1;
            }

            return 0;
        }

        void INodeOnFixedUpdate.OnFixedUpdate(float dt)
        {
            if (_running && _inFixed)
            {
                UpdateTimer(dt);
            }
        }

        void INodeOnUpdate.OnUpdate(float dt)
        {
            if (_running && !_inFixed)
            {
                UpdateTimer(dt);
            }
        }

        void UpdateTimer(float dt)
        {
            _timer += dt;
            var duration = _stackInputs[0].variable.Get(0).Get<float>();
            if (_timer >= duration)
            {
                _running = false;
                DebugLastActivatedFrameIndex = Time.frameCount;

                try
                {
                    // Exit via "Completed"
                    var guid = _flowOutputs[1].refNodeGuid;
                    if (guid != Core.SerializableGuid.empty)
                    {
                        var targetNode = Owner.GetNode(guid);
                        LogicGraph.TraverseWithContext(targetNode, _ownedCtx);
                        _ownedCtx.Clear();
                    }
                }
                catch
                {
                }
            }
        }
    }
}
