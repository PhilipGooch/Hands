using NBG.Core;
using Recoil;
using System;
using System.Collections.Generic;
using UnityEngine;
using VR.System;

public class Hand : MonoBehaviour
{
    [SerializeField]
    HandPositions handPositions;
    [SerializeField]
    HandVisuals handVisuals;

    public Hand otherHand;

    public float Trigger => VRSystem.Instance.ReadTriggerValue(handDirection);
    public float Grab => VRSystem.Instance.GetGrabAmount(handDirection);
    public Vector2 MoveDir => VRSystem.Instance.ReadMoveInput(handDirection);

    //public bool IsThreat => attachedBody == null && Trigger > 0 && !PlayerUIManager.Instance.InteractingWithUI;

    public HandPositions HandPositions => handPositions;

    Dictionary<HandInputType, bool> fixedUpdateHandInputs = new Dictionary<HandInputType, bool>();

    public HandDirection handDirection = HandDirection.Left;

    public Rigidbody attachedBody;
    ReBody attachedReBody;
    public Vector3 attachedTensor;
    GrabParams grabParams;

    Vector3 attachedAnchorPos = Vector3.zero;
    Vector3 anchorPos = Vector3.zero;
    Quaternion anchorRot = Quaternion.identity;
    public Vector3 worldAnchorPos => pos + rot * anchorPos;
    public Vector3 worldAttachedAnchorPos => attachedReBody.TransformPoint(attachedAnchorPos);

    public bool TwoHandMaster { get; private set; }
    public bool HoldingObjectWithTwoHands => TwoHandMaster || otherHand.TwoHandMaster;
    Vector3 twoHandAxis;
    float twoHandTwist;
    bool canInteract = true;

    public Vector3 pos;
    public Quaternion rot;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public static event Action<Hand, float> onHandFreeTrigger;
    public static event Action<Rigidbody> onAttachObject;
    public static event Action<Rigidbody> onDetachObject;

    float herdingDuration = 0f;
    [SerializeField]
    float herdGrabGracePeriod = 0.25f;

    private void Start()
    {
        //PlayerUIManager.Instance.onUIInteractionStarted += OnUIVisible;
        //PlayerUIManager.Instance.onUIInteractionEnded += OnUIHidden;
    }

    private void OnDestroy()
    {
        //PlayerUIManager.Instance.onUIInteractionStarted -= OnUIVisible;
        //PlayerUIManager.Instance.onUIInteractionEnded -= OnUIHidden;
    }

    public bool GetInput(HandInputType inputType)
    {
        if (Time.inFixedTimeStep)
        {
            if (fixedUpdateHandInputs.ContainsKey(inputType))
            {
                return fixedUpdateHandInputs[inputType];
            }
            return false;
        }
        else
        {
            return ReadInputFromDevice(inputType);
        }
    }

    bool ReadInputFromDevice(HandInputType inputType)
    {
        var result = VRSystem.Instance.ReadInputFromDevice(handDirection, inputType);
        return result;
    }

    public List<Collider> collidersInRange = new List<Collider>();
    Collider FindNearest(List<Collider> objects, ref Vector3 pos)
    {
        float minDist = float.MaxValue;
        Collider minCollider = null;
        Vector3 minPos = pos;
        for (int i = 0; i < objects.Count; i++)
        {
            bool isSheep = Tags.IsSheep(objects[i].gameObject);

            var pt = isSheep ?
                objects[i].transform.position :
                objects[i].ClosestPointSafe(pos);
            float currentDist = (pos - pt).magnitude;

            var grabBinding = objects[i].GetComponentInParent<GrabParamsBinding>();
            if (grabBinding)
            {
                currentDist -= grabBinding.Priority;
            }

            if (currentDist < minDist)
            {
                minDist = currentDist;
                minCollider = objects[i];
                minPos = pt;
            }
        }
        pos = minPos;
        return minCollider;
    }

