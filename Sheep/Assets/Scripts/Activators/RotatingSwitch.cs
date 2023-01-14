using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System;
using UnityEngine;

public class RotatingSwitch : MonoBehaviour, IProjectHandAnchor, IOverrideGrabAnchor, IManagedBehaviour, IBlockableInteractable
{
    [NodeAPI("OnRotate")]
    public event Action<int> onRotate;

    [SerializeField]
    [Range(0, 1)]
    private float snapzone = 0.5f;
    [SerializeField]
    private int steps = 4;
    [SerializeField]
    private bool invertValue = false;

    [HideInInspector]
    [SerializeField]
    private HingeJoint joint;
    [HideInInspector]
    [SerializeField]
    new private Rigidbody rigidbody;
    private ReBody reBody;

    private Quaternion originalRot;
    private float currentSnapAngle;
    private int currentSnapStep;
    private float anglePerStep;

    private Vector3 Axis => joint.axis;

    //animation
    [SerializeField]
    private float animationDuration = 0.1f;
    private float animationTimeSpent;
    private bool animationRun;
    private Quaternion knobStartRotation;
    private Quaternion knobTargetRotation;

    public bool ActivatorBlocked { get; set; }
    public Action OnTryingToMoveBlockedActivator { get; set; }

    private void OnValidate()
    {
        if (joint == null)
            joint = GetComponent<HingeJoint>();

        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
    }
    void IManagedBehaviour.OnAfterLevelLoaded()
    {
        originalRot = rigidbody.transform.localRotation;

        anglePerStep = 360f / steps;
        currentSnapAngle = 0;

        knobTargetRotation = GetGoalRotation();
    }

    public void FixedUpdate()
    {
        if (animationRun)
        {
            if (animationTimeSpent < animationDuration)
            {
                reBody.rotation = Quaternion.Lerp(knobStartRotation, knobTargetRotation, animationTimeSpent / animationDuration);
            }
            else
            {
                reBody.rotation = knobTargetRotation;
                animationRun = false;
            }
            animationTimeSpent += Time.deltaTime;
        }
        else
        {
            reBody.rotation = knobTargetRotation;
        }
    }

    private void Snap(float currentAngle)
    {
        var snap = ShouldSnapRotation(snapzone, anglePerStep, currentAngle);
        int finalSnapStep = currentSnapStep;

        if (snap.shouldSnap)
        {
            if (snap.newSnapStep != currentSnapStep)
            {
                if (!ActivatorBlocked)
                {
                    finalSnapStep = snap.newSnapStep;

                    currentSnapAngle = finalSnapStep * anglePerStep;

                    knobStartRotation = reBody.rotation;
                    knobTargetRotation = GetGoalRotation();
                    animationTimeSpent = 0;
                    animationRun = true;
                }
                else
                {
                    OnTryingToMoveBlockedActivator?.Invoke();
                }
            }
        }
        var value = invertValue ? finalSnapStep - currentSnapStep : currentSnapStep - finalSnapStep;
        var clampedValue = Mathf.Clamp(value, -1, 1);
        if (clampedValue != 0)
            onRotate?.Invoke(clampedValue);

        currentSnapStep = finalSnapStep;
    }

    private Quaternion GetGoalRotation()
    {
        return (rigidbody.transform.parent != null ? rigidbody.transform.parent.rotation : Quaternion.identity) *
            (originalRot * Quaternion.AngleAxis(currentSnapAngle, Axis));
    }

    public static (bool shouldSnap, int newSnapStep) ShouldSnapRotation(float snapzone, float degreesPerStep, float innerAngle)
    {
        var convertedSnapzone = (degreesPerStep / 2) * snapzone;

        int currStep = GetCurrentStep(innerAngle, degreesPerStep);

        if (CanSnapAtStep(currStep, degreesPerStep, innerAngle, convertedSnapzone))
            return (true, currStep);
        else
            return (false, 0);
    }

    private static bool CanSnapAtStep(int currStep, float step, float innerAngle, float deadzone)
    {
        float from = currStep * step;
        float min = from - deadzone;
        float max = from + deadzone;

        return innerAngle >= min && innerAngle <= max;
    }

    public static int GetCurrentStep(float innerAngle, float step)
    {
        return Mathf.RoundToInt(innerAngle / step);
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        var vrRotDelta = Quaternion.Inverse(knobTargetRotation) * vrRot;
        var projection = Vector3.Project(vrRotDelta.QToAngleAxis(), Axis).AngleAxisToQuaternion();
        var diff = re.GetTwistAngle(projection, Axis) * Mathf.Rad2Deg;

        //this can be commented out with minimal effect.
        vrRot = knobTargetRotation;
        vrAngular = Vector3.zero;
        vrVel = Vector3.zero;

        if (!animationRun)
            Snap((diff + currentSnapAngle) % 360);
    }

    public (Vector3 grabPosition, Quaternion grabRotation) Reanchor(Vector3 currentGrabPosition, Quaternion currentGrabRotation)
    {
        var worldAxis = reBody.rotation * Axis;
        var finalRotation = RotationHelper.RotateToAxisWithoutTwisting(currentGrabRotation, worldAxis);
        return (reBody.position, finalRotation);
    }

    #region Gizmos

    private const float gizmosArcRadius = 1;

    private void OnDrawGizmos()
    {
        if (joint == null)
        {
            return;
        }

        var worldAxis = transform.TransformDirection(Axis);

        anglePerStep = 360f / steps;

        var snapZone = (anglePerStep / 2) * snapzone;
        var deadzone = anglePerStep - snapZone * 2;

        for (int i = 0; i < steps; i++)
        {
            float from = i * anglePerStep;

            DebugUtils.DrawArc(
                transform.position,
                worldAxis,
                Quaternion.AngleAxis(from, worldAxis) * transform.right,
                snapZone,
                gizmosArcRadius,
                Color.green);

            DebugUtils.DrawArc(
                transform.position,
                worldAxis,
                Quaternion.AngleAxis(from + snapZone, worldAxis) * transform.right,
                deadzone,
                gizmosArcRadius,
                Color.red);

            DebugUtils.DrawArc(
                transform.position,
                worldAxis,
                Quaternion.AngleAxis(from + deadzone + snapZone, worldAxis) * transform.right,
                snapZone,
                gizmosArcRadius,
                Color.green);
        }
    }

    #endregion
}
