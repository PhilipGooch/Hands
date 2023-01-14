using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class RigidbodiesCollisionDetectionModeTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;

    public override string Name => "There are no non kinematic grabbable rigidbodies without continuous dynamic/speculative collision detection.";
    public override bool CanAssist => false;
    public override bool CanFix => true;
    public override bool AutoRerunAfterFix => false;

    public override string Category => "Scene";

    List<Rigidbody> viableToChange = new List<Rigidbody>();

    protected override Result OnRun(ILevel level)
    {
        return (FindViolations(level));
    }

    protected override void OnFix(ILevel level)
    {
        FixRigidbodiesCollisionDetection(level);
    }
    Result FindViolations(ILevel level)
    {

        viableToChange.Clear();

        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            Rigidbody[] allRigidbodies = root.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody r in allRigidbodies)
            {
                if (!r.isKinematic && r.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative && r.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic
                    && (r.GetComponentInChildren<GrabParamsBinding>() || r.GetComponentInParent<GrabParamsBinding>()))
                {
                    viableToChange.Add(r);
                    PrintLog($"Collision detection mode set to {r.collisionDetectionMode} ", r);
                }
            }

        }

        Result result = new Result();
        result.Count = viableToChange.Count;
        result.Status = viableToChange.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    void FixRigidbodiesCollisionDetection(ILevel level)
    {
        
        var result = FindViolations(level);

        if (result.Status == ValidationTestStatus.Failure)
        {
            Undo.RecordObjects(viableToChange.ToArray(), "Adjust joint");
            int group = Undo.GetCurrentGroup();
            foreach (Rigidbody r in viableToChange)
            {
                r.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                {
                    GameObject origin = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r.gameObject);
                    Rigidbody rig = origin.GetComponent<Rigidbody>();
                    if (rig != null)
                    {
                        Undo.RecordObject(rig, "Apply prefab");
                        rig.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                }
            }
            Undo.CollapseUndoOperations(group);
        }
    }
}
