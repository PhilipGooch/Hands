using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FindShouldBeStaticObjectsTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no MeshRenderers which should be marked static, but arent";

    public override string Category => "Performance";



    LayerMask dontCheck = (int)(Layers.Projectile | Layers.SheepHead | Layers.SheepTail | Layers.Hand | Layers.Object);

    protected override Result OnRun(ILevel level)
    {
        var violatingObjects = FindViolations(level);

        Result result = new Result();
        result.Count = violatingObjects.Count;
        result.Status = violatingObjects.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    List<GameObject> FindViolations(ILevel level)
    {
        List<GameObject> violations = new List<GameObject>();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            List<MeshRenderer> nonStaticRenderers = root.GetComponentsInChildren<MeshRenderer>().Where(
                x => x.gameObject.isStatic == false
                && !LayerUtils.IsPartOfLayer(x.gameObject.layer, dontCheck)
                && x.gameObject.GetComponent<UnderwaterEffectZone>() == null
                && x.gameObject.GetComponentInParent<SynchronizeRotation>() == null
                && x.gameObject.GetComponent<DestructibleObject>() == null
            ).ToList();

            foreach (var item in nonStaticRenderers)
            {
                if (item.GetComponentInChildren<Rigidbody>() == null && item.GetComponentInParent<Rigidbody>() == null)
                {

                    if (item.GetComponentInChildren<Animator>() == null && item.GetComponentInParent<Animator>() == null)
                    {
                        if (item.GetComponent<MeshFilter>().sharedMesh != null)
                        {
                            PrintLog("item should be static", item);
                            violations.Add(item.gameObject);
                        }
                    }
                }
            }
        }

        return violations;
    }
}
