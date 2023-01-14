using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField]
    float maxObjectSize = 1f;
    [SerializeField]
    float fireForce = 1000f;
    [SerializeField]
    Transform firePosition;
    [SerializeField]
    SingleShotParticleEffect shootParticles;

    List<LoadedObject> loadedObjects = new List<LoadedObject>();
    const float fireCooldown = 0.5f;
    float fireTimer = fireCooldown;
    List<Collider> cannonColliders = new List<Collider>();
    List<Collider> tempColliders = new List<Collider>();

    struct LoadedObject
    {
        public Rigidbody rigidbody;
        public ReBody reBody;
        bool sleepAllowed;
        InteractableEntity entity;
        GrabParamsBinding grabParamsBinding;
        System.Action onReset;

        public LoadedObject(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
            reBody = new ReBody(rigidbody);
            sleepAllowed = reBody.AllowSleeping;
            reBody.AllowSleeping = false;
            entity = rigidbody.GetComponent<InteractableEntity>();
            grabParamsBinding = rigidbody.GetComponentInParent<GrabParamsBinding>();
            onReset = null;
        }

        public void SetGrabbable(bool grabbable)
        {
            if (grabParamsBinding != null)
            {
                grabParamsBinding.Grabbable = grabbable;
            }
        }

        public void InitializeEntity(System.Action onEntityReset)
        {
            if (entity)
            {
                onReset = onEntityReset;
                entity.onResetState += onReset;
            }
        }

        public void CleanupEntity()
        {
            if (entity)
            {
                reBody.AllowSleeping = sleepAllowed;
                entity.onResetState -= onReset;
            }
        }
    }

    bool CanLoad
    {
        get
        {
            return fireTimer >= fireCooldown && loadedObjects.Count == 0;
        }
    }

    private void Awake()
    {
        GetComponentsInChildren(cannonColliders);
    }

    [NodeAPI("Shoot")]
    public void Shoot()
    {
        StartCoroutine(FireObjects());
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (fireTimer < fireCooldown)
        {
            fireTimer += Time.fixedDeltaTime;
        }
        else
        {
            for (int i = loadedObjects.Count - 1; i >= 0; i--)
            {
                var obj = loadedObjects[i];
                var rig = obj.rigidbody;
                if (rig == null)
                {
                    // Rigidbody destroyed
                    UnloadObject(obj);
                    continue;
                }
                var offsetIndex = (i - loadedObjects.Count) / 2;
                // Get offsets between -1 and 1 instead of -1 and 0
                if (loadedObjects.Count % 2 == 0 && offsetIndex >= 0)
                {
                    offsetIndex++;
                }

                var reBody = obj.reBody;

                reBody.velocity = Vector3.zero;
                reBody.angularVelocity = Vector3.zero;
                reBody.position = firePosition.position + firePosition.forward * offsetIndex * 0.25f;
                reBody.rotation = firePosition.rotation;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CanLoad)
        {
            var otherRig = other.GetComponentInParent<Rigidbody>();
            if (otherRig != null)
            {
                if (PhysicsUtils.GetApproximateSize(otherRig).magnitude < maxObjectSize)
                {
                    var sheep = otherRig.GetComponentInParent<Sheep>();
                    if (sheep != null)
                    {
                        LoadObject(sheep.tail);
                        LoadObject(sheep.head);
                    }
                    else
                    {
                        LoadObject(otherRig);
                    }
                }
            }
        }
    }

    void LoadObject(Rigidbody rig)
    {
        tempColliders.Clear();
        Player.Instance.StopGrabbingObject(rig);
        var loadedObj = new LoadedObject(rig);
        loadedObj.InitializeEntity(() => UnloadObject(loadedObj));
        loadedObj.SetGrabbable(false);
        loadedObjects.Add(loadedObj);
        SetIgnoreCollision(rig, true);
    }

    void UnloadObject(LoadedObject loadedObject)
    {
        if (loadedObjects.Contains(loadedObject))
        {
            loadedObjects.Remove(loadedObject);
            loadedObject.CleanupEntity();
            SetIgnoreCollision(loadedObject.rigidbody, false);
            loadedObject.SetGrabbable(true);
        }
    }

    IEnumerator FireObjects()
    {
        fireTimer = 0f;

        if (shootParticles != null)
        {
            shootParticles.Create(firePosition.position, firePosition.rotation);
        }

        foreach (var loadedObject in loadedObjects)
        {
            var reBody = loadedObject.reBody;
            reBody.position = firePosition.position;
            reBody.rotation = firePosition.rotation;
            reBody.AddForce(firePosition.forward * fireForce, ForceMode.VelocityChange);
        }

        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        for (int i = loadedObjects.Count - 1; i >= 0; i--)
        {
            UnloadObject(loadedObjects[i]);
        }
        loadedObjects.Clear();

    }

    void SetIgnoreCollision(Rigidbody rig, bool ignore)
    {
        if (rig != null)
        {
            tempColliders.Clear();
            rig.GetComponentsInChildren(tempColliders);
            SetIgnoreCollision(tempColliders, ignore);
        }
    }

    void SetIgnoreCollision(List<Collider> colliders, bool ignore)
    {
        foreach (var collider in colliders)
        {
            foreach (var cannonCollider in cannonColliders)
            {
                Physics.IgnoreCollision(collider, cannonCollider, ignore);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (firePosition != null)
        {
            DebugExtension.DrawArrow(firePosition.position, firePosition.forward * 2f, Color.red);
        }
    }
}
