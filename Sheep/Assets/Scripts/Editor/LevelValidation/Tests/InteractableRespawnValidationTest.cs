using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InteractableRespawnValidationTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksProject | ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no child rigidbodies without interactable entity components.";

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
        List<Rigidbody> childRigidbodies = new List<Rigidbody>();
        foreach (var root in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
        {
            //currently we are only interested in burnable interactable entities.
            var entities = root.GetComponentsInChildren<InteractableEntity>();

            foreach (var entity in entities)
            {
                childRigidbodies.Clear();
                entity.GetComponentsInChildren(childRigidbodies);
                foreach(var body in childRigidbodies)
                {
                    if (body.isKinematic)
                        continue;
                    var bodyEntity = body.GetComponentInChildren<InteractableEntity>();
                    // If there is no entity for this rigidbody, it's an error
                    if (bodyEntity == null)
                    {
                        AddViolation(body);
                    }
                    else
                    {
                        // If there is an entity component, but it belongs to a different rigidbody it's an error
                        var bodyToCheck = bodyEntity.GetComponent<Rigidbody>();
                        if (bodyToCheck == null)
                        {
                            bodyToCheck = bodyEntity.GetComponentInParent<Rigidbody>();
                        }

                        if (bodyToCheck != body)
                        {
                            AddViolation(body);
                        }
                    }
                }
            }
        }
    }

    void AddViolation(Rigidbody body)
    {
        PrintError($"Rigidbody with no InteractableEntity found {body.name}. It will not be able to respawn properly without an interactable entity component.", body.gameObject);
        violations.Add(body.gameObject);
    }

}
