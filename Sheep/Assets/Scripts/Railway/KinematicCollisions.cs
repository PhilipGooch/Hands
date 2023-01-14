using NBG.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct Overlap
{
    public bool overlaping;
    public float distance;

    public Overlap(bool overlaping, float distance)
    {
        this.overlaping = overlaping;
        this.distance = distance;
    }
}
public class KinematicCollisions : MonoBehaviour
{
#if UNITY_EDITOR
    List<Collider> overlappedColliders = new List<Collider>();
    List<CastDebug> castDebug = new List<CastDebug>();
#endif

    private RaycastHit[] collidersInRange;

    [SerializeField]
    int maxSearchDepth = 4;

    [SerializeField]
    float overlapTolerance = 0.020f;

    [Tooltip("Mostly for extruded parts which could collide before the main collider. or which arent touching the main collider")]
    [SerializeField]
    List<Collider> additionalStartingOrigins;

    [SerializeField]
    bool showDebugGizmos;

    Rigidbody originalRigidbody;
    Dictionary<Rigidbody, Collider[]> getComponentCache = new Dictionary<Rigidbody, Collider[]>();

    private void Awake()
    {
        collidersInRange = new RaycastHit[32];
#if UNITY_EDITOR
        if (showDebugGizmos)
        {
            overlappedColliders = new List<Collider>();
            castDebug = new List<CastDebug>();
        }
#endif

    }

    public Overlap CheckForCollisions(Vector3 direction, Collider initialCollider)
    {
#if UNITY_EDITOR
        if (showDebugGizmos)
        {
            overlappedColliders.Clear();
            castDebug.Clear();
        }
#endif

        originalRigidbody = initialCollider.attachedRigidbody;

        if (originalRigidbody == null)
            Debug.LogError("No rigidbody connected to starting collider");

        float clippingDepth = CheckForChainOverlaps(initialCollider, 0, direction);

        return new Overlap(clippingDepth >= overlapTolerance, clippingDepth);
    }

    float CheckForOverlaps(Collider[] thisObjectColliders, Vector3 direction, Collider origin, int collisionCount, int depth)
    {
        float clippingDepth = 0;

        int finalCount = collisionCount;

        if (depth == 1)
            finalCount += additionalStartingOrigins.Count;

        for (int i = 0; i < finalCount; ++i)
        {
            Collider collider;

            if (i >= collisionCount && depth == 1)
            {
                collider = additionalStartingOrigins[i - collisionCount];
            }
            else
                collider = collidersInRange[i].collider;

            if (ShouldSkip(origin, collider, thisObjectColliders))
                continue;

            if (DirectionDot(collider.transform.position, direction) < 0)
                continue;

            if (ShouldCheckForOverlap(origin, collider))
            {
                Overlap overlap = GetOverlap(origin, collider);

                if (overlap.overlaping)
                {
                    clippingDepth = Mathf.Max(overlap.distance, clippingDepth);
                    /* if (overlap.distance >= overlapTolerance)
                         Debug.Log($"{collider.gameObject.name} is overlapping with {origin.gameObject.name} --- overlap.distance {overlap.distance }");*/

#if UNITY_EDITOR
                    if (showDebugGizmos)
                    {
                        overlappedColliders.Add(collider);
                    }
#endif

                }
            }

            if (collider.attachedRigidbody != null)
            {
                clippingDepth = Mathf.Max(CheckForChainOverlaps(collider, depth, direction), clippingDepth);
            }
        }
        return clippingDepth;
    }

    float CheckForChainOverlaps(Collider origin, int depth, Vector3 direction)
    {
        depth++;
        if (depth > maxSearchDepth)
            return 0;

        int collisionsFound = SearchForCollisions(
                origin,
                origin.bounds.center,
                direction,
                //Should the cast extents be a bit thinner? Now they are the exact same size as the collider 
                new BoxBounds(origin).extents,
                 DetectionDistanceFromObjectScale(origin, direction)
        );

        if (collisionsFound == 0 && additionalStartingOrigins.Count == 0)
            return 0;

        return CheckForOverlaps(GetObjectColliders(origin), direction, origin, collisionsFound, depth);
    }

