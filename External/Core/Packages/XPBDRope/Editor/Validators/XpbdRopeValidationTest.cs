using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core.Editor;
using NBG.Core;
using UnityEditor;
using System.Linq;

namespace NBG.XPBDRope
{
    public class XpbdRopeValidator : ValidationTest
    {
        public override string Name => "XPBD Rope Version Validation";

        public override string Category => "Prefab & Scene";

        public override string AssistTooltip => "Rebuild all outdated ropes";

        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
        public override bool CanAssist => canAssist;

        public override bool CanFix => false;

        bool canAssist = false;

        protected override Result OnRun(ILevel context)
        {
            int count = 0;
            int errors = 0;
            int outdatedRopes = 0;
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(context))
            {
                var connectors = rootGO.GetComponentsInChildren<ConnectToRopeEndLEGACY>(true);
                foreach (var connector in connectors)
                {
                    PrintError("Detected a legacy ConnectToRopeEnd script.", connector);
                    errors++;
                }

                var ropes = rootGO.GetComponentsInChildren<Rope>(true);
                count += ropes.Length;
                foreach(var rope in ropes)
                {
                    if (rope.IsOutdated && rope.IsBuilt)
                    {
                        PrintError($"Detected old rope version. Expected version {Rope.latestRopeVersion} but found {rope.Version}", rope);
                        errors++;
                        outdatedRopes++;
                    }
                }
            }

            canAssist = outdatedRopes > 0;

            return Result.FromCount(errors, count);
        }

        protected override void OnAssist(ILevel context)
        {
            AssetDatabase.StartAssetEditing();
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(context))
            {
                var ropes = rootGO.GetComponentsInChildren<Rope>(true);
                foreach (var rope in ropes)
                {
                    if (rope.IsOutdated && rope.IsBuilt)
                    {
                        RopeBuilder.BuildRope(rope);
                        PrintLog($"Rebuilt rope {rope.name} to the latest version.", rope);
                        ValidationTests.SaveChangesInContext();
                    }
                }
            }
            AssetDatabase.StopAssetEditing();
        }

        protected override void OnFix(ILevel context)
        {
            AssetDatabase.StartAssetEditing();
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(context))
            {
                var connectors = rootGO.GetComponentsInChildren<ConnectToRopeEndLEGACY>(true);
                foreach (var connector in connectors)
                {
                    var rope = connector.target;
                    var joint = connector.joint;

                    if (rope != null)
                    {
                        // The rope direction will be flipped, since the rope extends from the start
                        rope.AttachEndTo = rope.AttachStartTo;
                        if (joint != null)
                        {
                            rope.AttachStartTo = connector.joint.GetComponent<Rigidbody>();
                        }

                        rope.Handles = rope.Handles.Reverse().ToArray();

                        if (rope.IsBuilt)
                        {
                            RopeBuilder.BuildRope(rope);
                        }

                        PrintLog($"Fixed rope connection to object for {rope.name}.", rope);
                    }
                    if (joint != null)
                    {
                        GameObject.DestroyImmediate(connector.joint);
                    }

                    GameObject.DestroyImmediate(connector);
                    ValidationTests.SaveChangesInContext();
                }

                var ropes = rootGO.GetComponentsInChildren<Rope>(true);
                foreach (var rope in ropes)
                {
                    if (rope.IsOutdated && rope.IsBuilt)
                    {
                        RopeBuilder.BuildRope(rope);
                        PrintLog($"Rebuilt rope {rope.name} to the latest version.", rope);
                        ValidationTests.SaveChangesInContext();
                    }
                }
            }
            AssetDatabase.StopAssetEditing();
        }
    }
}

