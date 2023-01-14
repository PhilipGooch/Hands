using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using System.Linq;

public class Nail : GrabbableImpaler, IRespawnListener
{
    protected List<NailedObject> nailedObjects = new List<NailedObject>();
    protected List<ReBody> connectedBodies = new List<ReBody>();
    SimpleTreePhysics treePhysics;
    List<Nail> tempNailList = new List<Nail>();
    List<NailedObject> tempNailedObjects = new List<NailedObject>();

    void Awake()
    {
        treePhysics = new SimpleTreePhysics(connectedBodies);
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        treePhysics.AddTreeAcceleration(worldPos, acc);
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        return treePhysics.CalculateTreeVelocity(worldPos);
    }

    protected override void OnImpale()
    {
        base.OnImpale();
        SetNailFreeMovement(false);
    }

    protected override void OnUnimpale()
    {
        base.OnUnimpale();
        SetNailFreeMovement(true);
    }

    protected override void OnObjectPenetrated(GameObject target, Vector3 penetrationPosition)
    {
        base.OnObjectPenetrated(target, penetrationPosition);
        AddJointForBody(target, penetrationPosition);
        RegenerateConnectedBodies();
    }

    protected override void OnObjectDepenetrated(GameObject target)
    {
        base.OnObjectDepenetrated(target);
        RemoveJointForBody(target);
    }

    void AddJointForBody(GameObject obj, Vector3 penetrationPosition)
    {
        var otherRig = obj.GetComponentInParent<Rigidbody>();
        if (rigidbody == otherRig) // avoid connecting nail to itself
        {
            return;
        }

        var joint = rigidbody.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = otherRig;
        joint.autoConfigureConnectedAnchor = false;
        var distanceBetweenTipAndPenetration = penetrationPosition - NailTip;
        var connectedAnchorPos = Vector3.Lerp(NailStart, NailTip, 0.5f) + distanceBetweenTipAndPenetration;
        if (otherRig != null)
        {
            // rigidbody.InverseTransformPoint ignores scale
            connectedAnchorPos = otherRig.transform.InverseTransformPoint(connectedAnchorPos);
        }
        joint.connectedAnchor = connectedAnchorPos;
        joint.axis = impaleDirection;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.anchor = reBody.InverseTransformPoint(NailStart);
        joint.enableCollision = true;
        var linearLimit = joint.linearLimit;
        // Avoid nail reaching its limit when being pulled out and getting stuck
        linearLimit.limit = nailLength * 0.5f + 0.25f;
        joint.linearLimit = linearLimit;

        AddNailedObject(obj, joint, otherRig, connectedAnchorPos);
    }

    protected void AddNailedObject(GameObject target, ConfigurableJoint joint, Rigidbody otherRig, Vector3 connectedAnchorPos)
    {
        if (otherRig != null)
        {
            target = otherRig.gameObject;
        }
        var nailedObject = target.AddComponent<NailedObject>();
        nailedObject.Initialize(this, joint, otherRig, connectedAnchorPos);
        nailedObject.CurrentlyBeingNailed = isGrabbed;
        nailedObject.onConnectionChange += RegenerateConnectedBodies;
        nailedObjects.Add(nailedObject);
    }

    protected void RemoveJointForBody(GameObject obj)
    {
        tempNailedObjects.Clear();
        obj.GetComponentsInParent(true, tempNailedObjects);
        foreach (var nailed in tempNailedObjects)
        {
            if (nailed.nailParent == this)
            {
                nailedObjects.Remove(nailed);
                nailed.RemoveSelf();
                break;
            }
        }
    }

    public void RegenerateConnectedBodies()
    {
        connectedBodies.Clear();
        tempNailList.Clear();

        FindConnectedBodiesRecursive(connectedBodies, tempNailList);

        foreach (var nail in tempNailList)
        {
            // All connected nails have the same bodies connected
            nail.connectedBodies.Clear();
            nail.connectedBodies.AddRange(connectedBodies);
        }
    }

    void FindConnectedBodiesRecursive(List<ReBody> currentBodies, List<Nail> encounteredNails)
    {
        if (!currentBodies.Contains(reBody))
        {
            currentBodies.Add(reBody);
        }
        foreach (var obj in nailedObjects)
        {
            var rig = obj.GetComponent<Rigidbody>();
            var otherRe = new ReBody(rig);
            if (otherRe.BodyExists && !currentBodies.Contains(otherRe))
            {
                currentBodies.Add(otherRe);
                var objectConnections = obj.GetComponents<NailedObject>();
                foreach (var connection in objectConnections)
                {
                    if (connection == null || connection.nailParent == null) // Connection was deleted this frame
                        continue;
                    var otherNail = connection.nailParent;
                    if (!currentBodies.Contains(otherNail.reBody) && !encounteredNails.Contains(otherNail))
                    {
                        encounteredNails.Add(otherNail);
                        otherNail.FindConnectedBodiesRecursive(currentBodies, encounteredNails);
                    }
                }
            }
        }
    }

    void SetNailFreeMovement(bool canMove)
    {
        foreach (var obj in nailedObjects)
        {
            var joint = obj.joint;
            var otherRig = obj.RecoilBody;
            if (canMove)
            {
                joint.anchor = reBody.InverseTransformPoint(NailStart);
                joint.connectedAnchor = obj.localNailPenetrationPosition;
            }
            else
            {
                joint.anchor = Vector3.zero;
                var connectedAnchorPos = reBody.position;
                if (otherRig.BodyExists)
                {
                    connectedAnchorPos = otherRig.InverseTransformPoint(connectedAnchorPos);
                }
                joint.connectedAnchor = connectedAnchorPos;
            }
            joint.xMotion = canMove ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
            obj.CurrentlyBeingNailed = canMove;
        }
    }

    public void OnNailedObjectDespawn()
    {
        RemoveAllConnections();
        //CheckForDestructableNailedObjects();
        //TeleportWholeTreeAroundObject(rigidbody, position, rotation);
    }

    void IRespawnListener.OnRespawn()
    {
    }

    void IRespawnListener.OnDespawn()
    {
        RemoveAllConnections();
    }

    protected void RemoveAllConnections()
    {
        int c = connectedBodies.Count - 1;

        for (int i = c; i >= 0; i--)
        {
            if (connectedBodies.Count <= i)
                continue;
            RemoveJointForBody(connectedBodies[i].rigidbody.gameObject);
        }
    }

    public void TeleportWholeTreeAroundObject(ReBody target, Vector3 position, Quaternion rotation)
    {
        for (int i = 0; i < connectedBodies.Count; i++)
        {
            var body = connectedBodies[i];
            if (body != target)
            {
                var positionOffset = target.InverseTransformPoint(body.position);
                var rotationOffset = Quaternion.Inverse(target.rotation) * body.rotation;

                var newPosition = position + rotation * positionOffset;
                var newRotation = rotation * rotationOffset;
                body.position = newPosition;
                body.rotation = newRotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }


        StartCoroutine(HoldUntilEndOfFixedUpdate(position, rotation));
    }

    IEnumerator HoldUntilEndOfFixedUpdate(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForFixedUpdate();

        reBody.velocity = Vector3.zero;
        reBody.angularVelocity = Vector3.zero;
        reBody.position = position;
        reBody.rotation = rotation;
    }
}