    Collider[] GetObjectColliders(Collider origin)
    {
        var rigid = origin.attachedRigidbody;
        if (rigid != null)
        {
            if (getComponentCache.ContainsKey(rigid))
            {
                return getComponentCache[rigid];
            }
            else
            {
                getComponentCache.Add(rigid, origin.GetComponentsInChildren<Collider>());

                return getComponentCache[rigid];
            }
        }
        else
        {
            return origin.GetComponentsInChildren<Collider>();
        }
    }

    Overlap GetOverlap(Collider origin, Collider other)
    {
        Vector3 dir;
        float distance;

        bool overlapped = Physics.ComputePenetration(
            origin, origin.transform.position, origin.transform.rotation,
            other, other.transform.position, other.transform.rotation,
            out dir, out distance
        );

        return new Overlap(overlapped, distance);
    }
    int SearchForCollisions(Collider origin, Vector3 center, Vector3 direction, Vector3 castExtents, float distance)
    {
        Quaternion rotation = origin.transform.rotation;

#if UNITY_EDITOR
        if (showDebugGizmos)
        {
            castDebug.Add(new CastDebug(center, castExtents, direction, rotation, distance));
        }
#endif
        return Physics.BoxCastNonAlloc(center, castExtents, direction, collidersInRange, rotation, distance, (int)Layers.KinematicCollisions);
    }

    bool ShouldSkip(Collider origin, Collider collider, Collider[] thisObjectColliders)
    {
        //i mean, why check collision with itself
        if (collider == origin)
            return true;

        //if its another kinematic object, we should skip checking it, since there is no way to to move it
        if (originalRigidbody != origin.attachedRigidbody && origin.attachedRigidbody.isKinematic)
            return true;

        //is collider inside origin object
        bool found = false;
        for (int i = 0; i < thisObjectColliders.Length; i++)
        {
            if (thisObjectColliders[i] == collider)
            {
                found = true;
                break;
            }
        }

        // if collider is not in NoSkip list and is part of origin, then skip cheking it
        if (found && !additionalStartingOrigins.Contains(collider))
            return true;

        return false;
    }

    bool ShouldCheckForOverlap(Collider origin, Collider collider)
    {
        //if root object
        if (origin.transform.parent == null)
            return true;

        //sheep parts are always overlapping, so its a perma false positive, only important to check for sheep overlap with other objects
        if (LayerUtils.IsPartOfLayer(origin.gameObject.layer, (int)Layers.Sheep) && LayerUtils.IsPartOfLayer(collider.gameObject.layer, (int)Layers.Sheep))
            return false;

        //if both colliders are from the same rigidbody, they cant interact with eachother
        if (origin.attachedRigidbody == collider.attachedRigidbody)
            return false;

        return true;
    }

    float DetectionDistanceFromObjectScale(Collider collider, Vector3 direction)
    {
        Vector3 size = collider.bounds.extents;

        return Vector3.Project(size, direction).magnitude;
    }

    float DirectionDot(Vector3 other, Vector3 direction)
    {
        Vector3 directionToOther = other - transform.position;
        return Vector3.Dot(directionToOther, direction);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            if (overlappedColliders != null)
            {
                for (int i = 0; i < overlappedColliders.Count; i++)
                {
                    Gizmos.color = Color.blue;

                    Gizmos.DrawSphere(overlappedColliders[i].transform.position, 0.1f);
                }
            }
            if (castDebug != null)
            {
                for (int i = 0; i < castDebug.Count; i++)
                {
                    Gizmos.color = Color.green;
                    Matrix4x4 oldGizmosMatrix = Gizmos.matrix;

                    Gizmos.matrix = Matrix4x4.TRS(castDebug[i].center, castDebug[i].rot, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, castDebug[i].castExtents * 2);

                    Gizmos.matrix = oldGizmosMatrix;

                    Gizmos.DrawRay(castDebug[i].center, castDebug[i].direction * castDebug[i].dist);
                }
            }
        }
    }
#endif

    struct CastDebug
    {
        public Vector3 center;
        public Vector3 castExtents;
        public Vector3 direction;
        public Quaternion rot;
        public float dist;

        public CastDebug(Vector3 center, Vector3 castExtents, Vector3 direction, Quaternion rot, float dist)
        {
            this.center = center;
            this.castExtents = castExtents;
            this.direction = direction;
            this.rot = rot;
            this.dist = dist;
        }
    }
}


