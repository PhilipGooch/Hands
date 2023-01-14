//#define DRAW_START_END_POINTS

using NBG.Core;
using Recoil;
using System.Collections.Generic;
using UnityEngine;

public abstract class GrabbableImpaler : MonoBehaviour, IGrabNotifications, IProjectHandAnchor
{
    protected new Rigidbody rigidbody;
    [SerializeField]
    protected Collider nailCollider;
    [SerializeField]
    bool penetrateGranularOnly = false;
    [SerializeField]
    float extraNailLength = 0f;
    [SerializeField]
    protected Vector3 impaleDirection = -Vector3.up;
    [SerializeField]
    bool requireGrabToPenetrate = true;
    [SerializeField]
    ImpalerType impalerType = ImpalerType.Nail;
    [SerializeField]
    bool sortByDistance;

    protected bool initialized = false;
    protected bool isGrabbed = false;
    protected float nailLength = 1f;
    protected float partialPenetrationDistance = 0.20f;
    Vector3 nailStart = Vector3.zero;
    RaycastHit[] raycastHits = new RaycastHit[32];
    Quaternion nailedRotation = Quaternion.identity;
    protected ReBody reBody;

    const float offset = 0.2f;

    enum ImpalerType
    {
        Nail,
        Axe
    }

    // An object can either be non-penetrated, partially penetrated or fully penetrated
    // A non-penetrated object does not interact with the impaler in any way
    // A partially penetrated object will fall out of the impaler when it is released (basically it will become a non-penetrated object)
    // A fully penetrated object should form a more rigid connection with the impaler
    struct InsideObject
    {
        public Collider collider;
        public float depth;

        public InsideObject(Collider collider, float depth)
        {
            this.collider = collider;
            this.depth = depth;
        }

        public override bool Equals(object obj)
        {
            if (obj is InsideObject)
            {
                var inside = (InsideObject)obj;
                return collider == inside.collider;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return collider.GetHashCode();
        }
    }
    protected List<Collider> nailColliders = new List<Collider>();
    List<InsideObject> insideObjects = new List<InsideObject>();
    List<InsideObject> previousInsideObjects = new List<InsideObject>();

    public bool IsImpaling()
    {
        return insideObjects.Count > 0;
    }

    protected virtual void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);

        if (rigidbody == null)
        {
            rigidbody = GetComponentInParent<Rigidbody>();
        }
        nailColliders.AddRange(rigidbody.GetComponentsInChildren<Collider>(true));

        BoxBounds boxBounds = new BoxBounds(nailCollider);

        var worldImpaleDirection = reBody.TransformDirection(impaleDirection);

