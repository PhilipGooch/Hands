using NBG.LogicGraph;
using Recoil;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NBG.Core;

[System.Flags]
public enum PhysicalDestructionSource
{
    Projectile = 1 << 0,
    Axe = 1 << 1,
    Pickaxe = 1 << 2,
    Explosion = 1 << 3,
    Sheep = 1 << 4,
    BasicCollision = 1 << 5,
}

public class DestructibleObject : MonoBehaviour, IGrabNotifications, IRespawnListener, IManagedBehaviour
{
    private enum EffectPlayMode
    {
        Combined,
        Randomised
    }

    [SerializeField]
    private GameObject objectToSpawn;
    [SerializeField]
    private float forceToDestroy = 100;
    [SerializeField]
    [Range(0f, 5f)]
    private float childForceInheritance = 0.75f;
    [SerializeField]
    [Range(0f, 1f)]
    private float childForceRandomness = 0.1f;
    [SerializeField]
    private float minForceToCountAsDamage = 100;
    [SerializeField]
    private Transform parentOverride;

    //Particle Effects
    [SerializeField]
    private EffectPlayMode destructionEffectPlayMode;
    [SerializeField]
    public List<SingleShotParticleEffect> destructionEffects;
    [SerializeField]
    private EffectPlayMode burnDestructionEffectPlayMode;
    [SerializeField]
    public List<SingleShotParticleEffect> burnDestructionEffects;
    [SerializeField]
    private EffectPlayMode hitEffectPlayMode;
    [SerializeField]
    public List<SingleShotParticleEffect> hitEffects;

    //Configuration
    [SerializeField]
    public PhysicalDestructionSource destroyedWithForceFrom = (PhysicalDestructionSource)~0;
    [SerializeField]
    private bool destroyByJointBreak = false;
    [SerializeField]
    private bool destroyedByMultipleHits = false;
    [SerializeField]
    private bool grabSpawnedObjects = false;
    [SerializeField]
    private bool transferJointsToSpawnedObject = true;
    [SerializeField]
    [Tooltip("If the spawned object contains multiple rigidbodies, the joints will be transfered to each new body, resulting in extra joint connections. Otherwise the closest body will be used.")]
    private bool transferSameJointToAllSpawnedRigidbodies = false;
    [SerializeField]
    private float sheepDestructionForceMulti = 100f;
    private const float maxSheepCollisionForce = 2200f;
    private const float minSheepCollisionForce = 1000f;
    private float AdjustedMinSheepCollisionForce => minSheepCollisionForce * sheepDestructionForceMulti;
    private float AdjustedMaxSheepCollisionForce => maxSheepCollisionForce * sheepDestructionForceMulti;

    [NodeAPI("onDestroyed")]
    public event Action onDestroyed;

    private float accumulatedForce = 0f;
    public float AccumulatedForce => accumulatedForce;

    private bool destroyed;
    private Hand grabbingHand = null;

    private List<Joint> externalConnections = new List<Joint>();
    private List<Joint> tempJointList = new List<Joint>();
    private List<DestructibleObject> tempChildDestructiblesList = new List<DestructibleObject>();

    private Transform RootTransform => parentOverride != null ? parentOverride : transform;

#if UNITY_EDITOR
    private List<string> log = new List<string>(5);
    private StringBuilder formattedLog = new StringBuilder();
    public string FormattedLog => formattedLog.ToString();

    private void AddToLog(GameObject sourceObj, float force, string forceSource, bool canDestroy)
    {
        var destroyer = sourceObj.GetComponentInParent<ObjectDestroyer>();
        var hasObjectDestroyer = destroyer != null ? "ObjectDestroyer found" : "No ObjectDestroyer";
        var canDestroyFormated = canDestroy ? " Can Destroy" : "Can't Destroy";
        if (log.Count == log.Capacity)
        {
            log.RemoveAt(0);
        }
        log.Add($"{forceSource} | {hasObjectDestroyer} | force {force} | {canDestroyFormated} \n");
        formattedLog.Clear();
        foreach (var item in log)
        {
            formattedLog.Append(item);
        }
    }
#endif