    void UpdateCollidersInRange()
    {
        for (int i = collidersInRange.Count - 1; i >= 0; i--)
        {
            // Object destroyed
            if (collidersInRange[i] == null)
            {
                collidersInRange.RemoveAt(i);
            }
            else if (!collidersInRange[i].gameObject.activeInHierarchy)
            {
                collidersInRange.RemoveAt(i);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Tags.IsGrab(other.gameObject) || other.isTrigger)
            return;
        collidersInRange.Add(other);
    }
    void OnTriggerExit(Collider other)
    {
        collidersInRange.Remove(other);
    }

    public void ReadPose(Vector3 vrPos, Quaternion vrRot, Vector3 vrVelocity, Vector3 vrAngularVelocity)
    {
        if (!vrPos.IsFinite()) vrPos = pos;

        // The velocity we receive is local. If the player is rotated it will be wrong. To get the world velocity we need to multiply by the player rot
        var playerRotation = Player.Instance.vrLoco.transform.rotation;
        if (!vrVelocity.IsFinite()) vrVelocity = velocity;
        else vrVelocity = playerRotation * vrVelocity;
        if (!vrAngularVelocity.IsFinite()) vrAngularVelocity = angularVelocity;
        else vrAngularVelocity = playerRotation * vrAngularVelocity;

        if (attachedBody != null) // anchor reprojection - to avoid forces in certain directions
        {
            //var project = attachedBody.GetComponent<IProjectHandAnchor>();
            //if (project != null)
            //{
            //    //vrRot = attachedBody.rotation * Quaternion.Inverse(anchorRot);
            //    //vrPos = attachedBody.TransformPoint(attachedAnchorPos) - attachedBody.rotation * Quaternion.Inverse(anchorRot)* anchorPos;
            //    var p = vrRot * anchorPos + vrPos;
            //    var r = (vrRot * anchorRot).normalized;
            //
            //    project.Project(ref p, ref r, ref vrVelocity, ref vrAngularVelocity, attachedAnchorPos, anchorRot);
            //    vrRot = (r * Quaternion.Inverse(anchorRot)).normalized;
            //    vrPos = p - vrRot * anchorPos;
            //}

            var constraint = attachedBody.GetComponent<IConstraint>();
            if (constraint != null)
            {
                vrRot = vrRot.EnsureValid();
                anchorRot = anchorRot.EnsureValid();
                var X = new PluckerTranslate(attachedReBody.position - vrPos);
                var Xinv = new PluckerTranslate(-X.r);
                var vrVel = new Vector6(vrAngularVelocity, vrVelocity);
                var vel = X.TransformVelocity(vrVel);

                var p = vrRot * anchorPos + vrPos - (vrRot * anchorRot) * attachedAnchorPos;
                var r = vrRot * anchorRot;
                r = r.EnsureValid();
                constraint.Apply(ref p, ref r, ref vel);

                vrRot = r * Quaternion.Inverse(anchorRot);
                vrRot = vrRot.EnsureValid();

                vrPos = p - vrRot * anchorPos + (vrRot * anchorRot) * attachedAnchorPos;

                vrVel = Xinv.TransformVelocity(vel);
                vrAngularVelocity = vrVel.angular;
                vrVelocity = vrVel.linear;
            }
        }

        pos = vrPos;// pose.transform.position;
        rot = vrRot;// pose.transform.rotation;
        velocity = vrVelocity;// (pos - lastPos) / Time.fixedDeltaTime;
        angularVelocity = vrAngularVelocity;// (rot * Quaternion.Inverse(lastRot)).QuaternionToAngleAxis() / Time.fixedDeltaTime;
    }

    void Update()
    {
        SetInputsForFixedUpdate();
        //if (throwAndDetach) return;
        UpdateOutline();

        var generatingThreat = canInteract && attachedBody == null && Trigger > 0;
        if (generatingThreat)
        {
            if (herdingDuration < herdGrabGracePeriod)
            {
                herdingDuration += Time.deltaTime;
            }
        }
        else
        {
            if (herdingDuration > 0)
            {
                herdingDuration -= Time.deltaTime;
            }
        }
    }

    void UpdateOutline()
    {
        var grabPos = pos;
        var nearestCollider = FindNearest(collidersInRange, ref grabPos);
        handVisuals.UpdateOutline(nearestCollider, attachedBody, false);
    }

    bool GrabIsPossible()
    {
        return canInteract && herdingDuration < herdGrabGracePeriod;
    }

    HashSet<IGrabNotifications> grabNotificationList = new HashSet<IGrabNotifications>();
    List<IGrabNotifications> tempNotificationList = new List<IGrabNotifications>();
    HashSet<IGrabNotifications> GetGrabNotificationsForBody(Rigidbody target)
    {
        grabNotificationList.Clear();
        if (target != null)
        {
            tempNotificationList.Clear();
            target.GetComponentsInChildren(false, tempNotificationList);
            foreach (var child in tempNotificationList)
            {
                grabNotificationList.Add(child);
            }
            tempNotificationList.Clear();
            target.GetComponentsInParent(false, tempNotificationList);
            foreach (var parent in tempNotificationList)
            {
                grabNotificationList.Add(parent);
            }
        }
        return grabNotificationList;
    }

    void SendGrabNotifications(Rigidbody body, bool grabbing, bool firstGrab)
    {
        // Sends a grab notification to any children and parents with a IGrabNotification component
        var targets = GetGrabNotificationsForBody(body);
        if (targets != null)
        {
            foreach (var target in targets)
            {
                if (grabbing)
                {
                    target.OnGrab(this, firstGrab);
                }
                else
                {
                    target.OnRelease(this, firstGrab);
                }
            }
        }
    }

    static HandInputType[] cachedHandInputTypes;

    void SetInputsForFixedUpdate()
    {
        if (cachedHandInputTypes == null)
        {
            // This generates garbage, cache the values to avoid it
            cachedHandInputTypes = (HandInputType[])Enum.GetValues(typeof(HandInputType));
        }

        foreach (HandInputType inputType in cachedHandInputTypes)
        {
            var realInput = ReadInputFromDevice(inputType);
            if (fixedUpdateHandInputs.ContainsKey(inputType))
            {
                fixedUpdateHandInputs[inputType] = fixedUpdateHandInputs[inputType] || realInput;
            }
            else
            {
                fixedUpdateHandInputs[inputType] = realInput;
            }
        }
    }

    public void FlushFixedUpdateInputs()
    {
        fixedUpdateHandInputs.Clear();
    }

    void RecoverVisualPosition()
    {
        handVisuals.RecoverVisualPosition();
    }

    public void OnLateUpdate()
    {
        handVisuals.UpdateVisuals(pos, rot, attachedReBody, grabParams, attachedAnchorPos, anchorPos, anchorRot, HoldingObjectWithTwoHands);
    }

    public void InterceptGrab(Rigidbody bodyToGrab, Vector3 grabPosition)
    {
        if (attachedBody != null)
        {
            ReleaseGrabbedObject();
        }

        if (bodyToGrab != null)
        {
            AttachObject(bodyToGrab, grabPosition);
        }
    }

    void AttachObject(Rigidbody grabbedBody, Vector3 grabPos)
    {
        AttachObject(pos, rot, grabbedBody, grabPos);
    }

   // Vector3 originalInertiaTensor;
  //  float originalMaxAngularVelocity;

    void AttachObject(Vector3 handPos, Quaternion handRot, Rigidbody grabbedBody, Vector3 grabPos)
    {
        attachedBody = grabbedBody;
        attachedReBody = new ReBody(attachedBody);

     //   originalInertiaTensor = attachedReBody.inertiaTensor;
    //    originalMaxAngularVelocity = attachedReBody.maxAngularVelocity;
   
        attachedReBody.maxAngularVelocity = 30;

        grabParams = GrabParamsBinding.GetParams(attachedBody.gameObject);

        onAttachObject?.Invoke(attachedBody);

        //var sheep = attachedBody.GetComponentInParent<Sheep>();
        //var isSheep = sheep != null;

        //if (isSheep)
        //{
        //    grabParams = GrabParams.sheepParams;
        //    grabPos = attachedReBody.position;
        //    var audio = sheep.GetComponentInChildren<SheepAudio>();
        //    if (audio)
        //        audio.PlayVoice(1.5f);
        //}
        //if (otherHand.attachedBody != attachedBody && !isSheep)
        //    attachedReBody.mass *= grabParams.massMultiplier;

        var grabOverride = attachedBody.GetComponent<IOverrideGrabAnchor>();
        if (grabOverride != null)
        {
            (grabPos, handRot) = grabOverride.Reanchor(grabPos, handRot);
            handPos = grabPos;
        }

        var invRot = Quaternion.Inverse(handRot);
        anchorRot = invRot * attachedReBody.rotation;

        anchorPos = invRot * (grabPos - handPos);
        attachedAnchorPos = Quaternion.Inverse(attachedReBody.rotation) * (grabPos - attachedReBody.position);

        if (grabParams.snapToCollider && grabOverride == null)
            SnapToCollider(grabPos, handRot);

        smoothVelocity.Reset();

        var firstGrab = otherHand.attachedBody != grabbedBody;
        SendGrabNotifications(grabbedBody, true, firstGrab);
    }


    void DetachObject()
    {
        //var isSheep = attachedBody.GetComponentInParent<Sheep>() != null;
        //if (otherHand.attachedBody != attachedBody && !isSheep)
        //    attachedReBody.mass /= grabParams.massMultiplier;

        //reset grabbed trigger preasure;
        var triggerHandler = attachedBody != null ? attachedBody.GetComponent<ITriggerHandler>() : null;
        if (triggerHandler != null)
        {
            triggerHandler.OnHandTrigger(0);
        }

        // Apply throw velocity
        {
            var playerRotation = Player.Instance.vrLoco.transform.rotation;
            VRSystem.Instance.GetEstimatedPeakVelocities(handDirection, out var peakVelocity, out var peakAngular);
            if (!peakVelocity.IsFinite())
            {
                peakVelocity = Vector3.zero;
            }
            if (!peakAngular.IsFinite())
            {
                peakAngular = Vector3.zero;
            }
            peakVelocity *= PlayArea.scale;
            peakVelocity = playerRotation * peakVelocity;
            peakAngular = playerRotation * peakAngular;

            (var maxA, var maxAngularA) = CalculateAccelerationLimits(attachedAnchorPos, rot);

            Dynamics.ApplyConstantDeceleration(attachedReBody, attachedReBody.TransformPoint(attachedAnchorPos), worldAnchorPos, peakVelocity, maxA, grabParams.maxLinearVelocity,
                        rot * anchorRot, peakAngular, maxAngularA, grabParams.maxAngularVelocity);
        }

        var firstGrab = otherHand.attachedBody != attachedBody;

      //  attachedReBody.inertiaTensor = originalInertiaTensor;
     //   attachedReBody.maxAngularVelocity = originalMaxAngularVelocity;
     
        SendGrabNotifications(attachedBody, false, firstGrab);
        onDetachObject?.Invoke(attachedBody);

        attachedBody = null;
        attachedReBody = ReBody.Empty();
    }

    void EnsureAttachedObjectValid()
    {
        if (attachedBody != null && !attachedBody.gameObject.activeInHierarchy)
        {
            ReleaseGrabbedObject();
        }
    }

    void ConvertToTwoHanded(Vector3 grabPos)
    {
        TwoHandMaster = true;
        //twoHandLockTogether = false;// attachedBody.isKinematic; //grabParams.hasAngularControl;

        var grabbedBody = attachedBody;
        RecoverVisualPosition();
        //if (twoHandLockTogether)
        //{
        //    // attach at middle

        //    twoHandPos = (worldAnchorPos + otherHand.worldAnchorPos) / 2;
        //    twoHandRot = Quaternion.identity;
        //    //twoHandAnchor.transform.rotation.SetLookRotation(lastPos-otherHand.lastPos);
        //    twoHandAnchorRot = attachedBody.rotation;

        //    DetachObject();
        //    AttachObject(twoHandPos, twoHandRot, grabbedBody, twoHandPos);
        //}
        //else
        {
            otherHand.AttachObject(grabbedBody, grabPos);
            //twoHandPos = (worldAnchorPos + otherHand.worldAnchorPos) / 2;
            //twoHandRot = Quaternion.identity;

            //twoHandAnchorRot = attachedBody.rotation;
        }
    }

    void ConvertToOneHanded(Hand holdingHand)
    {
        TwoHandMaster = false;

        var regrabAnchorPos = holdingHand.anchorPos;
        var regrabAttachedPos = holdingHand.attachedAnchorPos;

        // release tension along two hand axis
        var anchorAxis = (holdingHand.worldAnchorPos - holdingHand.otherHand.worldAnchorPos).normalized;
        var anchorDist = (holdingHand.worldAnchorPos - holdingHand.otherHand.worldAnchorPos).magnitude;
        var attachedAnchorDist = (holdingHand.attachedAnchorPos - holdingHand.otherHand.attachedAnchorPos).magnitude;
        var correction = anchorAxis * (anchorDist - attachedAnchorDist) / 2;
        regrabAttachedPos = attachedReBody.InverseTransformPoint(holdingHand.worldAttachedAnchorPos + correction);

        if (!grabParams.reanchor) // keep original position of grabbing
        {
            regrabAnchorPos = holdingHand.anchorPos;
            regrabAttachedPos = holdingHand.attachedAnchorPos;
        }

        holdingHand.otherHand.DetachObject();

        // ALT1. Anchor at edge of collider
        //var pos = holdingHand.attachedCollider.ClosestPoint(holdingHand.pose.transform.position);

        //// if outside hand bubble, move in by at least 10 cm
        //var local = holdingHand.pose.transform.InverseTransformPoint(pos);
        //if (local.magnitude > .5f)
        //    pos += holdingHand.pose.transform.TransformDirection(local.normalized * .1f);

        // ALT2. Anchor in the hand

        holdingHand.anchorPos = regrabAnchorPos;
        holdingHand.attachedAnchorPos = regrabAttachedPos;
    }

    public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude)
    {
        VRSystem.Instance.Vibrate(secondsFromNow, duration, vibrationFrequency, amplitude, handDirection);
    }

