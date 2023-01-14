using System;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Recoil;
using NBG.Water;
using NBG.Core.GameSystems;

public class InteractableEntity : MonoBehaviour, IGrabNotifications, IRespawnListener, IManagedBehaviour
{
    public PhysicalMaterial physicalMaterial;

    // Fire
    public bool Ignited { get; private set; }
    public bool HasBurnedOut { get; private set; }
    private bool InsideAnotherFireSource { get; set; }

    float timeSinceLastContactWithFire = 0f;
    float timeInsideFire = 0f;
    float burnTimer = 0f;
    float maxBurnTimer = 0f;

    // Water
    [SerializeField]
    public bool useWaterSystem;
    [SerializeField]
    public FloatingMeshInstance floatingSystem = new FloatingMeshInstance();

    bool previousSubmergedState;

    // Earth, Air and any other major element...
    [SerializeField]
    ActorComponent parentActor;
    public event System.Action onResetState;
    public event System.Action onSubmerged;

    public event Action<Hand, bool> onGrab;
    public event Action<Hand, bool> onRelease;
    public bool IsGrabbed { get; private set; }

    new Rigidbody rigidbody;
    ReBody reBody;
    List<Collider> activeColliders = new List<Collider>();
    List<InteractableEntity> childEntities = new List<InteractableEntity>();
    InteractableEntity parentEntity;
    [SerializeField]
    private bool handleEventsForChildren = false;

    // Effects
    ObjectEffects objectEffects;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = GetComponentInParent<Rigidbody>();
        }
        if (rigidbody != null && ManagedWorld.main.FindBody(rigidbody, true) == World.environmentId)
        {
            RigidbodyRegistration.RegisterHierarchy(rigidbody.gameObject);
        }

        reBody = new ReBody(rigidbody);
        SetupFloatingMesh();
        objectEffects = new ObjectEffects(gameObject);

        var children = GetComponentsInChildren<InteractableEntity>(true);
        foreach (var child in children)
        {
            if (child != this)
            {
                childEntities.Add(child);
                if (handleEventsForChildren)
                {
                    child.parentEntity = this;
                }
            }
        }
    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
        // ProBuilder meshes are null on Awake so we must initialize colliders in Start
        SetupActiveColliders();
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
    }

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        ShutDown();
    }

    // Only really needed for dynamically created objects, as they don't get IManagedBehaviour events
    // Actor integration might help us avoid this
    void OnDestroy()
    {
        ShutDown();
    }

    void ShutDown()
    {
        if (rigidbody != null)
        {
            floatingSystem.Shutdown();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!UnityEditor.Selection.Contains(this.gameObject))
            return;

        var waterSystem = GameSystemWorldDefault.Instance?.GetExistingSystem<WaterSystem>();
        waterSystem?.DrawDebugGizmos(floatingSystem);
    }
