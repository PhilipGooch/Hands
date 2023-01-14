using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using UnityEngine;

public class ThereAreNoListsWithNullValues : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no lists with null values";

    public override string Category => "Scene";

    List<Component> violations = new List<Component>();

    protected override Result OnRun(ILevel level)
    {
        FindViolations(level);

        var result = new Result();
        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    private void FindViolations(ILevel level)
    {
        violations.Clear();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            CheckActorComponents(root);

            CheckAudioManager(root);

            CheckDestructibleObject(root);
        }
    }

    void CheckActorComponents(GameObject root)
    {
        foreach (var actor in root.GetComponentsInChildren<ActorComponent>())
        {
            CheckForNullCollectionValues(actor.AllowedRespawnPoints, actor, "InteractableEntity", "allowedRespawnPoints");
        }
    }

    void CheckAudioManager(GameObject root)
    {
        foreach (var audioManager in root.GetComponentsInChildren<AudioManager>())
        {
            CheckForNullCollectionValues(audioManager.fire, audioManager, "AudioManager", "fire clips");
            CheckForNullCollectionValues(audioManager.steps, audioManager, "AudioManager", "steps clips");
            CheckForNullCollectionValues(audioManager.voices, audioManager, "AudioManager", "voices clips");
            CheckForNullCollectionValues(audioManager.pass, audioManager, "AudioManager", "pass clips");
        }
    }

    void CheckDestructibleObject(GameObject root)
    {
        foreach (var destructibleObj in root.GetComponentsInChildren<DestructibleObject>())
        {
            CheckForNullCollectionValues(destructibleObj.destructionEffects, destructibleObj, "DestructibleObject", "Destruction Effects");
            CheckForNullCollectionValues(destructibleObj.burnDestructionEffects, destructibleObj, "DestructibleObject", "Burn Destruction Effects");
            CheckForNullCollectionValues(destructibleObj.hitEffects, destructibleObj, "DestructibleObject", "Hit Effects");
        }
    }

    void CheckForNullCollectionValues<T>(IEnumerable<T> array, Component addIfNull, string objectName, string arrayName)
    {

        if (array != null)
        {
            foreach (var item in array)
            {
                if (item == null || item.Equals(default(T)))
                {
                    violations.Add(addIfNull);
                    PrintLog($"{objectName} has a null entry in {arrayName}", addIfNull);
                    break;
                }
            }
        }
    }
}