    public void OnFixedStep()
    {
        UpdateCollidersInRange();

        if (GetInput(HandInputType.grabDown) && GrabIsPossible() && collidersInRange.Count != 0)
        {
            var grabPos = pos;
            var collider = FindNearest(collidersInRange, ref grabPos);
            var grabbedBody = collider.GetComponentInParent<Rigidbody>();
            var grabParamsBinding = grabbedBody.GetComponentInParent<GrabParamsBinding>();
            // Check if grab is not disabled
            var canGrab = grabParamsBinding == null || grabParamsBinding.Grabbable;

            if (canGrab)
            {
                if (otherHand.attachedBody != grabbedBody) // begin grab
                {
                    var grabbedReBody = new ReBody(grabbedBody);
                    // unkew tensor to prevent jitter
                    attachedTensor = grabbedReBody.inertiaTensor;
                    var max = Mathf.Max(Mathf.Max(attachedTensor.x, attachedTensor.y), attachedTensor.z) / 10;
                    var unskewed = new Vector3(Mathf.Max(attachedTensor.x, max), Mathf.Max(attachedTensor.y, max), Mathf.Max(attachedTensor.z, max));
                    if (unskewed != attachedTensor)
                        grabbedReBody.inertiaTensor = unskewed.normalized * attachedTensor.magnitude;

                    AttachObject(grabbedBody, grabPos);
                }
                else // transform to 2 hand grab
                {
                    attachedTensor = otherHand.attachedTensor;
                    otherHand.ConvertToTwoHanded(grabPos);
                }
            }
        }
        if (GetInput(HandInputType.grabUp))
        {
            ReleaseGrabbedObject();
        }

        EnsureAttachedObjectValid();
        //pose.GetEstimatedPeakVelocities(out var velocity, out var angularVelocity);
        //velocity *= PlayArea.scale;

        if (attachedBody != null)
        {
            if (grabParams.reanchor && !attachedReBody.isKinematic)
                Reanchor();

            if (TwoHandMaster)
                MoveTwoHandCenter();
            MoveAttachedBody();
            var triggerHandler = attachedBody != null ? attachedBody.GetComponent<ITriggerHandler>() : null;
            if (triggerHandler != null)
            {
                if (otherHand.attachedBody == attachedBody)
                {
                    if (TwoHandMaster)
                        triggerHandler.OnHandTrigger(Mathf.Max(Trigger, otherHand.Trigger));
                }
                else
                    triggerHandler.OnHandTrigger(Trigger);
            }
        }
        else
        {
            onHandFreeTrigger?.Invoke(this, Trigger);
        }
    }

