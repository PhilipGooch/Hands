using NBG.Core;
using NBG.Core.Editor;
using UnityEngine;

namespace NBG.LogicGraph.Editor
{
    public class ValidateLogicGraphPlayerNodes : ValidationTest
    {
        public override string Name => "Nodes have no errors";
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
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                CheckLogicGraphPlayersRecursive(rootGO);
            }

            return Result.FromCount(_errorCount, _totalCount);
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

            foreach (var node in graph.Nodes.Values)
            {
                var validation = node as INodeValidation;
                if (validation == null)
                    continue;

                var error = validation.CheckForErrors();
                if (error == null)
                    continue;

                PrintError($"{node.Name}: {error}", lgp);
                ++_errorCount;
            }
        }
    }
}
