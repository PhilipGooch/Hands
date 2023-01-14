using NBG.LogicGraph.EditorInterface;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI.Tests
{
    public class LogicGraphUITests
    {
        GameObject gameObjectParent;
        GameObject gameObjectChild;

        LogicGraphPlayer graph;
        LogicGraphWindow window;
        LogicGraphView logicGraphView;
        LogicGraphPlayerEditor activeGraph;

        [SetUp]
        public void Init()
        {
            gameObjectParent = new GameObject("parent");
            graph = gameObjectParent.AddComponent<LogicGraphPlayer>();

            gameObjectChild = new GameObject("child");
            gameObjectChild.transform.parent = gameObjectParent.transform;

            Selection.activeGameObject = gameObjectParent;
            window = LogicGraphWindow.OpenNewNodeGraphWindow();
            logicGraphView = window.GraphView;
            activeGraph = window.ActiveGraph;
        }

        [TearDown]
        public void Cleanup()
        {
            graph = null;
            logicGraphView = null;
            activeGraph = null;
            window.Close();

            UnityEngine.Object.DestroyImmediate(gameObjectChild);
            gameObjectChild = null;

            UnityEngine.Object.DestroyImmediate(gameObjectParent);
            gameObjectParent = null;

            //need to autoclose window
        }

        //nodes tests:
        //create, remove all nodes
        //connect nodes
        //delete one connected node
        //delete both connected nodes
        //delete multiple nodes 
        //add/remove comment node

        //inspector tests:
        //select custom event node

        //blackboard test:
        //create/delete (delete from board, delete from graph) variables of all types

        #region Blackboard Tests
        [Test]
        public void AddBlackboardVariablesToGraphViewThenDeleteThemFromBoard()
        {
            //create variables
            var blackboard = window.BlackboardView;

            CreateAllTypesOfBlackboardVariables();

            //"drag" variables into the graph
            foreach (var item in blackboard.VariableViews)
            {
                logicGraphView.VariableDraggedIn(window.DragView, Vector3.zero, item.variable.ID);
            }

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, blackboard.VariableViews.Count);

            //remove variables from the board
            foreach (var item in blackboard.VariableViews)
            {
                item.DeleteVariable();
            }

            window.UpdateAll(true);
            Assert.AreEqual(blackboard.VariableViews.Count, 0);
        }

        [Test]
        public void AddBlackboardVariablesToGraphViewThenDeleteThemFromGraph()
        {
            CreateAllTypesOfBlackboardVariables();
            RemoveAllNodesAtOnce();

            //check if blackboard variables are still there
            var blackboard = window.BlackboardView;
            Assert.AreEqual(blackboard.VariableViews.Count, Enum.GetNames(typeof(VariableTypeUI)).Length);
        }

        [Test]
        public void AddRemoveAllTypesOfBlackboardVariables()
        {
            //create variables
            var blackboard = window.BlackboardView;

            CreateAllTypesOfBlackboardVariables();

            //remove variables
            foreach (var item in blackboard.VariableViews)
            {
                item.DeleteVariable();
            }

            window.UpdateAll(true);
            Assert.AreEqual(blackboard.VariableViews.Count, 0);
        }

        #endregion

        #region Connections Tests

        [Test]
        public void ConnectNodesDeleteOutput()
        {
            (NodeView node1View, NodeView node2View) = ConnectTwoNodes();

            node1View.Select(logicGraphView, false);
            window.UpdateAll(true);

            logicGraphView.DeleteSelection();
            window.UpdateAll(true);

            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 1);
        }

        [Test]
        public void ConnectNodesDeleteInput()
        {
            (NodeView node1View, NodeView node2View) = ConnectTwoNodes();

            node2View.Select(logicGraphView, false);
            window.UpdateAll(true);
            logicGraphView.DeleteSelection();

            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 1);
        }

        [Test]
        public void ConnectNodesDeleteBoth()
        {
            (NodeView node1View, NodeView node2View) = ConnectTwoNodes();

            node1View.Select(logicGraphView, true);
            node2View.Select(logicGraphView, true);
            window.UpdateAll(true);
            logicGraphView.DeleteSelection();

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 0);
        }

        #endregion

        #region Inspector Tests

        [Test]
        public void SelectCustomEventNode()
        {
            //create node
            NodeEntry eventNodeEntry = GetNodeEntry("CustomEventNode");
            var eventNode = CreateAndGetNode(eventNodeEntry);

            window.UpdateAll(true);

            var nodeView = (NodeView)GetNodeView(eventNode);
            nodeView.Select(logicGraphView, false);
            window.UpdateAll(true);
        }

        #endregion

        #region Node Tests

        [Test]
        public void AddRemoveOneByOneAllNodes()
        {
            //create nodes

            List<INodeContainer> nodes = new List<INodeContainer>();
            foreach (var item in activeGraph.NodesController.NodeTypes)
            {
                var node = CreateAndGetNode(item);
                nodes.Add(node);
            }

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, activeGraph.NodesController.NodeTypes.Count());

            //delete nodes one by one
            foreach (var item in nodes)
            {
                var nodeView = (GraphElement)GetNodeView(item);

                Assert.IsNotNull(nodeView);
                nodeView.Select(logicGraphView, true);
                logicGraphView.DeleteSelection();
                window.UpdateAll(true);
            }

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 0);
        }

        [Test]
        public void AddRemoveAtOnceAllNodes()
        {
            //create nodes

            List<INodeContainer> nodes = new List<INodeContainer>();
            foreach (var item in activeGraph.NodesController.NodeTypes)
            {
                var node = CreateAndGetNode(item);
                nodes.Add(node);
            }

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, activeGraph.NodesController.NodeTypes.Count());

            RemoveAllNodesAtOnce();
        }

        [Test]
        public void AddRemoveCommentNode()
        {
            //create comment
            NodeEntry commentEntry = GetNodeEntry("CommentNode");
            var commentNode = CreateAndGetNode(commentEntry);

            window.UpdateAll(true);

            var commentSerializedNode = GetNodeView(commentNode);
            var commentView = ((CommentView)commentSerializedNode);
            Assert.IsNotNull(commentView);

            //remove comment
            commentView.Select(logicGraphView, false);
            window.UpdateAll(true);
            logicGraphView.DeleteSelection();

            window.UpdateAll(true);

            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 0);
        }

        #endregion

        #region Group tests

        [Test]
        public void AddRemoveElementFromGroup()
        {
            //create nodes
            NodeEntry groupEntry = GetNodeEntry("GroupNode");
            var groupNode = CreateAndGetNode(groupEntry);

            NodeEntry testNodeEntry = GetNodeEntry("BranchNode");
            var testNode = CreateAndGetNode(testNodeEntry);

            window.UpdateAll(true);

            //add node to group
            var groupNodeView = GetNodeView(groupNode);
            var testNodeView = GetNodeView(testNode);

            var groupView = ((GroupView)groupNodeView);
            Assert.IsNotNull(groupView);
            var testNodeGraphElement = (GraphElement)testNodeView;
            Assert.IsNotNull(testNodeGraphElement);
            groupView.OnElementAdded(testNodeGraphElement);

            window.UpdateAll(true);

            //remove node from group
            groupView.OnElementRemoved(testNodeGraphElement);

            window.UpdateAll(true);
        }

        [Test]
        public void DeleteGroupWithNodes()
        {
            //create nodes
            NodeEntry groupEntry = GetNodeEntry("GroupNode");
            var groupNode = CreateAndGetNode(groupEntry);

            NodeEntry testNodeEntry = GetNodeEntry("BranchNode");
            var testNode = CreateAndGetNode(testNodeEntry);

            window.UpdateAll(true);

            //add node to group
            var groupNodeView = GetNodeView(groupNode);
            var testNodeView = GetNodeView(testNode);

            var groupView = ((GroupView)groupNodeView);
            Assert.IsNotNull(groupView);
            var testNodeGraphElement = (GraphElement)testNodeView;
            Assert.IsNotNull(testNodeGraphElement);
            groupView.OnElementAdded(testNodeGraphElement);

            window.UpdateAll(true);

            //remove group
            groupView.Select(logicGraphView, true);
            window.UpdateAll(true);
            logicGraphView.DeleteSelection();

            window.UpdateAll(true);

            Assert.IsNull(logicGraphView.SerializedNodeViews.FirstOrDefault(x => x.Container.ContainerType == ContainerType.Group));
        }

        [Test]
        public void DeleteNodeInGroup()
        {
            //create nodes
            NodeEntry groupEntry = GetNodeEntry("GroupNode");
            var groupNode = CreateAndGetNode(groupEntry);

            NodeEntry testNodeEntry = GetNodeEntry("BranchNode");
            var testNode = CreateAndGetNode(testNodeEntry);

            window.UpdateAll(true);

            //add node to group
            var groupNodeView = GetNodeView(groupNode);
            var testNodeView = GetNodeView(testNode);

            var groupView = ((GroupView)groupNodeView);
            Assert.IsNotNull(groupView);
            var testNodeGraphElement = (GraphElement)testNodeView;
            Assert.IsNotNull(testNodeGraphElement);
            groupView.OnElementAdded(testNodeGraphElement);

            window.UpdateAll(true);

            //remove group
            testNodeGraphElement.Select(logicGraphView, true);
            window.UpdateAll(true);
            logicGraphView.DeleteSelection();

            window.UpdateAll(true);

            Assert.IsNull(logicGraphView.SerializedNodeViews.FirstOrDefault(x => x.Container.ID == testNode.ID));
        }

        #endregion

        //connect two random nodes, in this case two float nodes
        (NodeView node1View, NodeView node2View) ConnectTwoNodes()
        {
            //this a very scetchy way of create two specific nodes, if their name or order would change, this will brake
            //create nodes
            NodeEntry node1Entry = GetNodeEntry("Add");
            INodeContainer node1 = CreateAndGetNode(node1Entry);

            NodeEntry node2Entry = GetNodeEntry("Absolute");
            INodeContainer node2 = CreateAndGetNode(node2Entry);

            window.UpdateAll(true);

            //connect nodes
            var node1View = (NodeView)GetNodeView(node1);
            var node2View = (NodeView)GetNodeView(node2);

            var outputPort = node1View.ports.FirstOrDefault(x => x.Value.Component.Link == ComponentLink.Data && x.Value.Component.Direction == ComponentDirection.Output);
            var inputPort = node2View.ports.FirstOrDefault(x => x.Value.Component.Link == ComponentLink.Data && x.Value.Component.Direction == ComponentDirection.Input);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            activeGraph.NodesController.ConnectNodes(outputPort.Value.Component, inputPort.Value.Component);
            window.UpdateAll(true);

            Assert.AreEqual(logicGraphView.edges.ToList().Count(), 1);

            return (node1View, node2View);
        }

        ISerializedNode GetNodeView(INodeContainer nodeContainer)
        {
            var nodeView = logicGraphView.SerializedNodeViews.FirstOrDefault(x => x.Container.ID == nodeContainer.ID);
            Assert.IsNotNull(nodeView);
            return nodeView;
        }

        NodeEntry GetNodeEntry(string name)
        {
            NodeEntry groupEntry = activeGraph.NodesController.NodeTypes.FirstOrDefault(x => x.description == name);
            Assert.IsNotNull(groupEntry, $"{name} entry not found, maybe the name was changed?");
            return groupEntry;
        }

        INodeContainer CreateAndGetNode(NodeEntry nodeEntry)
        {
            var node = logicGraphView.CreateAndGetNode(new ClickContext(nodeEntry, nodeEntry.reference), Vector2.zero);
            Assert.IsNotNull(node);
            return node;
        }

        void RemoveAllNodesAtOnce()
        {
            foreach (var item in logicGraphView.SerializedNodeViews)
            {
                ((GraphElement)item).Select(logicGraphView, true);
            }
            logicGraphView.DeleteSelection();

            window.UpdateAll(true);
            Assert.AreEqual(logicGraphView.SerializedNodeViews.Count, 0);
        }

        void CreateAllTypesOfBlackboardVariables()
        {
            var blackboard = window.BlackboardView;
            var myEnumMemberCount = Enum.GetNames(typeof(VariableTypeUI)).Length;
            for (int i = 0; i < myEnumMemberCount; i++)
            {
                blackboard.AddNewConstantVariable(EditorInterfaceUtils.VariableTypeUIToVariableType((VariableTypeUI)i));
            }

            window.UpdateAll(true);
            Assert.AreEqual(blackboard.VariableViews.Count, myEnumMemberCount);
        }
    }
}