using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NonConvexCollidersOnNonStaticObjectsTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no non static objects with non convex mesh colliders";
    public override string Category => "Performance";

    protected override Result OnRun(ILevel level)
    {
        List<MeshCollider> violations = FindViolations(level);

        var result = new Result();

        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    List<MeshCollider> FindViolations(ILevel level)
    {
        List<MeshCollider> violations = new List<MeshCollider>();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            var colliders = root.GetComponentsInChildren<MeshCollider>().Where(x => x.convex == false && x.gameObject.isStatic == false);
            foreach (var collider in colliders)
            {
                // Objects without rigidbodies can have non-convex colliders
                var hasRigidbody = collider.attachedRigidbody != null;
                if (hasRigidbody)
                {
                    PrintLog("Object should either be static or have its colliders set to convex", collider);
                    violations.Add(collider);
                }
            }
        }

        return violations;
    }
}
