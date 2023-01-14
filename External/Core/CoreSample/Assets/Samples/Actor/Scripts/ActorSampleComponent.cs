using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using Unity.Mathematics;
using UnityEngine;
using NBG.Actor;
using System;

/// <summary>
/// A sample actor component
/// </summary>
[DisallowMultipleComponent]
public class ActorSampleComponent : MonoBehaviour, IManagedBehaviour, ActorSystem.IActor
{
    [Header("Actor system properties"), ReadOnlyInPlayModeField, SerializeField]
    private Rigidbody actorPivotBody = null;

    [Tooltip("If null, then respawn location is world space and not relative to another rigidbody"), ReadOnlyInPlayModeField, SerializeField]
    private Rigidbody relativeRespawnTarget;

    GameObject ActorSystem.IActor.ActorGameObject => gameObject;

    void ActorSystem.IActorCallbacks.OnAfterSpawn() => OnAfterSpawn?.Invoke();
    void ActorSystem.IActorCallbacks.OnAfterDespawn() => OnAfterDespawn?.Invoke();

    int ActorSystem.IActor.DefaultSpawnRelativeToBodyID
    {
        get
        {
            if (spawnRelativeToBodyID == World.environmentId)
                spawnRelativeToBodyID = ManagedWorld.main.FindBody(relativeRespawnTarget, true);

            return spawnRelativeToBodyID;
        }
    }

    int ActorSystem.IActor.PivotBodyID
    {
        get
        {
            Debug.Assert(actorPivotBody != null, $"Actor: {gameObject.name} missing pivot body. Respawning undefined.", gameObject);

            if (actorPivotBodyID == World.environmentId)
                actorPivotBodyID = ManagedWorld.main.FindBody(actorPivotBody);

            return actorPivotBodyID;
        }
    }

    [NodeAPI("OnAfterSpawn")]
    public event Action OnAfterSpawn;
    [NodeAPI("OnAfterDespawn")]
    public event Action OnAfterDespawn;

    private int spawnRelativeToBodyID = World.environmentId;
    private int actorPivotBodyID = World.environmentId;

    private RigidTransform? defaultSpawnLocation = null;

    private bool cleanedUp = false;

    RigidTransform ActorSystem.IActor.DefaultSpawnPoint
    {
        get
        {
            if (!defaultSpawnLocation.HasValue)
                defaultSpawnLocation = new RigidTransform(actorPivotBody.transform.rotation, actorPivotBody.transform.position);
            return defaultSpawnLocation.Value;
        }
    }

    void IManagedBehaviour.OnLevelLoaded() { }

    void IManagedBehaviour.OnAfterLevelLoaded() { }

    private void TryCleanup()
    {
        if (!cleanedUp)
        {
            ActorSystem.Main?.UnregisterActor(this);
            cleanedUp = true;
        }
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        TryCleanup();
    }

    private void OnDestroy()
    {
        TryCleanup();
    }

    private void Reset()
    {
        // This is for hinting purposes, so it isn't a chore to setup single body actors. But there is no requirement for body to be on the same GO as actor.
        if (actorPivotBody == null)
            actorPivotBody = GetComponent<Rigidbody>();
    }
}
