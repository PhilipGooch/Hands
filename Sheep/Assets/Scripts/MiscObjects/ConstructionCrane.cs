using NBG.LogicGraph;
using NBG.XPBDRope;
using Recoil;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionCrane : MonoBehaviour
{
    [Header("Rope Holder")]
    [SerializeField]
    private ConfigurableJoint ropeHolderJoint;
    [SerializeField]
    private Transform ropeHolderAnchorTarget;
    [Range(0, 1)]
    [SerializeField]
    private float ropeHolderSpeed = 0.05f;
    [SerializeField]
    float ropeHolderMaxAcceleration = 5;
    [SerializeField]
    float ropeHolderMaxVelocity = 2;
    [HideInInspector]
    [SerializeField]
    Rigidbody ropeHolderRigidbody;
    ReBody ropeHolderReBody;

    private Vector3 ropeHolderOperationRange;
    private Vector3 ropeHolderStartPos;

    [Header("Magnet Rope")]
    [Range(0, 1)]
    [SerializeField]
    private float startingRopeLength;
    [Range(0, 1)]
    [SerializeField]
    private float maxMagnetVerticalSpeed = 0.01f;
    [SerializeField]
    private Rope[] targetRopes;
    [SerializeField]
    private AnimationCurve accelarationCurve;
    [SerializeField]
    private float accelerationDuration = 2f;
    private float accelerationTimer;

    [Header("Crane Top")]
    [SerializeField]
    HingeJoint turnJoint;
    [SerializeField]
    float hingeVel;

    [Header("Magnet")]
    [SerializeField]
    private Magnet magnet;

    [SerializeField]
    private List<MaterialSwapper> powerIndicators;

    [Header("Magnet On/Off Button")]
    [SerializeField]
    private MeshRenderer magnetToggleButtonRenderer;
    [SerializeField]
    private Material buttonOnMaterial;
    [SerializeField]
    private Material buttonOffMaterial;
    [SerializeField]
    private Material buttonNoPowerMaterial;

    [Header("Haptics")]
    [SerializeField]
    private HapticsBase ropeHaptics;
    [SerializeField]
    private HapticsBase craneTopHaptics;
    [SerializeField]
    private HapticsBase ropeHolderHaptics;
    [SerializeField]
    private ConstantHaptics ropeLimitReachedHaptics;
    [SerializeField]
    private ConstantHaptics ropeHolderLimitReachedHaptics;

    [Header("Audio")]
    [SerializeField]
    private AudioSource rotationHum;
    [SerializeField]
    private AudioSource magnetHum;
    [SerializeField]
    private AudioSource liftHum;
    [SerializeField]
    private AudioSource powertHum;
    [SerializeField]
    private AudioSource moveHum;
    [Range(0, 1)]
    [SerializeField]
    private float lowestPitchValue = 0.5f;
    [Range(0, 10)]
    [SerializeField]
    private float reachedLimitPitchMulti = 3;

    [NodeAPI("OnHookMoved")]
    public event Action<float> OnHookMoved;

    [NodeAPI("PowerState")]
    public bool PowerState { get; set; }

    private const float deadZone = 0.1f;

    private void OnValidate()
    {
        if (ropeHolderRigidbody == null && ropeHolderJoint != null)
            ropeHolderRigidbody = ropeHolderJoint.GetComponent<Rigidbody>();
    }
    private void Start()
    {
        ropeHolderReBody = new ReBody(ropeHolderRigidbody);

        foreach (var rope in targetRopes)
        {
            rope.RopeLengthMultiplier = startingRopeLength;
        }

        CalculateRopeHolderLimits();
    }

    private void FixedUpdate()
    {
        UpdatePowerIndicators();
    }

    [NodeAPI("UpdateCraneTurn")]
    public void UpdateCraneTurn(float activation)
    {
        var updatedValue = GetUpdatedValue(activation);
        craneTopHaptics.active = updatedValue != 0;

        var motor = turnJoint.motor;
        motor.targetVelocity = hingeVel * updatedValue;
        turnJoint.motor = motor;

        UpdateElectricSounds(rotationHum, updatedValue, false);
    }

    //0-1
    float GetRopeHolderPosition()
    {
        var locaPos = ropeHolderAnchorTarget.transform.InverseTransformPoint(ropeHolderJoint.transform.position);
        return ((Vector3.Dot(ropeHolderOperationRange.normalized, locaPos - ropeHolderStartPos) / ropeHolderJoint.linearLimit.limit) + 1f) / 2;
    }

    [NodeAPI("UpdateRopeHolderLinearMovement")]
    public void UpdateRopeHolderLinearMovement(float activation)
    {
        var updatedValue = GetUpdatedValue(activation);

        ropeHolderHaptics.active = updatedValue != 0;

        var currentPosition = GetRopeHolderPosition();
        var targetPos = Mathf.Lerp(0, 1, Mathf.Clamp01(currentPosition + ropeHolderSpeed * updatedValue));
        Servo.ApplyLinearDynamics(ropeHolderReBody, ropeHolderJoint, targetPos, currentPosition, ropeHolderMaxAcceleration, ropeHolderMaxVelocity);

        bool isNearLimit = IsRopeHolderNearLimit(updatedValue, currentPosition);

        ropeHolderLimitReachedHaptics.active = isNearLimit;
        if (isNearLimit)
            ropeHolderLimitReachedHaptics.HapticsIntervalMulti = GetLimitReachedVibrationMulti(updatedValue);

        UpdateElectricSounds(moveHum, updatedValue, isNearLimit);

        OnHookMoved?.Invoke(GetRopeHolderPosition());
    }

    private float GetLimitReachedVibrationMulti(float updatedValue)
    {
        return Mathf.Clamp(1 - Mathf.Abs(updatedValue), 0.3f, 1);
    }

    [NodeAPI("UpdateMagnetRopeLength")]
    public void UpdateMagnetRopeLength(float activation)
    {
        var updatedValue = GetUpdatedValue(activation);

        ropeHaptics.active = updatedValue != 0;

        if (updatedValue == 0)
            accelerationTimer = 0;
        else
            accelerationTimer = Mathf.Clamp(accelerationTimer += Time.fixedDeltaTime, 0, accelerationDuration);

        bool magnetRopeNearLimit = false;

        foreach (var rope in targetRopes)
        {
            if (updatedValue != 0)
            {
                rope.RopeLengthMultiplier += maxMagnetVerticalSpeed * updatedValue * accelarationCurve.Evaluate(accelerationTimer / accelerationDuration);
                magnetRopeNearLimit = rope.RopeLengthMultiplier == 0 || rope.RopeLengthMultiplier == 1;
            }
        }

        ropeLimitReachedHaptics.active = magnetRopeNearLimit;
        if (magnetRopeNearLimit)
        {
            ropeLimitReachedHaptics.HapticsIntervalMulti = GetLimitReachedVibrationMulti(updatedValue);
        }

        UpdateElectricSounds(liftHum, updatedValue, magnetRopeNearLimit);
    }


    private void CalculateRopeHolderLimits()
    {
        ropeHolderStartPos = ropeHolderAnchorTarget.transform.InverseTransformPoint(ropeHolderJoint.transform.position);
        ropeHolderOperationRange = ropeHolderJoint.axis * ropeHolderJoint.linearLimit.limit;
    }

    private bool IsRopeHolderNearLimit(float updatedValue, float localPos)
    {
        //if has reached one of the ends and is moving in that direction, then play sound
        return (localPos < 0.01f && updatedValue < 0)
          || (localPos > 0.99f && updatedValue > 0);
    }

    private void UpdateElectricSounds(AudioSource source, float value, bool isNearEnd)
    {
        var absValue = Mathf.Abs(value);
        source.pitch = GetPitchValue(absValue, isNearEnd);
        source.volume = absValue;
    }

    private float GetPitchValue(float absValue, bool isNearEnd)
    {
        if (!isNearEnd)
            return Mathf.Clamp(absValue * 2, lowestPitchValue, 1);
        else
            return Mathf.Clamp(absValue * reachedLimitPitchMulti, lowestPitchValue, 10);
    }

    private void UpdatePowerIndicators()
    {
        foreach (var indicator in powerIndicators)
        {
            foreach (var renderer in indicator.targetRenderers)
            {
                renderer.material = PowerState ? indicator.onMaterial : indicator.offMaterial;
            }
        }

        powertHum.volume = PowerState ? 1 : 0;
    }

    [NodeAPI("UpdateMagnet")]
    public void UpdateMagnet(bool magnetActive)
    {
        magnet.MagnetActive = magnetActive;
        if (magnetActive && PowerState)
        {
            magnetToggleButtonRenderer.material = buttonOnMaterial;
            magnetHum.volume = 1;
        }
        else
        {
            magnetToggleButtonRenderer.material = PowerState ? buttonOffMaterial : buttonNoPowerMaterial;
            magnetHum.volume = 0;
        }
    }

    private float GetUpdatedValue(float input)
    {
        var value = (input * 2) - 1;
        value = value.Between(-deadZone, deadZone) ? 0 : value;
        return value * (PowerState ? 1 : 0);
    }


    [System.Serializable]
    public struct MaterialSwapper
    {
        public List<Renderer> targetRenderers;
        public Material onMaterial;
        public Material offMaterial;
    }
}