    public void ReleaseGrabbedObject()
    {
        if (attachedBody != null)
            RecoverVisualPosition();

        if (TwoHandMaster) // convert to single hand grab
        {
            ConvertToOneHanded(otherHand);
        }
        else if (otherHand.TwoHandMaster) // convert to single hand grab
        {
            otherHand.ConvertToOneHanded(otherHand);
        }
        else if (attachedBody != null)
        {
            if (attachedReBody.inertiaTensor != attachedTensor)
                attachedReBody.inertiaTensor = attachedTensor;
            DetachObject();
        }
    }

    SmoothVector3 smoothVelocity = new SmoothVector3(10);

    private void Reanchor()
    {
        var dt = Time.fixedDeltaTime;

        var twistMagnitude = 0f;
        var irlVelocity = smoothVelocity.Enque(this.velocity) / PlayArea.scale; // smooth it (<.1 slow >.3 fast)
                                                                                // angular reanchor
        if (grabParams.angularControl)
        {
            // check if too much twist - reanchor rotation
            var twist = (this.rot * anchorRot * Quaternion.Inverse(attachedReBody.rotation)).QToAngleAxis();
            var angularRange = grabParams.angularAnchorRange;
            angularRange *= .25f + irlVelocity.magnitude / .1f; // the slower hand moves the easier is reanchoring
            twistMagnitude = twist.magnitude; // increase linear range when big angular errors

            // does not work that well for angular
            //dot = Vector3.Dot(twist.normalized, angularVelocity);
            //if (dot > 0) angularRange  = Mathf.Lerp(range, 1, dot / .1f);// extend range to 1m if moving away from anchor at 10cm/s
            //else angularRange  = Mathf.Lerp(range, range * .5f, -dot / .1f);// compress range to half if moving towards anchor at 10cm/s

            if (twist.magnitude > angularRange)
            {
                // approach target anchor
                //twist = Vector3.Lerp(twist, Vector3.ClampMagnitude(twist, angularAnchorMargin), dt); // proportional
                twist = Vector3.MoveTowards(twist, Vector3.ClampMagnitude(twist, angularRange), grabParams.angularAnchorSpeed * dt); // at specific speed

                anchorRot = Quaternion.Inverse(this.rot) * twist.AngleAxisToQuaternion() * attachedReBody.rotation;
            }
        }

        // Linear reanchor
        var attachedAnchorWorldPos = attachedReBody.TransformPoint(attachedAnchorPos);

        // calculate allowed joint error range (bigger if moving away from attachedAnchor, bigger is there's substantial angular force)
        var range = grabParams.linearAnchorRange; // range at rest
        var dot = Vector3.Dot((worldAnchorPos - attachedAnchorWorldPos).normalized, irlVelocity);
        if (dot > 0.1f) range *= 1 + (dot - .1f) * 10;  // extend range when moving towards anchor by 1m for every real life 10cm/s

        //else range = Mathf.Lerp(range, range*.5f, -dot / .1f);// compress range to half if moving towards anchor at 10cm/s
        //range+=.5f*twistMagnitude/1.57f; // extend range by .5 for 90 degees error

        //range = Mathf.Max(range, velocity.magnitude * .1f); // 100ms velocity
        //range = Mathf.Max(range, angularVelocity.magnitude * .1f); // 100ms velocity
        var bodyVelocity = attachedReBody.GetPointVelocity(attachedAnchorWorldPos);
        //if (bodyVelocity.magnitude> .1f)range = 100;
        //range = Mathf.Max(range, attachedBody.velocity.magnitude*.01f); // 100ms velocity
        //range = Mathf.Max(range, attachedBody.angularVelocity.magnitude * .1f); //100ms angular vel

        if ((worldAnchorPos - attachedAnchorWorldPos).magnitude > range)
        {
            var targetAttachedAnchorWorldPos = attachedAnchorWorldPos.ClampToAnchor(worldAnchorPos, range); // pull towards hand's anchor position

            // optional: limit to .5m from collider
            //if (grabParams.snapToCollider)
            //{
            //    var colliderPos = attachedBody.ClosestPoint(targetAttachedAnchorWorldPos);
            //    targetAttachedAnchorWorldPos = targetAttachedAnchorWorldPos.ClampToAnchor(colliderPos, .5f);
            //}

            var delta = targetAttachedAnchorWorldPos - attachedAnchorWorldPos;
            var deltaDir = delta.normalized;
            var deltaMag = delta.magnitude;

            // clamp reanchor velocity
            deltaMag = Mathf.Min(deltaMag, grabParams.linearAnchorSpeed * dt);

            // calculate split between hand and attached anchor movement
            var relativeAnchorPos = this.rot * anchorPos;
            var moveAnchorAmount = Mathf.Clamp(Vector3.Dot(relativeAnchorPos, deltaDir), 0, deltaMag);
            var moveAttachedAnchorAmount = deltaMag - moveAnchorAmount;

            // calculate new positions
            anchorPos -= Quaternion.Inverse(this.rot) * deltaDir * moveAnchorAmount;
            attachedAnchorPos = attachedReBody.InverseTransformPoint(attachedAnchorWorldPos + deltaDir * moveAttachedAnchorAmount);

            // snap to collider
            if (grabParams.snapToCollider)
                SnapToCollider(this.pos, this.rot);
        }
    }

