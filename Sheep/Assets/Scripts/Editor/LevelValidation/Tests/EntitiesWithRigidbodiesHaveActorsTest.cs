using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitiesWithRigidbodiesHaveActorsTest : ValidationTest
{
    public override string Name => "There are no entities with rigidbodies that don't have an actor parent.";

    public override string Category => "Actor";

    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;

    protected override Result OnRun(ILevel context)
    {
        var violations = FindViolations(context);

        var result = new Result();
        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;
        return result;
    }

    List<InteractableEntity> FindViolations(ILevel level)
    {
        var violations = new List<InteractableEntity>();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            var entities = root.GetComponentsInChildren<InteractableEntity>();
            foreach(var ent in entities)
            {
                var parentRig = ent.GetComponentInParent<Rigidbody>();
                var parentActor = ent.GetComponentInParent<ActorComponent>();
                if (parentRig != null && !parentRig.isKinematic && parentActor == null)
                {
                    PrintLog("Rigidbody has interactable entity components, but no ActorComponent to handle their respawning.", parentRig);
                    violations.Add(ent);
                }
            }
        }
        return violations;
    }
}
