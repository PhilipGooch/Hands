using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionAudioSourceMissingTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
    public override string Name => "There are no grabbable rigidbodies without CollisionAudioSensor component (NOTE, not all rigidbodies should have it)";
    public override string Category => "Audio";

    List<GameObject> violations = new List<GameObject>();


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
        foreach (var root in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
        {
            var rigidbodies = root.GetComponentsInChildren<Rigidbody>().Where(x => x.GetComponentInParent<GrabParamsBinding>() != null);
            foreach (var rigid in rigidbodies)
            {
                if (!rigid.GetComponentInChildren<CollisionAudioSensor>() && !rigid.GetComponentInParent<CollisionAudioSensor>() && !rigid.GetComponentInChildren<Attenuation>() && rigid.GetComponentInChildren<Collider>())
                {
                    PrintLog("No CollisionAudioSensor found", rigid);
                    violations.Add(rigid.gameObject);
                }
            }
        }
    }
}
