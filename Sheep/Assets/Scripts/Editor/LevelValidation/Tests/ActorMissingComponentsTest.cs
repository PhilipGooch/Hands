using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using UnityEngine;

public class ActorMissingComponentsTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict | ValidationTestCaps.ChecksProject;

    public override string Name => "There are no actors which are missing collider or rigidbody";
    public override string Category => "Actor";

    protected override Result OnRun(ILevel level)
    {
        List<ActorComponent> violations = FindViolations(level);

        var result = new Result();

        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    List<ActorComponent> FindViolations(ILevel level)
    {
        List<ActorComponent> violations = new List<ActorComponent>();
        foreach (var root in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
        {
            foreach (var actor in root.GetComponentsInChildren<ActorComponent>())
            {
                if (actor.GetComponentInChildren<Collider>() == null)
                {
                    violations.Add(actor);
                    PrintLog($"Actor {actor.name} doesnt have a collider!", actor);
                }
                if (actor.GetComponentInChildren<Rigidbody>() == null)
                {
                    PrintLog($"Actor {actor.name} doesnt have a rigidbody!", actor);
                    if (!violations.Contains(actor))
                        violations.Add(actor);
                }
            }
        }

        return violations;
    }
}
