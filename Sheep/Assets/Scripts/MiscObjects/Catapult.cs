using Recoil;
using UnityEngine;

public class Catapult : MonoBehaviour, IGrabNotifications, IProjectHandAnchor
{
    [SerializeField]
    HingeJoint catapultArmJoint;

    [SerializeField]
    float hingeForce = 10000;
    [SerializeField]
    float targetVelocity = 1000;

    [SerializeField]
    int lockCount = 7;

    [SerializeField]
    [Range(0f, 1f)]
    float catapultLaunchActivation = 0.5f;

    [SerializeField]
    [Range(0, 320)]
    int vibrationFrequency = 10;
    [SerializeField]
    [Range(0f, 1f)]
    float vibrationAmplitude = 0.5f;

    [SerializeField]
    ObjectActivator launchActivator;

    AudioSource clickSound;
    Rigidbody catapultArmBody;
    ReBody catapultReBody;
    int currentLock = 0;
    float anglePerLock;
    bool launching = false;
    Hand activeHand = null;
    float timeSinceLaunch = 0f;
    const float freeLaunchDuration = 0.5f;

    void Start()
    {
        clickSound = GetComponent<AudioSource>();
        catapultArmBody = catapultArmJoint.GetComponent<Rigidbody>();
        catapultReBody = new ReBody(catapultArmBody);
        anglePerLock = catapultArmJoint.limits.max / lockCount;
        UpdateArmTension(1f);
    }

    void UpdateArmTension(float amount)
    {
        var motor = catapultArmJoint.motor;
        motor.force = hingeForce * amount;
        motor.targetVelocity = targetVelocity;
        catapultArmJoint.motor = motor;
    }

    void UpdateLaunching()
    {
        var launchingThisFrame = launchActivator.ActivationAmount > catapultLaunchActivation;
        if (launchingThisFrame)
        {
            timeSinceLaunch = 0f;
        }
        else
        {
            if (timeSinceLaunch < freeLaunchDuration)
            {
                timeSinceLaunch += Time.fixedDeltaTime;
            }
        }

        launching = timeSinceLaunch < freeLaunchDuration;
    }

    private void FixedUpdate()
    {
        UpdateLaunching();

        //var currentAngle = (Quaternion.Inverse(catapultArmJoint.connectedBody.rotation) * catapultArmBody.rotation).eulerAngles.x;
        var currentAngle = catapultArmJoint.angle;

        var target = (lockCount - currentLock - 1) * anglePerLock;

        if (Mathf.DeltaAngle(target, currentAngle) < -1f && currentLock < lockCount && !launching)
        {
            currentLock++;
            var limits = catapultArmJoint.limits;
            limits.max = (lockCount - currentLock) * anglePerLock;
            catapultArmJoint.limits = limits;
            DoClick();
        }

        if (launching)
        {
            var limits = catapultArmJoint.limits;
            limits.max = lockCount * anglePerLock;
            catapultArmJoint.limits = limits;
            currentLock = 0;
        }
    }

    void DoClick()
    {
        clickSound.pitch = 0.5f + 0.15f * (currentLock / (float)lockCount);
        clickSound.PlayOneShot(clickSound.clip);
        if (activeHand != null)
        {
            activeHand.Vibrate(0f, Time.fixedDeltaTime, vibrationFrequency, vibrationAmplitude);
        }
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectAnchorUtils.ProjectHingeJoint(catapultReBody, catapultArmJoint, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
        vrVel = Vector3.zero;
        vrAngular = Vector3.zero;
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            UpdateArmTension(0f);
            activeHand = hand;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            UpdateArmTension(1f);
            activeHand = null;
        }
        else
        {
            activeHand = hand.otherHand;
        }
    }
}
