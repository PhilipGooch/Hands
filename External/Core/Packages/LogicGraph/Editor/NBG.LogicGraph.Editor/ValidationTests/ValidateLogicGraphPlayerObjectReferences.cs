//#define ENABLE_ValidateLogicGraphPlayerObjectReferences // TODO: discuss if we want scope ownership
#if ENABLE_ValidateLogicGraphPlayerObjectReferences
using NBG.Core;
using NBG.Core.Editor;
using NBG.LogicGraph.Nodes;
using UnityEngine;

namespace NBG.LogicGraph.Editor
{
    public class ValidateLogicGraphPlayerObjectReferences : ValidationTest
    {
        public override string Name => "Object references are valid and are in scope of a parent LogicGraph";
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
                var ctx = node as INodeObjectContext;
                if (ctx == null)
                    continue;

                // BindingNode with a static binding can have null context
                var bindingNode = node as BindingNode;
                if (bindingNode != null && bindingNode.IsStaticBinding)
                {
                    Debug.Assert(ctx.ObjectContext == null);
                    continue;
                }

                // Reference is missing
                if (ctx.ObjectContext == null)
                {
                    PrintError($"{lgp.gameObject.GetFullPath()} has a missing object reference on node '{node.Name}'", lgp);
                    ++_errorCount;
                    continue;
                }

                // Reference is not a direct child
                var component = (UnityEngine.Component)ctx.ObjectContext;
                var reference = component.transform;
                while (reference != null)
                {
                    if (reference.gameObject == lgp.gameObject)
                    {
                        break;
                    }
                    else if (reference != component.transform && reference.GetComponent<LogicGraphPlayer>() != null)
                    {
                        PrintError($"Out-of-scope object reference to {component} on node '{node.Name}'. {reference.gameObject.GetFullPath()} owns the scope.", lgp);
                        ++_errorCount;
                        break;
                    }

                    reference = reference.parent;
                    if (reference == null)
                    {
                        PrintError($"Out-of-scope object reference to {component} on node '{node.Name}'", lgp);
                        ++_errorCount;
                        break;
                    }
                }
            }
        }
    }
}
#endif //ENABLE_ValidateLogicGraphPlayerObjectReferences
