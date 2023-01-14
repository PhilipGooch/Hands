using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Actor;
using Unity.Mathematics;
using Recoil;
using NBG.Core;
using System.Linq;

/// <summary>
/// This component handles instantiating, respawning and pooling.
/// </summary>
///
[DisallowMultipleComponent]
public class ActorComponent : MonoBehaviour, ActorSystem.IActor, IManagedBehaviour
{
    [SerializeField]
    Rigidbody mainRigidbody;
    ReBody reBody;
    [SerializeField]
    float depthToRespawn = 15;
    [SerializeField]
    ObjectRespawnPoint[] allowedRespawnPoints;
    [SerializeField]
    RespawnBehaviours respawnBehavior = RespawnBehaviours.Respawn;

    public ObjectRespawnPoint[] AllowedRespawnPoints => allowedRespawnPoints;
    public IReadOnlyList<BoxBounds> BoundsCache => boundsCache.AsReadOnly();
    public RigidTransform RespawnLocation { get; set; } = RigidTransform.identity;
    public bool Respawns => respawnBehavior == RespawnBehaviours.Respawn;

    List<BoxBounds> boundsCache = new List<BoxBounds>();
    List<IRespawnListener> respawnListeners = new List<IRespawnListener>();
    Vector3 startPosition;
    BoxBounds particleBounds;

    enum RespawnBehaviours
    {
        Respawn,
        Destroy,
        Nothing
    }

    bool playDespawnParticles = false;

    #region IActor
    GameObject ActorSystem.IActor.ActorGameObject => gameObject;

    int ActorSystem.IActor.PivotBodyID => GetReBody().Id;

    int ActorSystem.IActor.DefaultSpawnRelativeToBodyID => World.environmentId;

    RigidTransform ActorSystem.IActor.DefaultSpawnPoint => RespawnLocation;

    void ActorSystem.IActorCallbacks.OnAfterDespawn()
    {
        if (playDespawnParticles)
        {
            PlayRespawnParticles();
        }

        for (int i = 0; i < respawnListeners.Count; i++)
        {
            respawnListeners[i].OnDespawn();
        }
    }

    void ActorSystem.IActorCallbacks.OnAfterSpawn()
    {
        PlayRespawnParticles();

        for (int i = 0; i < respawnListeners.Count; i++)
        {
            respawnListeners[i].OnRespawn();
        }
    }
    #endregion