        nailLength = Vector3.Project(boxBounds.size, impaleDirection).magnitude;
        nailStart = reBody.InverseTransformPoint((Vector3)boxBounds.center - worldImpaleDirection * nailLength * 0.5f);
        nailLength += extraNailLength;
        SetupInitialObjectState();
    }

    void OnDisable()
    {
        RemoveAllInsideObjects();
    }

    public Vector3 NailStart
    {
        get
        {
            return reBody.TransformPoint(nailStart);
        }
    }

    public Vector3 NailTip
    {
        get
        {
            return NailStart + reBody.TransformDirection(impaleDirection * nailLength);
        }
    }

    public Vector3 NailDirection
    {
        get
        {
            return (NailTip - NailStart).normalized;
        }
    }


    void SetupInitialObjectState()
    {
        initialized = false;

        var hitCount = RaycastThroughNail();

        for (int i = 0; i < hitCount; i++)
        {
            var raycastHit = raycastHits[i];
            if (!IsPartOfNail(raycastHit.collider))
            {
                // If the object is inside something, assume max depth to properly impale
                UpdateInsideObject(raycastHit.collider, raycastHit.point, nailLength);
            }
        }

        if (insideObjects.Count > 0)
        {
            OnImpale();
        }

        initialized = true;
    }

    protected void RemoveAllInsideObjects()
    {
        previousInsideObjects.Clear();
        previousInsideObjects.AddRange(insideObjects);
        insideObjects.Clear();
        HandleRemovedObjects();
        previousInsideObjects.Clear();
    }
    int RaycastThroughNail()
    {
        var start = NailStart - NailDirection * offset;
        var end = NailTip + GetProjectedVelocity();
        var diff = end - start;
        // Spherecast does not return the surface normal if it begins inside the collider, therefore it is not accurate enough
        var hitCount = Physics.RaycastNonAlloc(start, NailDirection, raycastHits, diff.magnitude);

        if (sortByDistance)
            SortRaycastHitsByDistance(hitCount);

        return hitCount;
    }

    Vector3 GetProjectedVelocity()
    {
        var velocity = reBody.GetPointVelocity(NailTip) * Time.fixedDeltaTime;
        var projectedVelocity = Vector3.Project(velocity, NailDirection);
        return projectedVelocity * (Vector3.Dot(projectedVelocity, NailDirection) > 0 ? 1 : 0);
    }

    void SortRaycastHitsByDistance(int hitCount)
    {
        var start = NailStart - NailDirection * offset;
        if (hitCount > 1)
            for (int i = 0; i < hitCount; i++)
            {
                for (int j = 0; j < hitCount - 1; j++)
                {
                    float distA = Vector3.SqrMagnitude(start - raycastHits[j].point);
                    float distB = Vector3.SqrMagnitude(start - raycastHits[j + 1].point);

                    if (distA > distB)
                    {
                        RaycastHit temp = raycastHits[j + 1];
                        raycastHits[j + 1] = raycastHits[j];
                        raycastHits[j] = temp;
                    }
                }
            }
    }
    
    void OnDrawGizmos()
    {
#if DRAW_START_END_POINTS
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(NailStart - NailDirection * offset, 0.05f);
        Gizmos.DrawLine(NailStart - NailDirection * offset, NailTip + GetProjectedVelocity());

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(NailTip, 0.05f);
#endif
    }


    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            if (IsImpaling())
            {
                OnUnimpale();
            }
            SetGrabState(true);
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            SetGrabState(false);
            RemovePartiallyPenetratingObjects();
            if (IsImpaling())
            {
                OnImpale();
            }
        }
    }

    protected virtual void OnImpale() { }
    protected virtual void OnUnimpale() { }
    protected virtual void OnObjectPenetrated(GameObject target, Vector3 penetrationPosition) { }
    protected virtual void OnObjectDepenetrated(GameObject target) { }

    void SetGrabState(bool grabbed)
    {
        isGrabbed = grabbed;
        if (IsImpaling())
        {
            nailedRotation = reBody.rotation;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isGrabbed && requireGrabToPenetrate)
            return;

        previousInsideObjects.Clear();
        previousInsideObjects.AddRange(insideObjects);
        insideObjects.Clear();

        var hits = RaycastThroughNail();
        for (int i = 0; i < hits; i++)
        {
            var hit = raycastHits[i];
            var other = hit.collider;
            if (Tags.IsPlayer(hit.collider.gameObject))
                continue;
            // we've hit our own collider, ignore
            if (IsPartOfNail(other))
                continue;

            var physicalObject = other.GetComponentInParent<InteractableEntity>();
            // If you want to nail/axe/embed object - they must have interactable entity components
            if (physicalObject == null)
            {
                continue;
            }
            var material = physicalObject.physicalMaterial;
            if (material == null)
            {
                continue;
            }
            if (impalerType == ImpalerType.Nail && !material.CanBeNailedByHand)
            {
                continue;
            }
            if (impalerType == ImpalerType.Axe && !material.CanBeAxed)
            {
                continue;
            }
            if (penetrateGranularOnly && !material.Granular)
            {
                continue;
            }

            var dot = Vector3.Dot(reBody.TransformDirection(-impaleDirection), hit.normal);
            if (dot > 0.5f)
            {
                UpdateInsideObject(other, hit.point, nailLength - hit.distance);
            }
        }

        HandleRemovedObjects();
    }

    bool IsPartOfNail(Collider target)
    {
        return nailColliders.Contains(target);
    }

    protected virtual bool CanPenetrate(Vector3 position)
    {
        return true;
    }

    void UpdateInsideObject(Collider other, Vector3 penetrationPosition, float depth)
    {
        InsideObject previousObject = default;
        foreach (var targetObject in previousInsideObjects)
        {
            if (targetObject.collider == other)
            {
                previousObject = targetObject;
                break;
            }
        }
        float previousDepth = 0;
        bool objectPenetrated = false;
        if (previousObject.collider != null)
        {
            previousDepth = previousObject.depth;
            previousObject.depth = depth;
            insideObjects.Add(previousObject);
            objectPenetrated = true;
        }
        else if (CanPenetrate(penetrationPosition))
        {
            if (previousInsideObjects.Count == 0) // First object determines the nail direction
            {
                nailedRotation = reBody.rotation;
            }
            Physics.IgnoreCollision(other, nailCollider);
            insideObjects.Add(new InsideObject(other, depth));
            objectPenetrated = true;
        }

        if (objectPenetrated)
        {
            UpdateDeepPenetration(other, previousDepth, depth, penetrationPosition);
        }
    }

    void HandleRemovedObjects()
    {
        foreach (var obj in previousInsideObjects)
        {
            // Ignore destroyed objects
            if (obj.collider == null)
                continue;
            if (!insideObjects.Contains(obj))
            {
                Physics.IgnoreCollision(obj.collider, nailCollider, false);
                UpdateDeepPenetration(obj.collider, obj.depth, 0f, Vector3.zero);
            }
        }
    }

    void UpdateDeepPenetration(Collider other, float previousDepth, float currentDepth, Vector3 penetrationPosition)
    {
        if (previousDepth < partialPenetrationDistance && currentDepth > partialPenetrationDistance)
        {
            OnObjectPenetrated(other.gameObject, penetrationPosition);
        }
        else if (previousDepth > partialPenetrationDistance && currentDepth < partialPenetrationDistance)
        {
            OnObjectDepenetrated(other.gameObject);
        }
    }

    void RemovePartiallyPenetratingObjects()
    {
        for (int i = insideObjects.Count - 1; i >= 0; i--)
        {
            var obj = insideObjects[i];
            if (obj.depth < partialPenetrationDistance)
            {
                Physics.IgnoreCollision(obj.collider, nailCollider, false);
                insideObjects.RemoveAt(i);
            }
        }
    }

    protected virtual bool ShouldProject()
    {
        return true;
    }

    public virtual void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        if (IsImpaling() && ShouldProject())
        {
            ProjectAnchorUtils.ProjectLinear(reBody, reBody.worldCenterOfMass, impaleDirection, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
            vrAngular = Vector3.zero;
            vrVel = Vector3.zero;
            vrRot = nailedRotation;
        }
    }
}
