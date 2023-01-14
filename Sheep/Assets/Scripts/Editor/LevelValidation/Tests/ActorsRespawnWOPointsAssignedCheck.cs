using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActorsRespawnWOPointsAssignedCheck : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;

    public override string Name => "There are no actors with no assigned respawn points, which cannot spawn anywhere";
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
            var actors = root.GetComponentsInChildren<ActorComponent>().Where(
                x => x.Respawns &&
                x.gameObject.activeInHierarchy &&
                (x.AllowedRespawnPoints == null || x.AllowedRespawnPoints.Length == 0));

            var toSkip = new List<GameObject>();
            toSkip.AddRange(actors.Select(x => x.gameObject));

            foreach (var actor in actors)
            {
                bounds.Clear();
                actor.CollectBoundsEditor(bounds);

                bool viableSpotFound = false;
                foreach (var respawnPoint in Object.FindObjectsOfType<ObjectRespawnPoint>())
                {
                    bool canSpawn = new EmptySpaceChecker(respawnPoint.transform).CheckIfFits(bounds, toSkip);
                    if (canSpawn)
                    {
                        viableSpotFound = true;
                        break;
                    }
                }

                if (!viableSpotFound)
                {
                    PrintLog("Object cannot spawn none of the existing spawn points ", actor);
                    violations.Add(actor);
                }
            }
        }

        return violations;
    }
}
