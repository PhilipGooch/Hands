using NBG.LogicGraph.Nodes;
using NBG.LogicGraph.Tests;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace NBG.LogicGraph.Editor.Tests
{
    [NodeHideInUI]
    public class LogicGraphTestHelperParent : MonoBehaviour
    {
        [NodeAPI("Action0 happened", scope: NodeAPIScope.Sim)]
        public event Action OnAction0;
        public void TriggerOnAction0()
        {
            OnAction0?.Invoke();
        }
    }

    [NodeHideInUI]
    public class LogicGraphTestHelper : LogicGraphTestHelperParent
    {
        public int number;

        [NodeAPI("Action1 happened", scope: NodeAPIScope.Sim)]
        public event Action<int> OnAction1;
        public void TriggerOnAction1(int arg1)
        {
            OnAction1?.Invoke(arg1);
        }

        [NodeAPI("Action2 happened", scope: NodeAPIScope.Sim)]
        public event Action<int, bool> OnAction2;
        public void TriggerOnAction2(int arg1, bool arg2)
        {
            OnAction2?.Invoke(arg1, arg2);
        }

        [NodeAPI("Action3 happened", scope: NodeAPIScope.Sim)]
        public event Action<UnityEngine.Object> OnAction3;
        public void TriggerOnAction3(UnityEngine.Object arg1)
        {
            OnAction3?.Invoke(arg1);
        }

        [NodeAPI("Action4 happened", scope: NodeAPIScope.Sim)]
        public event Action<LogicGraphTestHelper> OnAction4;
        public void TriggerOnAction4(LogicGraphTestHelper arg1)
        {
            OnAction4?.Invoke(arg1);
        }

        [NodeAPI("Action5 happened", scope: NodeAPIScope.Sim)]
        public event Action<LogicGraphTestHelper, int> OnAction5;
        public void TriggerOnAction5(LogicGraphTestHelper arg1, int arg2)
        {
            OnAction5?.Invoke(arg1, arg2);
        }

        public LogicGraphTestHelper()
        {
            OnAction1 += PrintInt;
        }

        [NodeAPI("Dummy")]
        public void PrintDummy()
        {
            Debug.Log($"{nameof(PrintDummy)} works.");
        }

        [NodeAPI("Prints a number")]
        public void PrintInt(int value)
        {
            Debug.Log($"{nameof(PrintInt)} works: {value}");
        }

        [NodeAPI("Prints a bool")]
        public void PrintBool(bool value)
        {
            Debug.Log($"{nameof(PrintBool)} works: {value}");
        }

        [NodeAPI("Prints a string")]
        public void PrintString(string value)
        {
            Debug.Log($"{nameof(PrintString)} works: {value}");
        }

        [NodeAPI("Prints a float")]
        public void PrintFloat(float value)
        {
            Debug.Log($"{nameof(PrintFloat)} works: {value}");
        }

        [NodeAPI("Prints an object")]
        public void PrintObject(UnityEngine.Object obj)
        {
            Debug.Log($"{nameof(PrintObject)} works: {obj}");
        }

        [NodeAPI("Always returns false")]
        public bool ReturnFalse()
        {
            return false;
        }

        [NodeAPI("Sets a number")]
        public void SetNumber(int number)
        {
            Debug.Log($"{nameof(SetNumber)} works: {number}");
            this.number = number;
        }

        [NodeAPI("Gets a number")]
        public int GetNumber()
        {
            Debug.Log($"{nameof(GetNumber)} works: {number}");
            return number;
        }

        [NodeAPI("Gets several outputs")]
        public int GetSeveralOutputs(out float b, out bool c)
        {
            Debug.Log($"{nameof(GetSeveralOutputs)} works: {number}");
            b = 2.0f;
            c = true;
            return number;
        }

        [NodeAPI("x * 2")]
        public static int Double(int x)
        {
            Debug.Log($"{nameof(Double)} works: {x} * 2 = {x * 2}");
            return x * 2;
        }

        [NodeAPI("TestParameterOrder")]
        public static int TestParameterOrder(int a, bool b, string c)
        {
            Debug.Log($"{nameof(TestParameterOrder)} works: {a}, {b}, {c}");
            return a;
        }

        [NodeAPI("TestUnityObject")]
        public UnityEngine.Object TestUnityObject(UnityEngine.Object obj)
        {
            Debug.Log($"{nameof(TestUnityObject)} works: {obj}");
            return obj;
        }

        [NodeAPI("TestUnityObjectDerivative")]
        public LogicGraphTestHelper TestUnityObjectDerivative(LogicGraphTestHelper obj)
        {
            Debug.Log($"{nameof(TestUnityObjectDerivative)} works: {obj}");
            return obj;
        }

        [NodeAPI("TestFlowWithOutput", flags: NodeAPIFlags.ForceFlowNode)]
        public void TestFlowWithOutput(int arg1, bool arg2, out float out1, out string out2)
        {
            Debug.Log($"{nameof(TestFlowWithOutput)} works: {arg1} {arg2}");
            out1 = (float)arg1;
            out2 = $"{arg1} {arg2}";
        }
    }

    [NodeHideInUI]
    public static class LogicGraphTestHelperExtensions
    {
        [NodeAPI("Sets a number to X")]
        public static void SetToX(this LogicGraphTestHelper helper, int arg1)
        {
            helper.number = arg1;
            Debug.Log($"{nameof(SetToX)} works: {arg1}");
        }
    }

    public class LogicGraphTests
    {
        GameObject gameObjectParent;
        GameObject gameObjectChild;
        LogicGraph graphParent;
        LogicGraph graphChild;

        IEnumerable<UserlandBinding> ubs;
        LogicGraphTestHelper helper;

        int _stackCountOnInit;
        int _executionContextStackCountOnInit;

        [SetUp]
        public void Init()
        {
            _stackCountOnInit = ((Stack)StackBindings.GetForCurrentThread()).Count;
            _executionContextStackCountOnInit = ExecutionContextBindings.GetForCurrentThread().Frames.Count;

            gameObjectParent = new GameObject("parent");
            var containerParent = gameObjectParent.AddComponent<MockLogicGraphContainer>();
            graphParent = new LogicGraph(containerParent);
            containerParent.Graph = graphParent;

            gameObjectChild = new GameObject("child");
            gameObjectChild.transform.parent = gameObjectParent.transform;
            var containerChild = gameObjectChild.AddComponent<MockLogicGraphContainer>();
            graphChild = new LogicGraph(containerChild);
            containerChild.Graph = graphChild;

            ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            helper = gameObjectChild.AddComponent<LogicGraphTestHelper>();
        }

        [TearDown]
        public void Cleanup()
        {
            Assert.IsTrue(ExecutionContextBindings.GetForCurrentThread().Frames.Count == _executionContextStackCountOnInit, $"ExecutionContextStack has {ExecutionContextBindings.GetForCurrentThread().Frames.Count} entries during cleanup, expected {_executionContextStackCountOnInit}.");
            Assert.IsTrue(((Stack)StackBindings.GetForCurrentThread()).Count == _stackCountOnInit, $"Stack has {((Stack)StackBindings.GetForCurrentThread()).Count} entries during cleanup, expected {_stackCountOnInit}.");

            helper = null;
            ubs = null;

            graphChild.OnDisable();
            graphChild = null;
            UnityEngine.Object.DestroyImmediate(gameObjectChild);
            gameObjectChild = null;

            graphParent.OnDisable();
            graphParent = null;
            UnityEngine.Object.DestroyImmediate(gameObjectParent);
            gameObjectParent = null;

            Assert.IsTrue(EventNode._listeners.Count == 0, "EventNode listeners were not cleaned up properly");
        }

        [Test]
        public void LogicGraph_Traversal_Works()
        {
            // Initialization
            var node0 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node0.StackInputs[0].variable.Get(0).Set<int>(1);

            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node1.StackInputs[0].variable.Get(0).Set<int>(3);

            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node2.StackInputs[0].variable.Get(0).Set<int>(7);

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {3}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {7}");
            LogicGraph.Traverse(node0, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_BranchNode_Works()
        {
            // Initialization
            var node0 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.ReturnFalse)));

            var node1 = graphChild.CreateNode<BranchNode>();

            var arg1 = "when true";
            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
            node2.StackInputs[0].variable.Get(0).Set<string>(arg1);

            var arg2 = "when false";
            var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
            node3.StackInputs[0].variable.Get(0).Set<string>(arg2);

            // Linking
            node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node1.StackInputs[0].refIndex = 0;
            node1.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);
            node1.FlowOutputs[1].refNodeGuid = graphChild.GetNodeId(node3);

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: {arg2}");
            LogicGraph.Traverse(node1, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_CustomOutputHandlerNode_Works()
        {
            // Initialization
            Node startingNode;
            int arg1 = 13;
            bool arg2 = true;

            // Child
            CustomOutputNode childNode0;
            {
                var node0 = childNode0 = graphChild.CreateNode<CustomOutputNode>();
                node0.SetOutputName("OnTest");
                node0.AddCustomIO("arg1", VariableType.Int);
                node0.AddCustomIO("arg2", VariableType.Bool);
                node0.StackInputs[0].variable.Get(0).Set<int>(arg1);
                node0.StackInputs[1].variable.Get(0).Set<bool>(arg2);

                startingNode = node0;
            }

            // Parent
            {
                var node0 = graphParent.CreateNode<HandleCustomOutputNode>((UnityEngine.Object)graphChild.Container);
                node0.SetVariant(childNode0);

                var node1 = graphParent.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
                var node2 = graphParent.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintBool)));

                // Linking
                node0.FlowOutputs[0].refNodeGuid = graphParent.GetNodeId(node1);
                node1.FlowOutputs[0].refNodeGuid = graphParent.GetNodeId(node2);

                node1.StackInputs[0].refNodeGuid = graphParent.GetNodeId(node0);
                node1.StackInputs[0].refIndex = 0;

                node2.StackInputs[0].refNodeGuid = graphParent.GetNodeId(node0);
                node2.StackInputs[0].refIndex = 1;
            }

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintBool)} works: {arg2}");
            LogicGraph.Traverse(startingNode, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();

            graphParent.OnDisable();
            graphChild.OnDisable();
        }

        [Test]
        public void LogicGraph_CustomEventNodeHandlerNode_Works()
        {
            // Child
            CustomEventNode childNode0;
            {
                var node0 = childNode0 = graphChild.CreateNode<CustomEventNode>();
                node0.SetEventName("OnTest");
                node0.AddCustomIO("arg1", VariableType.Int);
                node0.AddCustomIO("arg2", VariableType.Bool);

                var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
                var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintBool)));

                // Linking
                node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
                node1.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);

                node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
                node1.StackInputs[0].refIndex = 0;

                node2.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
                node2.StackInputs[0].refIndex = 1;
            }

            Node startingNode;
            int arg1 = 13;
            bool arg2 = true;

            // Parent
            {
                var node0 = graphParent.CreateNode<CallCustomEventNode>((UnityEngine.Object)graphChild.Container);
                node0.SetVariant(childNode0);
                node0.StackInputs[0].variable.Get(0).Set<int>(arg1);
                node0.StackInputs[1].variable.Get(0).Set<bool>(arg2);

                startingNode = node0;
            }

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintBool)} works: {arg2}");
            LogicGraph.Traverse(startingNode, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_CustomGetterNodeHandlerNode_Works()
        {
            // Child
            CustomGetterNode childNode0;
            {
                var node0 = childNode0 = graphChild.CreateNode<CustomGetterNode>();
                node0.SetOutputName("Value");
                node0.UpdateCustomIO(0, "test", VariableType.Int);

                var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.GetNumber)));
                
                // Linking
                node0.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node1);
                node0.StackInputs[0].refIndex = 0;
            }

            Node startingNode;
            int arg1 = 42;
            helper.number = arg1;

            // Parent
            {
                var node0 = graphParent.CreateNode<HandleCustomGetterNode>((UnityEngine.Object)graphChild.Container);
                node0.SetVariant(childNode0);
                
                var node1 = graphParent.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
                node1.StackInputs[0].refNodeGuid = graphParent.GetNodeId(node0);
                node1.StackInputs[0].refIndex = 0;

                startingNode = node1;
            }

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.GetNumber)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogicGraph.Traverse(startingNode, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_VariableNode_Works()
        {
            var arg1 = 7;

            var variableId = graphChild.AddVariable("Var1", VariableType.Int);
            var var1 = ((LogicGraphVariable)graphChild.Variables[variableId]);
            var1.variable.Get(0).Set<int>(arg1);

            // Initialization
            var node0 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));

            var node1 = graphChild.CreateNode<VariableNode>();
            node1.VariableId = variableId;

            // Linking
            node0.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node0.StackInputs[0].refIndex = 0;

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogicGraph.Traverse(node0, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_InitializesCorrectly()
        {
            {
                var node = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.SetNumber)));
                Assert.IsTrue(node.HasFlowInput == true);
                Assert.IsTrue(node.FlowOutputs.Count == 1);
                Assert.IsTrue(node.StackInputs.Count == 1);
                Assert.IsTrue(node.StackOutputs.Count == 0);
            }

            {
                var node = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.GetNumber)));
                Assert.IsTrue(node.HasFlowInput == false);
                Assert.IsTrue(node.FlowOutputs.Count == 0);
                Assert.IsTrue(node.StackInputs.Count == 0);
                Assert.IsTrue(node.StackOutputs.Count == 1);
            }
        }

        [Test]
        public void LogicGraph_FunctionNode_flow_Works()
        {
            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.SetNumber));
            Assert.IsNotNull(ub);

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);

            // Execution
            const int arg1 = 7;
            node.StackInputs[0].variable.Get(0).Set<int>(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.SetNumber)} works: {arg1}");
            LogicGraph.Traverse(node, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_flow_with_output_Works()
        {
            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestFlowWithOutput));
            Assert.IsNotNull(ub);

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);

            // Execution
            const int arg1 = 7;
            const bool arg2 = true;
            node.StackInputs[0].variable.Get(0).Set<int>(arg1);
            node.StackInputs[1].variable.Get(0).Set<bool>(arg2);

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestFlowWithOutput)} works: {arg1} {arg2}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            var ret1 = ctx.Stack.Peek(0).Get<float>();
            var ret2 = ctx.Stack.Peek(1).Get<string>();
            Assert.IsTrue(ret1 == (float)arg1);
            Assert.IsTrue(ret2 == $"{arg1} {arg2}");

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_flow_with_output_WorksInTraversal()
        {
            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestFlowWithOutput));
            Assert.IsNotNull(ub);

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);
            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintFloat)));
            var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));

            // Linking
            node2.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node);
            node2.StackInputs[0].refIndex = 0;
            node3.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node);
            node3.StackInputs[0].refIndex = 1;

            node.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);
            node2.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node3);

            // Execution
            const int arg1 = 7;
            const bool arg2 = true;
            node.StackInputs[0].variable.Get(0).Set<int>(arg1);
            node.StackInputs[1].variable.Get(0).Set<bool>(arg2);

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestFlowWithOutput)} works: {arg1} {arg2}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintFloat)} works: {(float)arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: {arg1} {arg2}");
            LogicGraph.Traverse(node, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_data_Works()
        {
            var ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.GetNumber));
            Assert.IsNotNull(ub);

            const int arg1 = 13;

            // Initialization
            helper.number = arg1;
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.GetNumber)} works: {arg1}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            var ret = ctx.Stack.PopInt();
            Assert.IsTrue(arg1 == ret);

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_UnityEngineObjectParameterWorks()
        {
            var ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestUnityObject));
            Assert.IsNotNull(ub);

            GameObject arg1 = gameObjectParent;

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);
            node.StackInputs[0].variable.Get(0).Set<UnityEngine.Object>(arg1);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestUnityObject)} works: {arg1}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            var ret = ctx.Stack.PopObject();
            Assert.IsTrue(arg1 == ret);

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_UnityEngineObjectDerivativeParameterWorks()
        {
            var ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestUnityObjectDerivative));
            Assert.IsNotNull(ub);

            LogicGraphTestHelper arg1 = helper;

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);
            node.StackInputs[0].variable.Get(0).Set<UnityEngine.Object>(arg1);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestUnityObjectDerivative)} works: {arg1}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            var ret = ctx.Stack.PopObject();
            Assert.IsTrue(arg1 == ret);

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_extension_Works()
        {
            var ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelperExtensions.SetToX));
            Assert.IsNotNull(ub);

            const int arg1 = 13;

            // Initialization
            helper.number = 0;
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);
            node.StackInputs[0].variable.Get(0).Set<int>(arg1);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelperExtensions.SetToX)} works: {arg1}");
            LogicGraph.Traverse(node, NodeAPIScope.Sim);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_data_multiple_returns_Works()
        {
            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.GetSeveralOutputs));
            Assert.IsNotNull(ub);

            const int arg1 = 13;

            // Initialization
            helper.number = arg1;
            var node = graphChild.CreateNode<FunctionNode>(helper, ub);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.GetSeveralOutputs)} works: {arg1}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            // Reverse order
            var c = ctx.Stack.PopBool();
            Assert.IsTrue(c == true);
            var b = ctx.Stack.PopFloat();
            Assert.IsTrue(b == 2.0f);
            var ret = ctx.Stack.PopInt();
            Assert.IsTrue(arg1 == ret);

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_variable_reference_Works()
        {
            // Consts
            const int arg1 = 7;

            // Initialization
            helper.number = arg1;

            var node0 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.SetNumber)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.GetSeveralOutputs)));
            var node2 = graphChild.CreateNode<FunctionNode>(null, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.Double)));

            // Linking (GetSeveralOutputs[0] => Double => SetNumber)
            node0.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node2);
            node0.StackInputs[0].refIndex = 0;

            node2.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node2.StackInputs[0].refIndex = 0;

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.GetSeveralOutputs)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.Double)} works: {arg1} * 2 = {arg1 * 2}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.SetNumber)} works: {arg1 * 2}");
            LogicGraph.Traverse(node0, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesCorrectly()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintDummy)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintDummy)} works.");
            helper.TriggerOnAction0();
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesOnlyTheTargettedEvent()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintDummy)));
            var node2 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction1)));
            var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintDummy)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node2.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node3);

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintDummy)} works.");
            helper.TriggerOnAction0();
            LogAssert.NoUnexpectedReceived();

            int arg1 = 7;
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintDummy)} works.");
            helper.TriggerOnAction1(arg1);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesUnityEngineObjectEventCorrectly()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction3)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintObject)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node1.StackInputs[0].refIndex = 0;

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            UnityEngine.Object arg1 = gameObjectParent;
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintObject)} works: {arg1}");
            helper.TriggerOnAction3(arg1);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesUnityEngineObjectDerivativeEventCorrectly()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction4)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintObject)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node1.StackInputs[0].refIndex = 0;

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            LogicGraphTestHelper arg1 = helper;
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintObject)} works: {arg1}");
            helper.TriggerOnAction4(arg1);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesUnityEngineObjectDerivativeWithExtraArgEventCorrectly()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction5)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintObject)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node1.StackInputs[0].refIndex = 0;

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            LogicGraphTestHelper arg1 = helper;
            int arg2 = 7;
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintObject)} works: {arg1}");
            helper.TriggerOnAction5(arg1, arg2);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_correct_stack_output_order()
        {
            const int arg1 = 13;
            const bool arg2 = true;

            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction2)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintBool)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);

            node1.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node1.StackInputs[0].refIndex = 0;

            node2.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node0);
            node2.StackInputs[0].refIndex = 1;

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintBool)} works: {arg2}");
            helper.TriggerOnAction2(arg1, arg2);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_TargetsAllBoundNodesOnce()
        {
            // Child
            {
                var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
                var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
                node1.StackInputs[0].variable.Get(0).Set<string>("child");

                // Linking
                node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            }

            // Parent
            {
                var node0 = graphParent.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
                var node1 = graphParent.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
                node1.StackInputs[0].variable.Get(0).Set<string>("parent");

                // Linking
                node0.FlowOutputs[0].refNodeGuid = graphParent.GetNodeId(node1);
            }

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: parent");
            helper.TriggerOnAction0();
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_UnbindingOneTargetLeavesOthersBound()
        {
            // Child
            INode nodeToRemove;
            {
                var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
                var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
                node1.StackInputs[0].variable.Get(0).Set<string>("child 1");

                var node2 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
                var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
                node3.StackInputs[0].variable.Get(0).Set<string>("child 2");
                nodeToRemove = node2;

                var node4 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
                var node5 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
                node5.StackInputs[0].variable.Get(0).Set<string>("child 3");

                // Linking
                node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
                node2.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node3);
                node4.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node5);
            }

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child 1");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child 2");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child 3");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child 1");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: child 3");

            helper.TriggerOnAction0();
            graphChild.RemoveNode(graphChild.GetNodeId(nodeToRemove));
            helper.TriggerOnAction0();

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutionLeavesStackClear()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction1)));

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;
            int arg1 = 7;

            graphChild.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg1}");
            helper.TriggerOnAction1(arg1);
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_DoesNotExecuteIfLogicGraphIsDisabled()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintDummy)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();
            graphChild.OnDisable();

            helper.TriggerOnAction0();
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_EventNode_ExecutesIfLogicGraphIsReenabled()
        {
            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction0)));
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintDummy)));

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);

            // Execution
            var stack = (Stack)StackBindings.GetForCurrentThread();
            var stackPosition = stack.Count;

            graphChild.OnEnable();
            graphChild.OnDisable();
            graphChild.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintDummy)} works.");
            helper.TriggerOnAction0();
            LogAssert.NoUnexpectedReceived();

            Assert.IsTrue(stack.Count == stackPosition, $"Stack has {stack.Count} entries more after execution, expected {stackPosition}.");
        }

        [Test]
        public void LogicGraph_GroupNode_Works()
        {
            // Initialization
            var node0 = graphChild.CreateNode<GroupNode>();

            var node1 = graphChild.CreateNode<CommentNode>();
            var node1id = graphChild.GetNodeId(node1);
            var node2 = graphChild.CreateNode<CommentNode>();
            var node2id = graphChild.GetNodeId(node2);

            node0.AddChild(node1id);
            node0.AddChild(node2id);
            Assert.IsTrue(node0.Children.Count() == 2);
            Assert.IsTrue(node0.Children.Contains(node1id));
            Assert.IsTrue(node0.Children.Contains(node2id));

            node0.RemoveChild(node1id);
            Assert.IsTrue(!node0.Children.Contains(node1id));
        }

        [Test]
        public void LogicGraph_GroupNode_HandlesDuplicates()
        {
            // Initialization
            var node0 = graphChild.CreateNode<GroupNode>();
            var node0id = graphChild.GetNodeId(node0);

            var node1 = graphChild.CreateNode<CommentNode>();
            var node1id = graphChild.GetNodeId(node1);

            node0.AddChild(node1id);
            Assert.IsTrue(node0.Children.Count() == 1);
            try
            {
                node0.AddChild(node1id);
                Assert.Fail("No exception");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("duplicate"));
            }
            Assert.IsTrue(node0.Children.Count() == 1);
            node0.RemoveChild(node1id);
            Assert.IsTrue(node0.Children.Count() == 0);
        }

        [Test]
        public void LogicGraph_GroupNode_WillNotAddSelf()
        {
            // Initialization
            var node0 = graphChild.CreateNode<GroupNode>();
            var node0id = graphChild.GetNodeId(node0);

            Assert.IsTrue(node0.Children.Count() == 0);
            try
            {
                node0.AddChild(node0id);
                Assert.Fail("No exception");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("itself"));
            }
            Assert.IsTrue(node0.Children.Count() == 0);
        }

        [Test]
        public void LogicGraph_SequenceNode_Works()
        {
            // Initialization
            var node0 = graphChild.CreateNode<SequenceNode>();
            var node0id = graphChild.GetNodeId(node0);

            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node1.StackInputs[0].variable.Get(0).Set<int>(0);
            var node1id = graphChild.GetNodeId(node1);

            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node2.StackInputs[0].variable.Get(0).Set<int>(1);
            var node2id = graphChild.GetNodeId(node2);

            var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintInt)));
            node3.StackInputs[0].variable.Get(0).Set<int>(2);
            var node3id = graphChild.GetNodeId(node3);

            // Linking
            node0.AddCustomFlow();
            node0.AddCustomFlow();
            node0.AddCustomFlow();

            node0.FlowOutputs[0].refNodeGuid = node3id;
            node0.FlowOutputs[1].refNodeGuid = node1id;
            node0.FlowOutputs[2].refNodeGuid = node2id;

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: 2");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: 0");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: 1");
            LogicGraph.Traverse(node0, NodeAPIScope.Sim);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_FunctionNode_correct_stack_input_order()
        {
            var ubs = UserlandBindings.Get(typeof(LogicGraphTestHelper));
            Assert.IsNotNull(ubs);

            var ub = ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestParameterOrder));
            Assert.IsNotNull(ub);

            const int arg1 = 13;
            const bool arg2 = true;
            const string arg3 = "!ok!";

            // Initialization
            var node = graphChild.CreateNode<FunctionNode>(null, ub);
            node.StackInputs[0].variable.Get(0).Set<int>(arg1);
            node.StackInputs[1].variable.Get(0).Set<bool>(arg2);
            node.StackInputs[2].variable.Get(0).Set<string>(arg3);

            // Execution
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestParameterOrder)} works: {arg1}, {arg2}, {arg3}");
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            node.Execute(ctx);

            var ret = ctx.Stack.PopInt();
            Assert.IsTrue(arg1 == ret);

            ctx.Pop();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LogicGraph_DelayNode_Works()
        {
            float duration = 1.0f;

            // Initialization
            var node0 = graphChild.CreateNode<UpdateEventNode>();
            var node0id = graphChild.GetNodeId(node0);

            var node1 = graphChild.CreateNode<DelayNode>();
            node1.StackInputs[0].variable.Get(0).Set<float>(duration);
            var node1id = graphChild.GetNodeId(node1);

            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
            node2.StackInputs[0].variable.Get(0).Set<string>("Started");
            var node2id = graphChild.GetNodeId(node2);

            var node3 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
            node3.StackInputs[0].variable.Get(0).Set<string>("Completed");
            var node3id = graphChild.GetNodeId(node3);

            // Linking
            node0.FlowOutputs[0].refNodeGuid = node1id;
            node1.FlowOutputs[0].refNodeGuid = node2id;
            node1.FlowOutputs[1].refNodeGuid = node3id;

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: Started");
            graphChild.OnUpdate(0.05f, true); // Process UpdateEventNode here
            Debug.Assert(node1.IsRunning);
            yield return null;
            LogAssert.NoUnexpectedReceived();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: Completed");
            float timer = 0.0f;
            int timeoutIterations = 30;
            while (timer < duration && timeoutIterations-- > 0)
            {
                graphChild.OnUpdate(0.05f, false); // Do not process UpdateEventNode here
                if (!node1.IsRunning)
                    break;
                timer += Time.deltaTime;
                yield return null;
            }
            Debug.Assert(timeoutIterations > 0);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogicGraph_ExecutionContext_ClearWorks()
        {
            var node0 = graphChild.CreateNode<UpdateEventNode>();
            var node1 = graphChild.CreateNode<CommentNode>();

            var ctx = new ExecutionContext();
            ctx.Push(node0);
            ctx.Push(node1);
            ctx.Stack.Push<int>(0);
            ctx.Stack.Push<bool>(false);
            ctx.Stack.Push<string>(string.Empty);
            Debug.Assert(ctx.Frames.Count == 2);
            Debug.Assert(ctx.Stack.Count == 3);

            ctx.Clear();
            Debug.Assert(ctx.Frames.Count == 0);
            Debug.Assert(ctx.Stack.Count == 0);
        }

        [Test]
        public void LogicGraph_ExecutionContext_DuplicateWorks()
        {
            var node0 = graphChild.CreateNode<UpdateEventNode>();
            var node1 = graphChild.CreateNode<CommentNode>();

            var ctx = new ExecutionContext();
            ctx.Push(node0);
            ctx.Push(node1);
            ctx.Stack.Push<int>(0);
            ctx.Stack.Push<bool>(false);
            ctx.Stack.Push<string>(string.Empty);
            Debug.Assert(ctx.Frames.Count == 2);
            Debug.Assert(ctx.Stack.Count == 3);

            var ctx2 = new ExecutionContext();
            ctx2.Duplicate(ctx);

            Debug.Assert(ctx.Stack.Count == ctx2.Stack.Count);
            if (ctx.Stack.Count == ctx2.Stack.Count)
            {
                for (int i = 0; i < ctx.Stack.Count; ++i)
                {
                    var h0 = ctx.Stack.Peek(i);
                    var h1 = ctx2.Stack.Peek(i);
                    Debug.Assert(h0.Container.CompareHandleValues(h0, h1));
                }
            }

            Debug.Assert(ctx.Frames.Count == ctx2.Frames.Count);
            if (ctx.Stack.Count == ctx2.Stack.Count)
            {
                for (int i = 0; i < ctx.Frames.Count; ++i)
                {
                    Debug.Assert(ctx.Frames[i].Equals(ctx2.Frames[i]));
                }
            }
        }

        [UnityTest]
        public IEnumerator LogicGraph_DelayNode_ContextSwitchesCorrectly()
        {
            float duration = 1.0f;

            // Initialization
            var node0 = graphChild.CreateNode<EventNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.OnAction1)));

            var arg1 = 1;
            var arg2 = false;
            var node1 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestFlowWithOutput)));
            node1.StackInputs[0].variable.Get(0).Set<int>(arg1);
            node1.StackInputs[1].variable.Get(0).Set<bool>(arg2);

            var arg3 = 3;
            var arg4 = true;
            var node2 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.TestFlowWithOutput)));
            node2.StackInputs[0].variable.Get(0).Set<int>(arg3);
            node2.StackInputs[1].variable.Get(0).Set<bool>(arg4);

            var node3 = graphChild.CreateNode<DelayNode>();
            node3.StackInputs[0].variable.Get(0).Set<float>(duration);

            var node4 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintFloat)));
            node4.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node4.StackInputs[0].refIndex = 0;

            var node5 = graphChild.CreateNode<FunctionNode>(helper, ubs.Single(x => x.Name == nameof(LogicGraphTestHelper.PrintString)));
            node5.StackInputs[0].refNodeGuid = graphChild.GetNodeId(node2);
            node5.StackInputs[0].refIndex = 1;

            // Linking
            node0.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node1);
            node1.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node2);
            node2.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node3);
            node3.FlowOutputs[1].refNodeGuid = graphChild.GetNodeId(node4); // output 1 for "Completed"
            node4.FlowOutputs[0].refNodeGuid = graphChild.GetNodeId(node5);

            // Execution
            graphChild.OnEnable();
            graphParent.OnEnable();

            int arg5 = 13;
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintInt)} works: {arg5}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestFlowWithOutput)} works: {arg1} {arg2}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.TestFlowWithOutput)} works: {arg3} {arg4}");
            helper.TriggerOnAction1(arg5);
            Debug.Assert(node3.IsRunning);
            yield return null;
            LogAssert.NoUnexpectedReceived();

            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintFloat)} works: {(float)arg1}");
            LogAssert.Expect(LogType.Log, $"{nameof(LogicGraphTestHelper.PrintString)} works: {arg3} {arg4}");
            float timer = 0.0f;
            int timeoutIterations = 30;
            while (timer < duration && timeoutIterations-- > 0)
            {
                graphChild.OnUpdate(0.05f, true);
                if (!node3.IsRunning)
                    break;
                timer += Time.deltaTime;
                yield return null;
            }
            Debug.Assert(timeoutIterations > 0);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
