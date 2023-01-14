using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public class SheepAnimation : MonoBehaviour
{
    [SerializeField]
    Animator animator;
    [SerializeField]
    Transform root;
    [SerializeField]
    Transform eyeL;
    [SerializeField]
    Transform eyeR;
    [SerializeField]
    AnimatedTransform[] spineTransforms;
    [SerializeField]
    AnimatedTransform[] neckTransforms;
    [SerializeField]
    [Tooltip("How far in degrees the sheep will bend while turning.")]
    float turnBendAmount = 30;
    [SerializeField]
    [Tooltip("How hard will the sheep react to turning.")]
    float turnBendMultiplier = 0.5f;
    [SerializeField]
    [Tooltip("How fast should the sheep wiggle while falling.")]
    float fallWiggleSpeed = 1f;
    [SerializeField]
    [Tooltip("How far in degrees the sheep will wiggle while it's falling to it's doom.")]
    float fallWiggleAmount = 30f;
    [SerializeField]
    [Range(0f, 90f)]
    float maxInnerEyeAngle = 10f;
    [SerializeField]
    [Range(0f, 90f)]
    float maxOuterEyeAngle = 10f;
    [SerializeField]
    [Range(0f,1f)]
    float turnToPlayerAmount = 0.6f;
    [SerializeField]
    int idleAnimationCount = 2;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("How fast does the sheep rotation change to match the locomotion rotation. Higher value might lead to jittery movement, low value might lead to strafing.")]
    float rotationChangeSpeed = 0.5f;

    Sheep sheep;
    SheepAudio sheepAudio;

    Quaternion initialHeadWorldRot;
    AnimatedTransform headTransform;
    Quaternion initialRotEyeL;
    Quaternion initialRotEyeR;
    float bend;
    float fallWiggleTimer = 0f;
    Quaternion previousRot = Quaternion.identity;

    float facePhase = 0;
    float lookPhase = 0;

    float sensitivityRate;
    float sensitivtyPhase;

    enum Curiosity
    {
        None,
        Look,
        Face,
        //Turn
    }

    Curiosity curiosity;
    float stateCooldown;// time to force current state
    float stateRecalculate;
    float idleTime = 0f;

    int speedId = Animator.StringToHash("speed");
    int turnId = Animator.StringToHash("turn");
    int offsetId = Animator.StringToHash("offset");
    int fallId = Animator.StringToHash("falling");
    int vocalizingId = Animator.StringToHash("vocalizing");
    int idleAnimationId = Animator.StringToHash("idleAnimation");
    int idleTimeId = Animator.StringToHash("idleTime");
    int randomValueId = Animator.StringToHash("randomValue");



    [System.Serializable]
    public struct AnimatedTransform
    {
        public bool useOriginalRotation;
        public Transform transform;
        [HideInInspector]
        public Quaternion originalRotation;

        public Quaternion Rotation
        {
            get
            {
                if (useOriginalRotation)
                {
                    return originalRotation;
                }
                return transform.localRotation;
            }
        }

        public void Setup()
        {
            if (useOriginalRotation)
            {
                originalRotation = transform.localRotation;
            }
        }

        public void Update()
        {
            if (useOriginalRotation)
            {
                transform.localRotation = originalRotation;
            }
        }
    }

    private void Awake()
    {
        sheep = GetComponentInParent<Sheep>();
        sheepAudio = GetComponentInChildren<SheepAudio>();
        //localRot = head.localRotation;
        for (int i = 0; i < spineTransforms.Length; i++)
            spineTransforms[i].Setup();
        for (int i = 0; i < neckTransforms.Length; i++)
            neckTransforms[i].Setup();
        headTransform = neckTransforms[neckTransforms.Length - 1];
        initialHeadWorldRot = headTransform.transform.rotation;
        initialRotEyeL = eyeL.localRotation;
        initialRotEyeR = eyeR.localRotation;
        sensitivtyPhase = Random.value;
        sensitivityRate = Random.value > 0 ?
            1f / Random.Range(3, 10) : -1f / Random.Range(3, 10);
        animator.SetFloat(offsetId, Random.value);
        sheepAudio.onSheepVocalize += OnVocalize;
    }

    private void OnDestroy()
    {
        sheepAudio.onSheepVocalize -= OnVocalize;
    }

    void OnVocalize()
    {
        animator.SetTrigger(vocalizingId);
    }

    private void Update()
    {
        var headPos = sheep.reHead.position;
        var tailPos = sheep.reTail.position;
        var dir = sheep.SheepLocomotion.CurrentFacing;
        var swing = Quaternion.FromToRotation(root.forward, dir);
        var rot = root.rotation;
        rot = swing * rot;

        
        var uprightSpeed = (1 - Mathf.Abs(dir.normalized.y)) * Time.deltaTime * 20; // when on "flat" ground recover quicker
        //var twist = Quaternion.FromToRotation(rot*Vector3.up, Vector3.up);

        var up = sheep.sheepNormal;
        Vector3.OrthoNormalize(ref dir, ref up);

        var twist = Quaternion.FromToRotation(rot * Vector3.up, Vector3.up);
        //twist = (Vector3.Project(twist.QuaternionToAngleAxis(), dir.normalized) *  uprightSpeed).AngleAxisToQuaternion();
        twist.ToAngleAxis(out var twistAngle, out var axis);
        twistAngle *= Vector3.Dot(axis, dir.normalized);
        twist = Quaternion.AngleAxis(twistAngle *uprightSpeed,dir.normalized);

        root.rotation = Quaternion.Slerp(previousRot, twist * rot, rotationChangeSpeed);
        previousRot = root.rotation;

        root.position = Vector3.Lerp(tailPos, headPos, .33f) - root.rotation *Vector3.up * Sheep.radius;
        //root.LookAt(headBody.position - Vector3.up * sheep.radius);

        if (sheep.state == Sheep.SheepState.Idle)
        {
            idleTime += Time.deltaTime;
        }
        else
        {
            idleTime = 0f;
        }

        animator.SetFloat(turnId, sheep.angularVelocity);
        animator.SetFloat(speedId, sheep.speed);
        animator.SetBool(fallId, !sheep.Grounded && !sheep.Jumping);
        animator.SetInteger(idleAnimationId, Random.Range(0, idleAnimationCount));
        animator.SetFloat(idleTimeId, idleTime);
        animator.SetFloat(randomValueId, Random.value);
    }

    void TransitionTo(Curiosity newcuriosity)
    {
        if (curiosity == newcuriosity) return;
        curiosity = newcuriosity;
        stateCooldown = Random.Range(1, 2);
        stateRecalculate = Random.Range(2, 5);
    }

    void TransitionToTurn()
    {
        if (sheep.state != Sheep.SheepState.TurnToPlayer && sheep.state != Sheep.SheepState.Scared)
        {
            sheep.ForceTurnToPlayer();
            TransitionTo(Curiosity.Face);
            
        }
    }

    private void LateUpdate()
    {
        var headPos = sheep.reHead.position;
        var tailPos = sheep.reTail.position;
        var dir = (headPos - tailPos).normalized;

        foreach(var bone in spineTransforms)
        {
            bone.Update();
        }
        foreach(var bone in neckTransforms)
        {
            bone.Update();
        }

        // bend the spine into the turn

        if (sheep.Grounded || sheep.Jumping)
        {
            bend = Mathf.MoveTowards(bend, Mathf.Clamp(sheep.angularVelocity * turnBendMultiplier, -1, 1) * turnBendAmount, (180) * Time.deltaTime);
            fallWiggleTimer = 0f;
        }
        else // falling!
        {
            var targetBend = Mathf.Sin(Mathf.PI * 2f * fallWiggleTimer * fallWiggleSpeed) * fallWiggleAmount;
            bend = Mathf.MoveTowards(bend, targetBend, 720 * Time.deltaTime);
            fallWiggleTimer += Time.deltaTime;
        }
        var twist = -bend/2; 
        var axis = root.rotation * Vector3.up;
        spineTransforms[0].transform.rotation = Quaternion.AngleAxis(-bend, axis) * Quaternion.AngleAxis(twist, dir) * spineTransforms[0].transform.rotation;
        for (int i = 1; i < spineTransforms.Length; i++)
            spineTransforms[i].transform.rotation = Quaternion.AngleAxis(2*bend  / (spineTransforms.Length - 1), axis) *  spineTransforms[i].transform.rotation;

        // sense player (HMD)
        var camPos = Camera.main.transform.position;
        var camDir = camPos - headTransform.transform.position;

        var angle = Vector3.Angle(dir, camDir);
        var a = Mathf.InverseLerp(60, 180, angle);
        var rangeMult = 1 - a * a;
        var dist = camDir.magnitude;
        var sensor = Mathf.InverseLerp(5 * rangeMult, 2 * rangeMult, dist)* Mathf.Lerp(.5f, 1.5f, sensitivtyPhase);

        sensor -= sheep.threatLevel * .2f; // dumb down when scared
        //var debug = sensor;
        //GetComponentInChildren<SkinnedMeshRenderer>().material.color = new Color(1, 1 - debug, 1 - debug);


        // sensitivy randomization
        sensitivtyPhase += sensitivityRate * Time.deltaTime;
        if (sensitivityRate > 0 && sensitivtyPhase >= 1)
            sensitivityRate = -1f / Random.Range(3, 10);
        if (sensitivityRate < 0 && sensitivtyPhase <= 0)
            sensitivityRate = 1f / Random.Range(3, 10);

        if (sheep.state == Sheep.SheepState.TurnToPlayer && curiosity != Curiosity.Face)
            TransitionTo(Curiosity.Face);
        // calculate curiosity(awareness) state
        var rng = Random.value;
        if (stateCooldown > 0)
            stateCooldown -= Time.fixedDeltaTime;
        else
        {
            stateRecalculate -= Time.fixedDeltaTime;

            switch (curiosity)
            {
                case Curiosity.None:
                    if (sensor > .5f)
                        if (rng > .9f-angle/180*.2f) TransitionToTurn();
                        else if (rng > .7f || sensor > .75f || angle>90) TransitionTo(Curiosity.Face);
                        else TransitionTo(Curiosity.Look);
                    break;
                case Curiosity.Look:
                    if (sensor < .1f) TransitionTo(Curiosity.None);
                    else if (sensor > .75f || angle > 90)
                        if (rng > .9f - angle / 180 * .2f) TransitionToTurn();
                        else TransitionTo(Curiosity.Face);
                    else if (stateRecalculate < 0 && rng > .9f) 
                        TransitionTo(Curiosity.None);
                    else if (stateRecalculate < 0 && rng > .8f)
                        TransitionTo(Curiosity.Face);
                    //else if (stateRecalculate < 0 && rng > .7f)
                    //    TransitionTo(Curiosity.Turn);
                    break;
                case Curiosity.Face:
                    if (sensor < .1f) TransitionTo(Curiosity.None);
                    else if (sensor < .25f && angle < 90) TransitionTo(Curiosity.Look);
                    else if (stateRecalculate < 0 && rng > .9f)
                        TransitionTo(Curiosity.None);
                    else if (stateRecalculate < 0 && rng > .8f)
                        TransitionTo(Curiosity.Look);
                    else if (stateRecalculate < 0 && rng > .7f)
                        TransitionToTurn();
                    break;
                //case Curiosity.Turn:
                //    if (sensor > .5f || dist<1) // also quit if too close
                //        if (rng > .7f || sensor > .75f || angle > 90) TransitionTo(Curiosity.Face);
                //        else TransitionTo(Curiosity.Look);
                //    else if (stateRecalculate < 0 && rng > .9f)
                //        TransitionTo(Curiosity.None);
                //    else if (stateRecalculate < 0 && rng > .8f)
                //        TransitionTo(Curiosity.Look);
                //    else if (stateRecalculate < 0 && rng > .7f)
                //        TransitionTo(Curiosity.Face);
                //    //else
                //    //    TransitionTo(Curiosity.None);
                //    break;
                default:
                    break;
            }
            if (stateRecalculate <= 0 && curiosity!= Curiosity.None)
            {
                if (rng > .9f)
                    TransitionTo(Curiosity.None);
                else if (rng > .8f)
                    TransitionTo(Curiosity.Look);
                else if (rng > .7f)
                    TransitionTo(Curiosity.Face);
                //else if (rng > .6f)
                //    TransitionTo(Curiosity.Turn);

                stateRecalculate = Random.Range(2, 5);
            }
        }
        if(curiosity== Curiosity.Look && angle>90)
            TransitionTo(Curiosity.Face);
        if (curiosity == Curiosity.Face && angle > 150)
            TransitionToTurn();

        //if (curiosity == Curiosity.Turn)
        //    sheep.lookAt = camDir;// camPos;
        //else
        //    sheep.lookAt = Vector3.zero;



        if (curiosity>=Curiosity.Face)
            facePhase = Mathf.MoveTowards(facePhase, 1, Time.deltaTime / Mathf.Lerp(.3f, .6f, Mathf.InverseLerp(2, 5, camDir.magnitude)));
        else
            facePhase = Mathf.MoveTowards(facePhase, 0, Time.deltaTime / .6f);
        if (curiosity >= Curiosity.Look)
            lookPhase = Mathf.MoveTowards(lookPhase, 1, Time.deltaTime / Mathf.Lerp(.05f, .1f, Mathf.InverseLerp(2, 5, camDir.magnitude)));
        else
            lookPhase = Mathf.MoveTowards(lookPhase, 0, Time.deltaTime / .2f);
        var faceT = Mathf.SmoothStep(0, 1, facePhase);
        var lookT = Mathf.SmoothStep(0, 1, lookPhase);


        // move head
        var targetRot = Quaternion.LookRotation(camDir) * initialHeadWorldRot;
        var targetDelta = targetRot * Quaternion.Inverse(headTransform.transform.rotation);
        var targetDeltaAA = targetDelta.QToAngleAxis()*faceT;
        //var angleAxis = delta.QuaternionToAngleAxis();
        deltaAA = Vector3.MoveTowards(deltaAA, targetDeltaAA, 4*360 *Mathf.Deg2Rad * Time.deltaTime);
        var delta = deltaAA.AngleAxisToQuaternion();

        var stepDelta = Quaternion.Slerp(Quaternion.identity, delta, turnToPlayerAmount / neckTransforms.Length);
        for (int i = 0; i < neckTransforms.Length; i++)
            neckTransforms[i].transform.rotation = stepDelta * neckTransforms[i].transform.rotation;


        // move eyes
        //var eyeLcenter = eyeL.parent.position;
        //var eyeRcenter = eyeR.parent.position;
        //var eyeLpos = FindLineSphereIntersections(eyeLcenter, camPos- eyeLcenter, eyeL.position, eyeRadius);
        //var eyeRpos = FindLineSphereIntersections(eyeRcenter, camPos - eyeRcenter, eyeR.position, eyeRadius);
        //var eyeLrot = Quaternion.LookRotation(eyeLpos - eyeL.position) * Quaternion.Euler(90, 0, 0);
        //var eyeRrot = Quaternion.LookRotation(eyeRpos - eyeR.position) * Quaternion.Euler(90, 0, 0);
        //eyeL.localRotation = Quaternion.Slerp(initialRotEyeL, Quaternion.Inverse(eyeL.parent.rotation) * eyeLrot, lookT);
        //eyeR.localRotation = Quaternion.Slerp(initialRotEyeR, Quaternion.Inverse(eyeR.parent.rotation) * eyeRrot, lookT);

        //eyeR.rotation = Quaternion.LookRotation(eyeR.forward, Vector3.up);
        //eyeL.localRotation = eyeL.localRotation * Quaternion.AngleAxis(15, Vector3.up);
        eyeL.localRotation = initialRotEyeL * Quaternion.AngleAxis(GetEyeLookAngle(eyeL, initialRotEyeL, camPos, false), Vector3.up);
        eyeR.localRotation = initialRotEyeR * Quaternion.AngleAxis(GetEyeLookAngle(eyeR, initialRotEyeR, camPos, true), Vector3.up);
    }
    Vector3 deltaAA;


    float GetEyeLookAngle(Transform eye, Quaternion initialEyeRot, Vector3 camPos, bool invertLimits)
    {
        var baseWorldRot = eye.parent.rotation * initialEyeRot;
        var right = baseWorldRot * Vector3.right;
        var up = baseWorldRot * Vector3.up;
        var toCamera = (camPos - eye.position).normalized;
        var projectedOnPlane = Vector3.ProjectOnPlane(toCamera, up);
        var minAngle = invertLimits ? -maxOuterEyeAngle : -maxInnerEyeAngle;
        var maxAngle = invertLimits ? maxInnerEyeAngle : maxOuterEyeAngle;
        return Mathf.Clamp(Vector3.SignedAngle(-right, projectedOnPlane, up), minAngle, maxAngle);
    }

    public static Vector3 FindLineSphereIntersections(Vector3 rayStart, Vector3 dir, Vector3 circleCenter, float circleRadius)
    {
        // http://www.codeproject.com/Articles/19799/Simple-Ray-Tracing-in-C-Part-II-Triangles-Intersec

        //var cx = circleCenter.X;
        //var cy = circleCenter.Y;
        //var cz = circleCenter.Z;

        rayStart -= circleCenter;
        var px = rayStart.x;
        var py = rayStart.y;
        var pz = rayStart.z;

        var vx = dir.x;
        var vy = dir.y;
        var vz = dir.z;

        var A = vx * vx + vy * vy + vz * vz;
        var B = 2 * (px * vx + py * vy + pz * vz );
        var C = px * px + py * py + pz * pz - circleRadius * circleRadius;

        // discriminant
        var D = B * B - 4 * A * C;

        if (D < 0) return Vector3.zero;
        var sqrtD = Mathf.Sqrt(D);

        var t1 = (-B - sqrtD) / (2 * A);
        var t2 = (-B + sqrtD) / (2 * A);
        var t = t1;
        if (t1 <= 0 && t2>t1)
            t = t2;
        return rayStart + dir * t + circleCenter;
    }

    private void OnValidate()
    {
        if (!animator && root)
        {
            animator = root.GetComponent<Animator>();
        }
    }
}