    private void MoveTwoHandCenter()
    {
        var dt = Time.fixedDeltaTime;

        var axis = (worldAnchorPos - otherHand.worldAnchorPos).normalized;
        var twoHandAttachedAnchorPos = (attachedAnchorPos + otherHand.attachedAnchorPos) / 2;

        twoHandAxis = axis; // store

        // get twists from each hand
        var twist1 = Vector3.Dot(angularVelocity, axis);
        var twist2 = Vector3.Dot(otherHand.angularVelocity, axis);

        // mix twist angles based on square magnitude
        var mix = .5f;
        var sqrSum = twist1 * twist1 + twist2 * twist2;
        if (sqrSum > 0.001f)
            mix = twist2 * twist2 / sqrSum;
        var twistVelocity = Mathf.Lerp(twist1, twist2, mix);

        // change rotational anchors to match the twist mixing
        anchorRot = ((twistVelocity - twist1) * dt * (Quaternion.Inverse(rot) * axis)).AngleAxisToQuaternion() * anchorRot;
        otherHand.anchorRot = ((twistVelocity - twist2) * dt * (Quaternion.Inverse(otherHand.rot) * axis)).AngleAxisToQuaternion() * otherHand.anchorRot;

        if (attachedReBody.isKinematic)
        {
            // adjust anchors to actual dist to prevent jump on release
            var dist = (worldAnchorPos - otherHand.worldAnchorPos).magnitude;
            var anchorAxis = attachedAnchorPos - otherHand.attachedAnchorPos;
            attachedAnchorPos = twoHandAttachedAnchorPos + anchorAxis.normalized * dist / 2;
            otherHand.attachedAnchorPos = twoHandAttachedAnchorPos - anchorAxis.normalized * dist / 2;

            // body rotation swing to align axis, then twist based on input
            var finalRot = (twistVelocity * dt * axis).AngleAxisToQuaternion() * Quaternion.FromToRotation(worldAttachedAnchorPos - otherHand.worldAttachedAnchorPos, axis) * attachedReBody.rotation;

            //attachedBody.MovePosition((worldAnchorPos + otherHand.worldAnchorPos) / 2 - finalRot * twoHandAttachedAnchorPos);
            //attachedBody.MoveRotation(finalRot);
            return;
        }
        else
        {
            // get params
            (var maxA2, var maxAngularA2) = CalculateAccelerationLimits(twoHandAttachedAnchorPos, rot);
            var worldAnchor = attachedReBody.TransformPoint(twoHandAttachedAnchorPos);

            if (grabParams.angularControl)
            {
                // integrate twist error
                twoHandTwist -= twistVelocity * dt;
                twoHandTwist += Vector3.Dot(attachedReBody.angularVelocity * dt, axis);

                // angular around twist axis
                var angAcceleration = axis * Dynamics.ConstantDeceleration(twoHandTwist, Vector3.Dot(attachedReBody.angularVelocity, axis), twistVelocity, 0, maxAngularA2, grabParams.maxAngularVelocity, dt); // use CD and twist
                attachedReBody.AddAngularAccelerationAtPosition(angAcceleration, worldAnchor);
            }
            //linear motion along normal
            var twoHandPos = (worldAnchorPos + otherHand.worldAnchorPos) / 2;
            var twoHandVelocity = (this.velocity + otherHand.velocity) / 2;
            Dynamics.ApplyConstantDecelerationLinearProjected(twoHandAxis, attachedReBody, worldAnchor, twoHandPos, twoHandVelocity, Physics.gravity, maxA2, grabParams.maxLinearVelocity);
        }
    }

