using NBG.Core;
using Recoil;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : MonoBehaviour
{
    bool magnetActive;
    public bool MagnetActive
    {
        get
        {
            return magnetActive;
        }
        set
        {
            magnetActive = value;
            if (!magnetActive)
                DisconnectAll();
        }
    }

    [SerializeField]
    float maxMagnetForce = 10000;
    [SerializeField]
    float magnetRange = 10f;
    [Tooltip("Controls how much force needs to be accumulated until snapped object gets unsnapped")]
    [SerializeField]
    float snappedObjectAcumulatedForceMulti = 100;
    [SerializeField]
    LayerMask affectedLayers = (int)(Layers.Object | Layers.Walls);
    [Tooltip("Should magnet pull objects from all directions")]
    [SerializeField]
    bool allDirectionsPull = false;
    [SerializeField]
    Vector3 magnetDirection;
    [SerializeField]
    new Rigidbody rigidbody;
    ReBody reBody;
  
    void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        reBody = new ReBody(rigidbody);
    }

    Collider[] hits = new Collider[64];

    Dictionary<InteractableEntity, MagnetConnectionData> connections = new Dictionary<InteractableEntity, MagnetConnectionData>();
    List<InteractableEntity> connectionsToRemove = new List<InteractableEntity>();

    void ProcessHits(int hitCount)
    {
        for (int i = 0; i < hitCount; i++)
        {
            var col = hits[i];
            var rig = col.attachedRigidbody;
            if (rig != rigidbody && rig != null)
            {
                var physicalObject = col.GetComponentInParent<InteractableEntity>();

                if (physicalObject != null
                    && physicalObject.physicalMaterial != null
                    && physicalObject.physicalMaterial.Magnetic &&
                    !connections.ContainsKey(physicalObject))
                {
                    connections.Add(physicalObject, new MagnetConnectionData(CreateJoint(rig), col, rig));
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!MagnetActive)
            return;

        var hitCount = Physics.OverlapSphereNonAlloc(reBody.worldCenterOfMass, magnetRange, hits, affectedLayers);
        ProcessHits(hitCount);

        foreach (var entity in connections.Keys)
        {
            MagnetConnectionData connection = connections[entity];

            var closestPointToCollider = connection.collider.ClosestPointSafe(transform.position);
            var distanceToCollider = Vector3.Distance(transform.position, closestPointToCollider);

            if (distanceToCollider >= magnetRange)
            {
                connectionsToRemove.Add(entity);
                continue;
            }

            var joint = connection.joint;
            if (joint == null)
            {
                joint = CreateJoint(connection.rigidbody);
                connection.AddJoint(joint);
            }

            var magnetDir = transform.TransformDirection(magnetDirection);

            //Update drive froces
            {
                var x = joint.xDrive;
                var y = joint.yDrive;
                var z = joint.zDrive;

                float multi = 0;
                if (ShouldUseGuides(connection, closestPointToCollider, magnetDir))
                {
                    var distanceToCenterOfMass = Vector3.Distance(transform.position, connection.reBody.worldCenterOfMass);
                    var clampedDistance = Mathf.Clamp01(distanceToCenterOfMass / magnetRange);

                    multi = Mathf.Pow(1f - clampedDistance, 3) + 0.01f;
                    joint.connectedAnchor = connection.reBody.centerOfMass;
                }

                var force = maxMagnetForce * multi;

                x.positionSpring = force;
                y.positionSpring = force;
                z.positionSpring = force;

                joint.xDrive = x;
                joint.yDrive = y;
                joint.zDrive = z;
            }

            //snap
            connection.TrySnap(distanceToCollider, closestPointToCollider);

            if (connection.SnapJointLocked)
            {
                //is pull direction the same as magnet direction
                if (Vector3.Dot(magnetDir, joint.currentForce.normalized) > 0 && entity.IsGrabbed)
                {
                    joint.breakForce = Mathf.Infinity;
                    // the numbers get really big really fast so need to make them smaller
                    connection.acumulatedAwayForce += joint.currentForce.magnitude / 100;
                }

                if (!entity.IsGrabbed)
                {
                    joint.connectedAnchor = connection.reBody.InverseTransformPoint(closestPointToCollider);
                    joint.breakForce = connection.reBody.mass * 100;
                }

                connection.TryUnsnap(snappedObjectAcumulatedForceMulti);
            }
            else
            {
                connection.timeSinceLastSnapJoint += Time.fixedDeltaTime;
            }
        }


        foreach (var toRemove in connectionsToRemove)
        {
            Disconnect(toRemove);
        }
        connectionsToRemove.Clear();
    }

    bool ShouldUseGuides(MagnetConnectionData connection, Vector3 closestPointToCollider, Vector3 magnetDir)
    {
        if (!connection.SnapJointLocked)
        {
            if (allDirectionsPull)
            {
                return true;
            }
            else
            {
                if (Vector3.Dot(magnetDir, closestPointToCollider - reBody.worldCenterOfMass) > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void DisconnectAll()
    {
        foreach (var key in connections.Keys)
        {
            Destroy(connections[key].joint);
        }

        connections.Clear();
    }

    void Disconnect(InteractableEntity key)
    {
        Destroy(connections[key].joint);
        connections.Remove(key);
    }

    ConfigurableJoint CreateJoint(Rigidbody target)
    {
        var joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = target;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.anchor = Vector3.zero;
        joint.enableCollision = true;
        return joint;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }

    internal class MagnetConnectionData
    {
        internal ConfigurableJoint joint;

        //may possibly need to use a list of colliders with the same parent InteractableEntity
        internal Collider collider;
        internal Rigidbody rigidbody;
        internal ReBody reBody;
        internal float acumulatedAwayForce;
        internal float timeSinceLastSnapJoint;

        internal bool SnapJointLocked => joint != null && snapped;
        bool snapped;

        const float kSnapLimit = 0.01f;
        const float kSnapCooldown = 1f;

        internal MagnetConnectionData(ConfigurableJoint joint, Collider collider, Rigidbody rigidbody)
        {
            this.joint = joint;
            this.collider = collider;
            this.rigidbody = rigidbody;
            reBody = new ReBody(rigidbody);

            acumulatedAwayForce = 0;
            timeSinceLastSnapJoint = 0;
            snapped = false;
        }

        internal void AddJoint(ConfigurableJoint joint)
        {
            this.joint = joint;
            snapped = false;
        }

        internal void TrySnap(float distanceToCollider, Vector3 closestPointToCollider)
        {
            if (ShouldSnap(distanceToCollider))
            {
                Snap(closestPointToCollider);
            }
        }

        internal void TryUnsnap(float snappedObjectAcumulatedForceMulti)
        {
            if (ShouldUnsnap(snappedObjectAcumulatedForceMulti))
            {
                Unsnap();
            }
        }

        bool ShouldSnap(float distanceToCollider)
        {
            return distanceToCollider <= kSnapLimit && timeSinceLastSnapJoint >= kSnapCooldown && !SnapJointLocked;
        }

        bool ShouldUnsnap(float snappedObjectAcumulatedForceMulti)
        {
            return acumulatedAwayForce > snappedObjectAcumulatedForceMulti * reBody.mass;
        }

        void Snap(Vector3 closestPointToCollider)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.connectedAnchor = reBody.InverseTransformPoint(closestPointToCollider);

            snapped = true;
        }

        void Unsnap()
        {
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;

            acumulatedAwayForce = 0;
            timeSinceLastSnapJoint = 0;

            snapped = false;
        }
    }

}

