using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InteractableEntityValidationTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksProject | ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no interactable entities that are missing physical materials.";

    public override string Category => "Prefab & Scene";

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
            //currently we are only interested in burnable interactable entities.
            var entities = root.GetComponentsInChildren<InteractableEntity>();

            foreach (var entity in entities)
            {
                if (entity.physicalMaterial == null)
                {
                    if (violations.Contains(entity.gameObject))
                        continue;

                    violations.Add(entity.gameObject);

                    PrintLog($"InteractableEntity with no physical material found {entity.name}", entity.gameObject);
                }
                var meshFilters = entity.GetComponentsInChildren<MeshFilter>();
            }
        }
    }

}