    private void MoveAttachedBody()
    {
        var dt = Time.fixedDeltaTime;
        // full velocity tracking more immediate but overshoots, zero tracking a bit laggy but conservative
        float velocityTracking = 1;// grabParams.velocityTracking;
        (var maxA, var maxAngularA) = CalculateAccelerationLimits(attachedAnchorPos, rot);

        if (attachedReBody.isKinematic)// kinematic bodies are not moved
        {
            return;
            /*if (twoHandMaster || otherHand.twoHandMaster) return;

            attachedBody.MovePosition(worldAnchorPos - (rot * anchorRot) * attachedAnchorPos);
            attachedBody.MoveRotation(rot * anchorRot);*/
        }
        else if (HoldingObjectWithTwoHands)
        {
            var normal = otherHand.TwoHandMaster ? otherHand.twoHandAxis : twoHandAxis;
            // Only apply half of the wanted acceleration, since we will be applying the other half from the other hand.
            // Otherwise we get a judder when holding an object with two hands nearby
            Dynamics.ApplyConstantDecelerationLinearProjectedOnPlane(normal, attachedReBody, attachedReBody.TransformPoint(attachedAnchorPos), worldAnchorPos, velocityTracking * velocity, .5f * Physics.gravity, maxA, grabParams.maxLinearVelocity, 0.5f);
        }
        else
        {
            var pbd = attachedBody.GetComponent<IPositionBasedDynamics>();
            var treePhysics = attachedBody.GetComponent<ITreePhysics>();
            if (pbd != null)
            {
                pbd.ApplyPosition(worldAnchorPos, rot * anchorRot, attachedAnchorPos);
            }
            else if (treePhysics != null && treePhysics.TreePhysicsActive())
            {
                var worldPos = attachedReBody.TransformPoint(attachedAnchorPos);
                var vel = treePhysics.CalculateTreeVelocity(worldPos);
                var acc = Dynamics.CalculateConstantDecceleration(worldPos, worldAnchorPos, attachedBody.rotation, rot * anchorRot, vel, new Vector6(velocityTracking * angularVelocity, velocityTracking * velocity), maxA, grabParams.maxLinearVelocity, maxAngularA, grabParams.maxAngularVelocity);
                treePhysics.AddTreeAcceleration(worldPos, acc);
                //Debug.Log($"{vel},{Dynamics.GetPointVelocity(attachedBody, worldPos)}");
            }
            else
            {
                if (!grabParams.angularControl)
                    Dynamics.ApplyConstantDecelerationLinear(attachedReBody, attachedReBody.TransformPoint(attachedAnchorPos), worldAnchorPos, velocityTracking * velocity, Physics.gravity, maxA, grabParams.maxLinearVelocity);
                else
                    Dynamics.ApplyConstantDeceleration(attachedReBody, attachedReBody.TransformPoint(attachedAnchorPos), worldAnchorPos, velocityTracking * velocity, maxA, grabParams.maxLinearVelocity,
                        rot * anchorRot, velocityTracking * angularVelocity, maxAngularA, grabParams.maxAngularVelocity);
            }
        }
    }
    private void SnapToCollider(Vector3 pos, Quaternion rot)
    {
        var attachedAnchorWorldPos = attachedReBody.TransformPoint(attachedAnchorPos);

        // reproject to collider
        var project = attachedBody.GetComponent<IProjectOnCollider>();
        // TODO: Optimize
        var colliderPos = attachedBody.ClosestPoint(attachedAnchorWorldPos);
        if (project != null)
            colliderPos = project.Project(colliderPos);

        var delta = colliderPos - attachedAnchorWorldPos;
        attachedAnchorPos = attachedReBody.InverseTransformPoint(attachedAnchorWorldPos + delta);
        anchorPos += Quaternion.Inverse(rot) * delta;

        // stay within hand
        var anchorWorldPos = worldAnchorPos;
        anchorPos = Quaternion.Inverse(rot) * (anchorWorldPos.ClampToAnchor(pos, .5f) - pos);
    }