    ReBody GetReBody()
    {
        if (!reBody.BodyExists && mainRigidbody != null)
        {
            reBody = new ReBody(mainRigidbody);
        }
        return reBody;
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
        startPosition = transform.position;
        GetRespawnListeners();
        SetupBoundsCache();
        SetupRespawnParticles();
    }

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
    }

    public void Respawn(bool playDespawnParticles = false)
    {
        this.playDespawnParticles = playDespawnParticles;
        if (respawnBehavior == RespawnBehaviours.Respawn)
        {
            ObjectRespawnSystem.Instance.AddToRespawnQueue(this, allowedRespawnPoints);
        }
        else if (respawnBehavior == RespawnBehaviours.Destroy)
        {
            ActorSystem.Main.Despawn(this);
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        ActorSystem.Main.Despawn(this);
        ActorSystem.Main.SetActorSpawnPlacement(this, World.environmentId, new RigidTransform(rotation, position));
        ActorSystem.Main.RequestSpawn(this);
    }

    void FixedUpdate()
    {
        if (respawnBehavior != RespawnBehaviours.Nothing)
        {
            var body = GetReBody();
            if (startPosition.y - body.worldCenterOfMass.y > depthToRespawn)
            {
                Respawn(true);
            }
        }
    }

    void PlayRespawnParticles()
    {
        GameParameters.Instance.despawnEffect.Create(GetObjectCenter(), math.mul(GetObjectRotation(), particleBounds.rotation), particleBounds.size);
    }

    Vector3 GetObjectCenter()
    {
        Vector3 averagePos = Vector3.zero;
        var bodies = ActorSystem.Main.GetActorBodies(this);
        if (bodies.Count > 0)
        {
            for(int i = 0; i < bodies.Count; i++)
            {
                averagePos += (Vector3)World.main.GetBodyPosition(bodies[i]).pos;
            }
            return averagePos / bodies.Count;
        }
        else
        {
            return transform.position;
        }
    }

    Quaternion GetObjectRotation()
    {
        var body = GetReBody();
        return body.rotation;
    }

    void SetupBoundsCache()
    {
        boundsCache.Clear();
        var ourBody = GetReBody();
        var actorBodies = ActorSystem.Main.GetActorBodies(this);
        var invRot = math.inverse(ourBody.rotation);

        bool IsActorBody(int id)
        {
            for(int i = 0; i < actorBodies.Count; i++)
            {
                if (actorBodies[i] == id)
                    return true;
            }
            return false;
        }

        var includeIncative = !gameObject.activeInHierarchy;
        var allColliders = transform.GetComponentsInChildren<Collider>(includeIncative);
        foreach(var col in allColliders)
        {
            var colReBody = new ReBody(col.gameObject.GetComponentInParent<Rigidbody>(includeIncative));
            if (colReBody.BodyExists && IsActorBody(colReBody.Id))
            {
                // World space bounds
                var bounds = new BoxBounds(col);
                // Convert to local space bounds, relative to main rigidbody
                bounds.center = ourBody.InverseTransformPoint(bounds.center);
                bounds.rotation = math.mul(invRot, bounds.rotation);
                boundsCache.Add(bounds);
            }
        }

        if (boundsCache.Count == 0)
        {
            Debug.Log("Actor has no bounds!", gameObject);
        }
    }

    void SetupRespawnParticles()
    {
        var fullBounds = boundsCache[0];
        for(int i = 1; i < boundsCache.Count; i++)
        {
            fullBounds.Encapsulate(boundsCache[i]);
        }
        particleBounds = fullBounds;
    }

    void GetRespawnListeners()
    {
        if (respawnBehavior != RespawnBehaviours.Nothing)
        {
            GetComponentsInChildren(respawnListeners);
        }
    }

    void OnValidate()
    {
        if (mainRigidbody == null)
        {
            mainRigidbody = GetComponentInChildren<Rigidbody>();
        }
    }

#if UNITY_EDITOR
    public void CollectBoundsEditor(List<BoxBounds> results)
    {
        var childActors = GetComponentsInChildren<ActorComponent>();
        HashSet<Collider> allColliders = new HashSet<Collider>(GetComponentsInChildren<Collider>());
        HashSet<Collider> exclusions = new HashSet<Collider>();
        var invRot = Quaternion.Inverse(transform.rotation);

        foreach (var child in childActors)
        {
            if (child != this)
            {
                var childColliders = child.GetComponentsInChildren<Collider>();
                exclusions.UnionWith(childColliders);
            }
        }
        allColliders.ExceptWith(exclusions);

        foreach (var col in allColliders)
        {
            var bounds = new BoxBounds(col);
            bounds.center = transform.InverseTransformPoint(bounds.center);
            bounds.rotation = invRot * bounds.rotation;
            results.Add(bounds);
        }
    }

    List<BoxBounds> boundsEditor = new List<BoxBounds>();

    void OnDrawGizmosSelected()
    {
        if (respawnBehavior == RespawnBehaviours.Respawn)
        {
            IReadOnlyList<BoxBounds> bounds;
            List<GameObject> toSkip = null;

            if (Application.isPlaying)
            {
                bounds = BoundsCache;
            }
            else
            {
                boundsEditor.Clear();
                CollectBoundsEditor(boundsEditor);
                bounds = boundsEditor;
                toSkip = FindObjectsOfType<ActorComponent>().Select(x => x.gameObject).ToList();
            }

            var points = AllowedRespawnPoints;
            if (points == null || points.Length == 0)
                points = GameObject.FindObjectsOfType<ObjectRespawnPoint>();

            foreach (var p in points)
            {
                if (p == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Gizmos.color = p.CheckIfFits(bounds) ? Color.green : Color.red;
                }
                else
                {
                    var checker = new EmptySpaceChecker(p.transform);
                    Gizmos.color = checker.CheckIfFits(bounds, toSkip) ? Color.green : Color.red;
                }

                foreach (var b in bounds)
                {
                    var m = Matrix4x4.TRS(p.transform.TransformPoint(b.center), p.transform.rotation * b.rotation, b.size);
                    Gizmos.matrix = m;
                    Gizmos.DrawWireCube(float3.zero, new float3(1, 1, 1));
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }
    #endif
}
