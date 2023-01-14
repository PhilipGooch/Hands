using UnityEngine;

public class HydraulicJack : ObjectActivator
{
    [SerializeField]
    private HingeJoint pipeJoint;
    [SerializeField]
    Rigidbody handleSocket;

    [Tooltip("How many degrees of rotation are counted as a step, the lower the smoother, higher numbers make it feel like a cog")]
    [SerializeField]
    private float stepThresh = 0.1f;
    [SerializeField]
    private int stepsToFullyExtend = 3000;
    [Tooltip("Jack socket joint damper when handle is present in the socket")]
    [SerializeField]
    private int engagedDamper = 20000;
    [SerializeField]
    Vector3 socketDirection = Vector3.up;

    ConfigurableJoint joint;
    private TriggerEventSender triggerEventSender;

    float lastAngle;

    private float step = 0.01f;
    private float startDamper;
    private float accumulatedDelta = 0;

    private void Start()
    {
        triggerEventSender = GetComponentInChildren<TriggerEventSender>();

        triggerEventSender.onTriggerEnter += OnTriggerEnter;
        triggerEventSender.onTriggerExit += OnTriggerExit;

        lastAngle = pipeJoint.angle;
        startDamper = pipeJoint.spring.damper;

        step = 1f / stepsToFullyExtend;
    }

    private void OnTriggerEnter(Collider obj)
    {
        if (obj.GetComponentInChildren<HydraulicJackLever>() != null)
        {
            ConnectHandle(obj.gameObject);

            var spring = pipeJoint.spring;
            spring.damper = engagedDamper;
            pipeJoint.spring = spring;
        }
    }

    private void OnTriggerExit(Collider obj)
    {
        if (obj.GetComponentInChildren<HydraulicJackLever>() != null)
        {
            Destroy(joint);
            ActivationAmount = 0;

            var spring = pipeJoint.spring;
            spring.damper = startDamper;
            pipeJoint.spring = spring;
        }
    }

    void ConnectHandle(GameObject handle)
    {
        joint = handle.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handleSocket;
        joint.axis = socketDirection;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.enableCollision = true;
    }

    private void FixedUpdate()
    {
        if (joint == null)
            return;

        float currAngle = pipeJoint.angle;
        var dif = currAngle - lastAngle;

        accumulatedDelta = Mathf.Max(accumulatedDelta + dif, 0);

        while (accumulatedDelta >= stepThresh)
        {
            ActivationAmount += step;
            accumulatedDelta -= stepThresh;
        }

        lastAngle = currAngle;
    }

    private void OnDrawGizmos()
    {
        var direction = handleSocket.transform.TransformDirection(socketDirection) *-1;
        NBG.Core.DebugExtension.DrawArrow(handleSocket.transform.position - direction, direction, Color.cyan);
    }
}