    private Vector3 ParentScale
    {
        get
        {
            if (parentScale == null)
            {
                parentScale = RootTransform.lossyScale;
            }
            return (Vector3)parentScale;
        }
        set
        {
            parentScale = value;
        }
    }
    private Vector3? parentScale = null;

    new private Rigidbody rigidbody;
    private ReBody reBody;
    private InteractableEntity entity;
    private MeshRenderer meshRenderer;
    private SkinnedMeshRenderer skinnedMeshRenderer;

    public enum DestructionSource
    {
        Physical,
        Fire
    }

    void OnValidate()
    {
        if (destroyedByMultipleHits && minForceToCountAsDamage > forceToDestroy)
        {
            Debug.LogWarning($"{gameObject.name}.{this.GetType()}: minForceToCountAsDamage > forceToDestroy", gameObject);
        }
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
        entity = GetComponent<InteractableEntity>();
        rigidbody = GetComponent<Rigidbody>();
        InformOtherDestructiblesAboutExternalConnections();
        reBody = new ReBody(rigidbody);
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
    }

    private void OnCollisionStay(Collision collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
    }

    public void ResetState()
    {
        accumulatedForce = 0;
        destroyed = false;
    }

    private void ProcessCollision(Collision collision)
    {
        var force = collision.impulse.magnitude / Time.fixedDeltaTime;
        var contact = collision.GetContact(0);
        var collisionObj = collision.collider.gameObject;

        var destroyer = collisionObj.GetComponentInParent<ObjectDestroyer>();
        var physicalDestructionSource = destroyer != null ? destroyer.DestroyerType : PhysicalDestructionSource.BasicCollision;
#if UNITY_EDITOR
        AddToLog(collisionObj, force, "Collision", CanDestroy(physicalDestructionSource));
#endif

        AddDestructionForce(
            contact.point,
            contact.normal,
            force,
            collisionObj,
            collisionObj.layer,
            DestructionSource.Physical,
            physicalDestructionSource);
    }

    internal void ProcessExplosion(
        Vector3 position,
        Vector3 direction,
        float force,
        GameObject damageSourceObject,
        int layer,
        Action<Rigidbody> postProcessDestructionPiece = null)
    {
#if UNITY_EDITOR
        //currently only explosions use this method
        AddToLog(damageSourceObject, force, "Explosion", CanDestroy(PhysicalDestructionSource.Explosion));
#endif

        AddDestructionForce(position, direction, force, damageSourceObject, layer, DestructionSource.Physical, PhysicalDestructionSource.Explosion, postProcessDestructionPiece);
    }

    private bool CanDestroy(PhysicalDestructionSource destructionSource)
    {
        if (destroyed || destroyedWithForceFrom == 0)
        {
            return false;
        }

        // If not destroyed by everything, check if the object we're colliding with can destroy us
        if (~destroyedWithForceFrom != 0 && (destructionSource & destroyedWithForceFrom) == 0)
        {
            // The destroyer is not of the type that can destroy us
            return false;
        }

        return true;
    }

    private void AddDestructionForce(
         Vector3 position,
         Vector3 direction,
         float force,
         GameObject damageSourceObject,
         int layer,
         DestructionSource destructionSource = DestructionSource.Physical,
         PhysicalDestructionSource physicalDestructionSource = PhysicalDestructionSource.BasicCollision,
         Action<Rigidbody> postProcessDestructionPiece = null)
    {
        if (CanDestroy(physicalDestructionSource))
        {
            if (LayerUtils.IsPartOfLayer(layer, Layers.Sheep) && physicalDestructionSource.HasFlag(PhysicalDestructionSource.Sheep) && force > 0)
            {
                var destructionForce = Mathf.Lerp(AdjustedMinSheepCollisionForce, AdjustedMaxSheepCollisionForce, Mathf.InverseLerp(0, maxSheepCollisionForce, force));
                DestroyObject(position, direction, destructionForce, destructionSource, postProcessDestructionPiece);
                return;
            }

            var forceToTestAgainst = force;
            if (destroyedByMultipleHits && force >= minForceToCountAsDamage)
            {
                accumulatedForce += force;
                forceToTestAgainst = accumulatedForce;
            }

            if (forceToTestAgainst >= forceToDestroy)
            {
                VibrateDamageSource(damageSourceObject);
                DestroyObject(position, direction, force, destructionSource, postProcessDestructionPiece);
            }
            else if (destroyedByMultipleHits && force > minForceToCountAsDamage)
            {
                VibrateDamageSource(damageSourceObject);
                ShowHitParticles(position, direction, force);
            }
        }
    }

