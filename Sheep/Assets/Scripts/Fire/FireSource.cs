using NBG.Core;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FireSource : MonoBehaviour, IRespawnListener
{
    [SerializeField]
    Collider initialFireZone = null;
    [Tooltip("Should this fire source ignite it's own interactable entity?")]
    [SerializeField]
    bool igniteEntityOnStart = false;
    [SerializeField]
    bool generateThreat = true;
    [Tooltip("Multiplies the range of the threat.")]
    [SerializeField]
    float threatRangeMultiplier = 0.5f;

    bool BurnedOut => interactableEntity != null && interactableEntity.physicalMaterial.CanBurnout && interactableEntity.HasBurnedOut;
    
    float fireZoneIncrease = 0f;

    List<Collider> collidersOnFire = new List<Collider>();
    RaycastHit[] hits = new RaycastHit[64];

    InteractableEntity interactableEntity = null;

    private void Awake()
    {
        interactableEntity = GetComponentInParent<InteractableEntity>();
        ResetState();
    }

    void IgniteEntity()
    {
        if (igniteEntityOnStart)
            interactableEntity?.IgniteObject();
    }

    public void InitializeFire(List<Collider> objectColliders, float fireZoneIncrease = 1f)
    {
        collidersOnFire.Clear();
        if (initialFireZone != null)
        {
            collidersOnFire.Add(initialFireZone);
        }
        if (objectColliders != null)
        {
            collidersOnFire.AddRange(objectColliders);
        }
        this.fireZoneIncrease = fireZoneIncrease;
    }

    void FixedUpdate()
    {
        if (BurnedOut)
            return;

        foreach (var col in collidersOnFire)
        {
            var bounds = new BoxBounds(col);
            var castSize = (bounds.size / 2) + new float3(1, 1, 1) * fireZoneIncrease;
            var castLength = Vector3.Project(castSize, Vector3.up).magnitude;//+ castSize.y;
            var castOrigin = bounds.center;
            castOrigin.y -= castSize.y;

            var hitCount = Physics.BoxCastNonAlloc(castOrigin, castSize, Vector3.up, hits, transform.rotation, castLength);

            for (int i = 0; i < hitCount; i++)
            {
                var target = hits[i].collider.transform;

                var otherPhysicalObject = target.GetComponentInParent<InteractableEntity>();
                if (otherPhysicalObject != null)
                {
                    // Check if fire is not blocked by other objects or walls
                    Vector3 closest = hits[i].collider.ClosestPointSafe(bounds.center);

                    var castDir = closest - (Vector3)bounds.center;
                    if (Physics.Raycast(bounds.center, castDir.normalized, out var hitInfo, hits[0].distance))
                    {
                        if (hitInfo.collider == hits[i].collider)
                        {
                            if (otherPhysicalObject != interactableEntity)
                                otherPhysicalObject.NotifyObjectInsideFire();
                        }
                    }
                    else if (otherPhysicalObject != interactableEntity)
                    {
                        otherPhysicalObject.NotifyObjectInsideFire();
                    }
                }
            }

            if (generateThreat)
            {
                Threat.AddRegularThreat(new BoxBoundThreat(col, threatRangeMultiplier, 9f));
            }
        }
    }

    void ResetState()
    {
        InitializeFire(null, 0);
        IgniteEntity();
    }

    public void OnDespawn()
    {
    }

    public void OnRespawn()
    {
        ResetState();
    }
}
