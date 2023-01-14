using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Audio;
using System.Linq;
using NBG.Core;

public class PhysicsMaterialNotAttachedTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no Colliders which dont have (or have invalid) PhysicsMaterial";

    public override string Category => "Audio";

    List<Collider> violations = new List<Collider>();

    protected override Result OnRun(ILevel level)
    {
        FindViolations(level);

        var result = new Result();
        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }
    void FindViolations(ILevel level)
    {
        violations.Clear();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {

            var colliders = root.GetComponentsInChildren<Collider>().Where(x => x.isTrigger == false);
            foreach (var collider in colliders)
            {
                if (collider.sharedMaterial == null)
                {
                    PrintLog("No PhysicsMat found", collider);
                    violations.Add(collider);
                    continue;
                }

                if (SurfaceTypes.Resolve(collider.sharedMaterial).id == 0)
                {
                    PrintLog("Unknown PhysicsMat", collider);
                    violations.Add(collider);

                    continue;
                }
            }


        }
    }
}