    private void VibrateDamageSource(GameObject source)
    {
        var sourceRigidbody = source.GetComponentInParent<Rigidbody>();
        if (sourceRigidbody != null)
        {
            var grabbingHand = Player.Instance.GetHandThatIsGrabbingBody(sourceRigidbody);
            if (grabbingHand != null)
            {
                var instance = GameParameters.Instance;
                grabbingHand.Vibrate(0, instance.destructibleHitDuration, instance.destructibleHitFrequency, instance.destructibleHitAmplitude);
            }
        }
    }

    private void OnJointBreak(float breakForce)
    {
        if (destroyByJointBreak)
        {
            DestroyObject(transform.position, transform.forward);
        }
    }

    private void ShowHitParticles(Vector3 destructionCenter, Vector3 destructionDirection, float hitForce)
    {
        PlayEffects(
            hitEffectPlayMode,
            hitEffects,
            destructionCenter,
            destructionDirection,
            hitForce
            );
    }

    [NodeAPI("DestroyThis")]
    public void SimpleDestroy()
    {
        if (!destroyed)
        {
            DestroyObject(transform.position, Vector3.zero, 0);
        }
    }

    public void DestroyObject(Vector3 destructionCenter, Vector3 destructionDirection, float destructionForce = 0f,
        DestructionSource destructionSource = DestructionSource.Physical, Action<Rigidbody> postProcessDestructionPiece = null)
    {
        destroyed = true;

        StopGrab();

        onDestroyed?.Invoke();

        var transformToUse = RootTransform;
        transformToUse.gameObject.SetActive(false);

        if (objectToSpawn != null)
        {
            var objectInstance = Instantiate(objectToSpawn, transformToUse.position, transformToUse.rotation);
            ScaleObject(objectInstance, ParentScale);
            RigidbodyRegistration.RegisterHierarchy(objectInstance);

            TransferEntityState(entity, objectInstance);

            var rigs = objectInstance.GetComponentsInChildren<Rigidbody>();
            foreach (var rig in rigs)
            {
                var otherReBody = new ReBody(rig);
                if (reBody.BodyExists)
                {
                    otherReBody.velocity = reBody.velocity;
                }
                var randomizedDirection = RandomizeForceDirection(destructionDirection);
                var destructionVelocity = CalculateChildVelocity(destructionForce, otherReBody.mass);
                otherReBody.velocity += destructionVelocity * randomizedDirection;
                postProcessDestructionPiece?.Invoke(rig);
            }

            if (transferJointsToSpawnedObject)
            {
                InheritParentExternalConnections(objectInstance, rigs);
                ReformParentConnections(objectInstance, rigs);
            }
        }

        DestroyExternalConnections();

        (var effects, var playmode) = GetDestructionEffect(destructionSource);

        PlayEffects(
            playmode,
            effects,
            destructionCenter,
            destructionDirection,
            destructionForce
            );

        entity?.Respawn(false);
    }

    private void StopGrab()
    {
        if (rigidbody != null && !grabSpawnedObjects)
            Player.Instance.StopGrabbingObject(rigidbody);
    }

    private void PlayEffects(EffectPlayMode playMode, List<SingleShotParticleEffect> effects, Vector3 center, Vector3 direction, float force)
    {
        if (effects == null || effects.Count == 0)
            return;

        switch (playMode)
        {
            case EffectPlayMode.Combined:
                foreach (var effect in effects)
                {
                    PlayEffect(effect, center, direction, force);
                }
                break;
            case EffectPlayMode.Randomised:
                var randomIndex = UnityEngine.Random.Range(0, effects.Count);
                PlayEffect(effects[randomIndex], center, direction, force);
                break;
            default:
                break;
        }
    }

