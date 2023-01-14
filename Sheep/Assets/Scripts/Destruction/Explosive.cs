using NBG.Core;
using Recoil;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    [SerializeField]
    float explosionRadius = 5f;
    [SerializeField]
    float explosionForce = 5000f;
    [Tooltip("multiplies explosionForce when sending it to the DestructibleObject for damage calculations (higher multi means less actual force is needed for destruction)")]
    [SerializeField]
    float destructableObjectForceMulti = 1f;
    [SerializeField]
    [Tooltip("Layers that are affected by the dynamite explosion.")]
    LayerMask affectedLayers = (int)(Layers.Sheep | Layers.Projectile | Layers.Object | Layers.Default | Layers.IgnoreWalls);
    [SerializeField]
    [Tooltip("Layers that absorb the explosion force of the dynamite and can shield the objects behind them.")]
    LayerMask blockingLayers = (int)(Layers.Projectile | Layers.Object | Layers.Default | Layers.Walls | Layers.IgnoreWalls);

    [HideInInspector]
    [SerializeField]
    new Rigidbody rigidbody;
    [HideInInspector]
    [SerializeField]
    DestructibleObject destructibleObject;

    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();

        if (destructibleObject == null)
            destructibleObject = GetComponentInParent<DestructibleObject>();
    }

    private void Start()
    {
        Debug.Assert(rigidbody != null, "No rigidbody on explosive object");
        Debug.Assert(destructibleObject != null, "No destructibleObject on explosive object");

        destructibleObject.onDestroyed += AddExplosionForce;
    }

    private void OnDestroy()
    {
        destructibleObject.onDestroyed -= AddExplosionForce;
    }

    Collider[] hits = new Collider[128];
    List<Rigidbody> affectedRigs = new List<Rigidbody>();
    List<DestructibleObject> affectedDestructibles = new List<DestructibleObject>();

    void AddExplosionForce()
    {
        affectedRigs.Clear();
        affectedDestructibles.Clear();
        var explosionCenter = transform.position;
        var hitCount = Physics.OverlapSphereNonAlloc(explosionCenter, explosionRadius, hits, affectedLayers);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];

            var rig = hit.attachedRigidbody;
            var destructible = hit.GetComponentInParent<DestructibleObject>();

            if (affectedDestructibles.Contains(destructible) || affectedRigs.Contains(rig))
                continue;

            bool isPathFree = false;
            if (rig != null)
                isPathFree = PathIsFreeToComponent(explosionCenter, hit.ClosestPointSafe(explosionCenter), rig, blockingLayers);
            else if (destructible != null)
                isPathFree = PathIsFreeToComponent(explosionCenter, hit.ClosestPointSafe(explosionCenter), destructible, blockingLayers);

            if (isPathFree)
            {
                //  var destructible = hit.attachedRigidbody.GetComponent<DestructibleObject>();
                if (destructible != null)
                {
                    affectedDestructibles.Add(destructible);
                    var diff = destructible.transform.position - explosionCenter;
                    var explosionStrength = explosionForce * Mathf.Clamp(diff.magnitude / explosionRadius, 0.05f, 1f);
                    destructible.ProcessExplosion(
                        destructible.transform.position,
                        diff.normalized,
                        explosionStrength * destructableObjectForceMulti,
                        gameObject,
                        gameObject.layer,
                        AddExplosionForce);
                }
                if (rig != null)
                {
                    affectedRigs.Add(rig);
                    AddExplosionForce(rig);
                }
            }
        }
    }

    RaycastHit[] tempRaycasts = new RaycastHit[8];

    private bool PathIsFreeToComponent(Vector3 explosionCenter, Vector3 targetPoint, Component target, int affectedLayers)
    {
        var diff = targetPoint - explosionCenter;
        var obstacles = Physics.RaycastNonAlloc(explosionCenter, diff.normalized, tempRaycasts, diff.magnitude, affectedLayers);

        for (int z = 0; z < obstacles; z++)
        {
            var castToCheck = tempRaycasts[z];

            var hitRigidbody = castToCheck.collider.GetComponentInParent<Rigidbody>();
            var hitDestructible = castToCheck.collider.GetComponentInParent<DestructibleObject>();

            if (hitRigidbody != target && hitRigidbody != rigidbody &&
                hitDestructible != target && hitDestructible != destructibleObject)
            {
                // Sheep do not block explosions to prevent self-blocking, destructibles and non kinematic rigidbodies dont block explosions as well

                bool destructibleInPath = hitDestructible != null;
                bool nonKinematicRigidbodyInPath = hitRigidbody != null && !hitRigidbody.isKinematic;
                bool sheepInPath = LayerUtils.IsPartOfLayer(castToCheck.collider.gameObject.layer, Layers.Sheep);

                if (!sheepInPath && !destructibleInPath && !nonKinematicRigidbodyInPath)
                {
                    return false;
                }
            }
        }
        return true;
    }

    void AddExplosionForce(Rigidbody target)
    {
        var reTarget = new ReBody(target);
        reTarget.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0.5f, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