    private (float maxA, float maxAngularA) CalculateAccelerationLimits(Vector3 attachedAnchorPos, Quaternion rot)
    {
        // some default values
        var maxA = grabParams.maxLinearAcceleration;
        var maxAngularA = grabParams.maxAngularAcceleration;

        // autocalculate: scale acceleration based on object weight
        if (maxA == 0)
        {
            // Clamp max acceleration at 250. Larger values introduce jittering when objects collide.
            maxA = Mathf.Clamp(500 * 50 / attachedReBody.mass, 0, 250);
        }

        if (grabParams.angularControl && maxAngularA == 0)
        {
            // calculate max acceleration based on how "heavy" the objects appears when rotating to target
            var worldAnchor = attachedReBody.TransformPoint(attachedAnchorPos);
            var angPos = (attachedReBody.rotation * Quaternion.Inverse(rot)).QToAngleAxis();
            maxAngularA = maxA * attachedReBody.mass / Dynamics.TensorAtPointAxis(attachedReBody, attachedReBody.worldCenterOfMass - worldAnchor, angPos.normalized);
        }
        return (maxA, maxAngularA);
    }

    void OnUIVisible()
    {
        canInteract = false;
        ReleaseGrabbedObject();
    }

    void OnUIHidden()
    {
        canInteract = true;
    }

    public void Restart()
    {
        collidersInRange.Clear();
        attachedBody = null;
    }
}