    private void PlayEffect(SingleShotParticleEffect effect, Vector3 center, Vector3 direction, float force)
    {
        var instance = effect.Create(center, Quaternion.LookRotation(Vector3.Lerp(direction, Vector3.up, 0.25f)), ParentScale) as SingleShotParticleEffect;
        if (instance is SingleShotParticleMeshEffect)
        {
            if (meshRenderer != null)
                ((SingleShotParticleMeshEffect)instance).SetTargetMesh(meshRenderer);
            else if (skinnedMeshRenderer != null)
                ((SingleShotParticleMeshEffect)instance).SetTargetMesh(skinnedMeshRenderer);
            else
                Debug.Log("MeshRenderer or SkinnedRenderer not found on the object", gameObject);
        }

        var velocity = CalculateChildVelocity(force, 1f);
        var randomizedDirection = RandomizeForceDirection(direction);
        instance.SetVelocity(randomizedDirection * velocity);
    }

    private Vector3 RandomizeForceDirection(Vector3 destructionDirection)
    {
        return Vector3.Lerp(destructionDirection, UnityEngine.Random.onUnitSphere, childForceRandomness);
    }

    private float CalculateChildVelocity(float force, float childMass)
    {
        if (reBody.BodyExists)
        {
            force *= reBody.invMass;
        }
        return force * childForceInheritance * Time.fixedDeltaTime / childMass;
    }

    private (List<SingleShotParticleEffect>, EffectPlayMode) GetDestructionEffect(DestructionSource destructionSource)
    {
        List<SingleShotParticleEffect> effects = null;
        EffectPlayMode playmode = new EffectPlayMode();

        if (destructionSource == DestructionSource.Fire)
        {
            effects = burnDestructionEffects;
            playmode = burnDestructionEffectPlayMode;
        }
        if (effects == null || effects.Count == 0)
        {
            effects = destructionEffects;
            playmode = destructionEffectPlayMode;
        }

        return (effects, playmode);
    }

    private void ScaleObject(GameObject target, Vector3 scale)
    {
        target.transform.localScale = scale;
        target.GetComponentsInChildren(tempChildDestructiblesList);

        foreach (var child in tempChildDestructiblesList)
        {
            child.ParentScale = scale;
        }

        target.GetComponentsInChildren(tempJointList);
        foreach (var joint in tempJointList)
        {
            if (joint.autoConfigureConnectedAnchor)
            {
                // Trigger anchor position recalculation
                joint.autoConfigureConnectedAnchor = false;
                joint.autoConfigureConnectedAnchor = true;
            }
        }
    }

    // External joints are joints that are exist on other objects and are connected to this rigidbody
    // Transfer them to the newly spawned child rigidebodies
    private void InheritParentExternalConnections(GameObject target, Rigidbody[] rigidbodies)
    {
        foreach (var joint in externalConnections)
        {
            //something was connected to this body. Reconnect it to the new body
            if (joint != null)
            {
                var otherRig = joint.GetComponent<Rigidbody>();

                if (transferSameJointToAllSpawnedRigidbodies)
                {
                    foreach (var body in rigidbodies)
                    {
                        ReconnectExternalBodyToNewBody(joint, body);
                    }
                }
                else
                {
                    var closestBody = FindClosestBody(otherRig.worldCenterOfMass, rigidbodies);
                    ReconnectExternalBodyToNewBody(joint, closestBody);
                }

                Destroy(joint);
            }
        }

        externalConnections.Clear();
    }

    // Transfer joints that the parent had to its spawned children rigidbodies
    private void ReformParentConnections(GameObject target, Rigidbody[] rigidbodies)
    {
        tempJointList.Clear();
        GetComponents(tempJointList);
        foreach (var connection in tempJointList)
        {
            var otherRig = connection.connectedBody;
            var connectionPosition = reBody.worldCenterOfMass;
            if (otherRig != null)
            {
                // Target was deleted this frame, ignore
                if (!otherRig.gameObject.activeSelf)
                {
                    continue;
                }
                var otherReBody = new ReBody(otherRig);
                connectionPosition = otherReBody.worldCenterOfMass;
            }

            if (transferSameJointToAllSpawnedRigidbodies)
            {
                foreach (var body in rigidbodies)
                {
                    ReconnectNewBodyToExternalBody(connection, body);
                }
            }
            else
            {
                var closestBody = FindClosestBody(connectionPosition, rigidbodies);
                ReconnectNewBodyToExternalBody(connection, closestBody);
            }

            Destroy(connection);
        }
    }

