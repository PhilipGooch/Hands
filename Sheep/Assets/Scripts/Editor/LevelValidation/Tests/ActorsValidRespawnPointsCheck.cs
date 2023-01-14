using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActorsValidRespawnPointsCheck : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;

    public override string Name => "There are no actors which cannot spawn in assigned respawn points";
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
        List<BoxBounds> bounds = new List<BoxBounds>();
        List<ActorComponent> violations = new List<ActorComponent>();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            //should it check if the object is enabled? 
            var actors = root.GetComponentsInChildren<ActorComponent>().Where(x => x.Respawns);

            var toSkip = new List<GameObject>();
            toSkip.AddRange(actors.Select(x => x.gameObject));

            foreach (var actor in actors)
            {
                bounds.Clear();
                actor.CollectBoundsEditor(bounds);

                foreach (var respawnPoint in actor.AllowedRespawnPoints)
                {
                    bool canSpawn = new EmptySpaceChecker(respawnPoint.transform).CheckIfFits(bounds, toSkip);
                    if (!canSpawn)
                    {
                        PrintLog("Object cannot spawn in at least one of the assigned spawn points", actor);
                        violations.Add(actor);
                    }
                }
            }
        }

        return violations;
    }
}