#endif

    void SetupActiveColliders()
    {
        UnityEngine.Profiling.Profiler.BeginSample("SetupActiveColliders");
        var allColliders = GetComponentsInChildren<Collider>();
        foreach (var col in allColliders)
        {
            if (col.enabled)
            {
                activeColliders.Add(col);
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void SetupFloatingMesh()
    {
        UnityEngine.Profiling.Profiler.BeginSample("SetupFloatingMesh");
        if (rigidbody != null && useWaterSystem)
        {
            floatingSystem.Initialize(gameObject, physicalMaterial);
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void ResetState()
    {
        ExtinquishObject();
        InsideAnotherFireSource = false;
        HasBurnedOut = false;
        maxBurnTimer = 0f;
        timeSinceLastContactWithFire = 0f;
        objectEffects.ResetState();
        onResetState?.Invoke();
        ResetChildrenState();
    }

    void ResetChildrenState()
    {
        foreach (var child in childEntities)
        {
            child.ResetState();
        }
    }

    public void TransferState(InteractableEntity otherEntity)
    {
        if (otherEntity.Ignited && physicalMaterial.Flammable)
        {
            IgniteObject();
        }

        maxBurnTimer = otherEntity.maxBurnTimer;
        UpdateFireTint();
    }

    void FixedUpdate()
    {
        UpdateWaterLogic();
        UpdateFireLogic();
    }

    public bool IsPointInsideWater(Vector3 point)
    {
        if (useWaterSystem)
            return floatingSystem.IsPointInsideWater(point);

        return false;
    }

    void UpdateWaterLogic()
    {
        if (rigidbody != null)
        {
            var submerged = floatingSystem.Submerged;
            if (submerged && submerged != previousSubmergedState)
            {
                ExtinquishObject();
                onSubmerged?.Invoke();
            }
            previousSubmergedState = submerged;
        }
    }

    public void NotifyObjectInsideFire()
    {
        InsideAnotherFireSource = true;
    }

    public void ExtinquishObject()
    {
        if (parentEntity != null)
        {
            parentEntity.ExtinquishObject();
        }
        else
        {
            if (Ignited)
            {
                if (physicalMaterial.CreateFireSourceWhenOnFire)
                {
                    Destroy(GetComponent<FireSource>());
                }
                objectEffects.DisableFire();
            }
            burnTimer = 0f;
            timeInsideFire = 0f;
            Ignited = false;
        }
    }

    void UpdateFireLogic()
    {
        if (parentEntity != null)
        {
            if (InsideAnotherFireSource)
            {
                parentEntity.InsideAnotherFireSource = true;
            }
        }
        else
        {
            if (physicalMaterial.Flammable && !Ignited && ((physicalMaterial.CanBurnout && !HasBurnedOut) || !physicalMaterial.CanBurnout))
            {
                if (InsideAnotherFireSource)
                {
                    timeInsideFire += Time.fixedDeltaTime;
                    if (timeInsideFire > physicalMaterial.TimeToIgnite)
                    {
                        timeSinceLastContactWithFire = 0;
                        IgniteObject();
                    }
                }
                else
                {
                    // Slowly cool off if not inside the fire
                    timeInsideFire -= Time.fixedDeltaTime * 0.25f;
                    timeInsideFire = Mathf.Clamp(timeInsideFire, 0, float.MaxValue);
                }
            }

            if (Ignited)
            {
                if (InsideAnotherFireSource && !physicalMaterial.CanBurnout)
                {
                    timeSinceLastContactWithFire = 0;
                }
                else
                {
                    timeSinceLastContactWithFire += Time.fixedDeltaTime;
                }

                burnTimer += Time.fixedDeltaTime;

                UpdateFireTint();

                if (physicalMaterial.CanBurnout && timeSinceLastContactWithFire >= physicalMaterial.TimeUntilSelfExtinguish)
                {
                    ExtinquishObject();
                    HasBurnedOut = true;
                    if (physicalMaterial.DespawnAfterBurnout)
                    {
                        Respawn(false);
                    }
                }

                if (!(physicalMaterial.CanBurnout && HasBurnedOut) && physicalMaterial.CanExtinguishWithMovement && reBody.BodyExists)
                {
                    foreach (var col in activeColliders)
                    {
                        var velocity = reBody.GetPointVelocity(col.bounds.center).magnitude;
                        if (velocity > physicalMaterial.VelocityToExtinguishFire)
                        {
                            ExtinquishObject();
                        }
                    }
                }

                if (physicalMaterial.Burnable)
                {
                    if (burnTimer > physicalMaterial.BurnDuration)
                    {
                        // BURNT
                        objectEffects.DisableAllEffects();
                        var destructibles = GetComponentsInChildren<DestructibleObject>();
                        if (destructibles.Length > 0)
                        {
                            foreach (var destructible in destructibles)
                            {
                                destructible.DestroyObject(destructible.transform.position, -Vector3.up, 0f, DestructibleObject.DestructionSource.Fire);
                            }
                        }
                        else
                        {
                            gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        InsideAnotherFireSource = false;
    }

    void UpdateFireTint()
    {
        if (physicalMaterial.Flammable)
        {
            maxBurnTimer = Mathf.Max(maxBurnTimer, burnTimer);

            if (physicalMaterial.DarkenColorWhenOnFire)
            {
                objectEffects.SetTint(physicalMaterial.MaxColorTintFromFire, Mathf.Clamp01(maxBurnTimer / physicalMaterial.TimeForFullTintFromFire));
            }
        }
    }

    public void IgniteObject()
    {
        if (parentEntity != null)
        {
            parentEntity.IgniteObject();
        }
        else if (!Ignited)
        {
            Ignited = true;
            if (physicalMaterial.CreateFireSourceWhenOnFire)
            {
                var fireSource = gameObject.AddComponent<FireSource>();
                fireSource.InitializeFire(activeColliders, 0.1f);
            }

            objectEffects.EnableFire(physicalMaterial.SpawnFireParticles);
        }
    }
    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            IsGrabbed = true;
        }

        onGrab?.Invoke(hand, firstGrab);
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            IsGrabbed = false;
        }

        onRelease?.Invoke(hand, firstGrab);
    }

    public void Respawn(bool playDespawnParticles = false)
    {
        if (parentActor != null)
        {
            parentActor.Respawn(playDespawnParticles);
        }
        else // In case we don't have an actor, we disable the gameobject instead
        {
            OnDespawn();
            gameObject.SetActive(false);
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (parentActor != null)
        {
            parentActor.Teleport(position, rotation);
        }
    }

    Vector3 RandomPositionOnObject()
    {
        var randomCollider = UnityEngine.Random.Range(0, activeColliders.Count);
        var col = activeColliders[randomCollider];

        var randomPoint = col.bounds.center + Vector3.Scale(UnityEngine.Random.insideUnitSphere, col.bounds.extents);

        return col.ClosestPointSafe(randomPoint);
    }

    public void OnRespawn() { }
    public void OnDespawn()
    {
        ResetState();
    }

    void OnValidate()
    {
        if (parentActor == null)
        {
            parentActor = GetComponentInParent<ActorComponent>();
        }
    }
}