    private void DestroyExternalConnections()
    {
        foreach (var joint in externalConnections)
        {
            if (joint != null)
            {
                Destroy(joint);
            }
        }
        externalConnections.Clear();
    }

    private void InformOtherDestructiblesAboutExternalConnections()
    {
        tempJointList.Clear();
        GetComponentsInChildren(tempJointList);
        foreach (var joint in tempJointList)
        {
            var otherRig = joint.connectedBody;
            if (otherRig != null)
            {
                var otherDestructible = otherRig.GetComponent<DestructibleObject>();
                if (otherDestructible != null)
                {
                    otherDestructible.externalConnections.Add(joint);
                }
            }
        }
    }

    private Rigidbody FindClosestBody(Vector3 position, Rigidbody[] possibleBodies)
    {
        float closestDistance = float.MaxValue;
        Rigidbody chosenBody = null;
        foreach (var newBody in possibleBodies)
        {
            var rBod = new ReBody(newBody);
            var distance = (rBod.worldCenterOfMass - position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                chosenBody = newBody;
            }
        }
        return chosenBody;
    }

    private Joint ReconnectExternalBodyToNewBody(Joint oldJoint, Rigidbody newBody)
    {
        var targetGO = oldJoint.gameObject;
        var newJoint = targetGO.AddComponent(oldJoint.GetType()) as Joint;
        newJoint.connectedBody = newBody;
        newJoint.breakForce = oldJoint.breakTorque;
        newJoint.breakTorque = oldJoint.breakTorque;

        var childDestructible = newBody.GetComponent<DestructibleObject>();
        if (childDestructible != null)
        {
            childDestructible.externalConnections.Add(newJoint);
        }
        return newJoint;
    }

    private Joint ReconnectNewBodyToExternalBody(Joint oldJoint, Rigidbody newBody)
    {
        var targetGO = newBody.gameObject;
        var newJoint = targetGO.AddComponent(oldJoint.GetType()) as Joint;
        newJoint.connectedBody = oldJoint.connectedBody;
        newJoint.breakForce = oldJoint.breakTorque;
        newJoint.breakTorque = oldJoint.breakTorque;

        var otherRig = oldJoint.connectedBody;
        if (otherRig != null)
        {
            var otherDestructible = otherRig.GetComponent<DestructibleObject>();
            if (otherDestructible != null)
            {
                otherDestructible.externalConnections.Add(newJoint);
            }
        }

        return newJoint;
    }

    private void TransferEntityState(InteractableEntity entity, GameObject spawnedObject)
    {
        if (entity != null)
        {
            if (entity.Ignited)
            {
                var spawnedEntities = spawnedObject.GetComponentsInChildren<InteractableEntity>();
                foreach (var spawnedE in spawnedEntities)
                {
                    spawnedE.TransferState(entity);
                }
            }
        }

        if (grabSpawnedObjects && grabbingHand != null)
        {
            var spawnedRig = spawnedObject.GetComponent<Rigidbody>();
            if (spawnedRig != null)
            {
                var currentOffset = reBody.BodyExists ? reBody.InverseTransformPoint(grabbingHand.worldAttachedAnchorPos) : transform.InverseTransformPoint(grabbingHand.worldAttachedAnchorPos);
                var spawnedRe = new ReBody(spawnedRig);
                var grabPos = spawnedRe.TransformPoint(currentOffset);
                grabbingHand.InterceptGrab(spawnedRig, grabPos);
            }
        }
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbingHand = hand;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbingHand = null;
        }
    }

    public void OnDespawn()
    {
    }

    public void OnRespawn()
    {
        ResetState();
    }
}

