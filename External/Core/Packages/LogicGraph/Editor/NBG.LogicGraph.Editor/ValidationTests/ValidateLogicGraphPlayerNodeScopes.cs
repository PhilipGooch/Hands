using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.LogicGraph.Editor
{
    public class ValidateLogicGraphPlayerNodeScopes : ValidationTest
    {
        public override string Name => "Nodes have no scope link errors";
        public override string Category => "LogicGraph Player";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
        public override bool CanAssist => false;
        public override bool CanFix => false;

        private int _errorCount;
        private int _totalCount;

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
        }

        protected override Result OnRun(ILevel level)
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (LogicGraphPlayer.EnableScopes)
            {
                foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
                {
                    CheckLogicGraphPlayersRecursive(rootGO);
                }

                return Result.FromCount(_errorCount, _totalCount);
            }
            else
            {
                return Result.FromCount(0, 0);
            }
#pragma warning restore CS0162 // Unreachable code detected
        }

        private void CheckLogicGraphPlayersRecursive(GameObject root)
        {
            // Self
            var lgp = root.GetComponent<LogicGraphPlayer>();
            if (lgp != null)
            {
                CheckLogicGraphPlayer(lgp);
                ++_totalCount;
            }

            // Children
            for (int i = 0; i < root.transform.childCount; i++)
            {
                CheckLogicGraphPlayersRecursive(root.transform.GetChild(i).gameObject);
            }
        }

        private void CheckLogicGraphPlayer(LogicGraphPlayer lgp)
        {
            var graph = lgp.Graph;
            if (graph == null)
                return;

            foreach (var pair in graph.Nodes)
            {
                var node = (Node)pair.Value;
                if (node.Scope == NodeAPIScope.Generic)
                    continue;

                var visited = new List<SerializableGuid>(64);
                visited.Add(pair.Key);

                foreach (var fo in node.FlowOutputs)
                {
                    var otherNode = FindNodeWithADifferentScopeRecursive(graph, node.Scope, fo.refNodeGuid, visited);
                    if (otherNode != null)
                    {
                        PrintError($"{node.Name} (scope: {node.Scope}) connects to {otherNode.Name} (scope: {otherNode.Scope})", lgp);
                        ++_errorCount;
                    }
                }

                foreach (var si in node.StackInputs)
                {
                    var otherNode = FindNodeWithADifferentScopeRecursive(graph, node.Scope, si.refNodeGuid, visited);
                    if (otherNode != null)
                    {
                        PrintError($"{node.Name} (scope: {node.Scope}) connects to {otherNode.Name} (scope: {otherNode.Scope})", lgp);
                        ++_errorCount;
                    }
                }
            }
        }

        static Node FindNodeWithADifferentScopeRecursive(ILogicGraph graph, NodeAPIScope expectedScope, SerializableGuid startGuid, List<SerializableGuid> visited)
        {
            if (startGuid == SerializableGuid.empty)
                return null;

            if (visited.Contains(startGuid))
                return null;
            visited.Add(startGuid);

            var node = (Node)graph.GetNode(startGuid);
            if (node.Scope != NodeAPIScope.Generic && node.Scope != expectedScope)
                return node;

            foreach (var fo in node.FlowOutputs)
            {
                var otherNode = FindNodeWithADifferentScopeRecursive(graph, expectedScope, fo.refNodeGuid, visited);
                if (otherNode != null)
                    return otherNode;
            }

            foreach (var si in node.StackInputs)
            {
                var otherNode = FindNodeWithADifferentScopeRecursive(graph, expectedScope, si.refNodeGuid, visited);
                if (otherNode != null)
                    return otherNode;
            }

            return null;
        }
    }
}
