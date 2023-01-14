using Recoil;
using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Semaphore : MonoBehaviour, IBlockableInteractable
{
    private enum BarierState
    {
        Static,
        Lowering,
        Raising,
        Paused
    }

    [Header("Rotation")]
    [SerializeField]
    private Rigidbody barrier;
    private ReBody reBarrier;
    [SerializeField]
    private ConfigurableJoint joint;

    [Range(-180, 45)]
    [SerializeField]
    private float raisedAngle = -90;

    [Range(-180, 45)]
    [SerializeField]
    private float loweredAngle = 0;
    [SerializeField]
    Vector3 rotationAxis;

    [Header("Light")]
    [SerializeField]
    Material roadBlockedLightMaterial;
    [SerializeField]
    Material roadFreeLightMaterial;
    [SerializeField]
    MeshRenderer lightRenderer;

    [SerializeField]
    AdaptiveAudioSource movementSound;

    private Quaternion raisedRotation;
    private Quaternion loweredRotation;
    private Quaternion start;
    private Quaternion goal;

    private CollisionEventsSender collisionEventsSender;

    private Action<bool> onFinish;

    private bool interupted;

    private float duration;
    private float timeSpent;

    private BarierState state;

    bool activatorBlocked;
    public bool ActivatorBlocked
    {
        get
        {
            return activatorBlocked;
        }
        set
        {

            activatorBlocked = value;
            if (activatorBlocked)
                SetLightsToBlockedPath();
            else
                SetLightsToFreePath();
        }
    }
    public Action OnTryingToMoveBlockedActivator { get; set; }

    private void OnValidate()
    {
        if (movementSound.audioSource == null)
            movementSound.audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        reBarrier = new ReBody(barrier);
        collisionEventsSender = GetComponentInChildren<CollisionEventsSender>();
        collisionEventsSender.onCollisionEnter += CollisionEnter;

        raisedRotation = transform.rotation * Quaternion.Euler(rotationAxis * raisedAngle);
        loweredRotation = transform.rotation * Quaternion.Euler(rotationAxis * loweredAngle);

        SetState(BarierState.Static);
    }

    public void Lower(float duration, Action<bool> onLower)
    {
        onFinish = onLower;
        this.duration = duration;

        SetState(BarierState.Lowering);
    }

    public void Raise(float duration, Action<bool> onRaise)
    {
        onFinish = onRaise;
        this.duration = duration;

        SetState(BarierState.Raising);
    }

    private void FixedUpdate()
    {
        if (state == BarierState.Raising || state == BarierState.Lowering)
        {
            if (state == BarierState.Lowering && interupted)
            {
                SetState(BarierState.Paused);
                onFinish?.Invoke(true);
            }
            else
            {
                if (timeSpent < duration)
                {
                    reBarrier.rotation = Quaternion.Lerp(start, goal, Mathf.SmoothStep(0, 1, timeSpent / duration));
                }
                else
                {
                    reBarrier.rotation = goal;
                    if (state == BarierState.Raising)
                        SetState(BarierState.Static);
                    else
                        SetState(BarierState.Paused);

                    onFinish?.Invoke(interupted);
                }
                timeSpent += Time.fixedDeltaTime;
            }
        }
    }
    private void SetState(BarierState toSet)
    {
        switch (toSet)
        {
            case BarierState.Static:
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                SetLightsToFreePath();
                movementSound.StopSound();
                break;
            case BarierState.Lowering:
                interupted = false;
                timeSpent = 0;
                start = reBarrier.rotation;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                goal = loweredRotation;
                SetLightsToBlockedPath();
                movementSound.PlaySound();
                break;
            case BarierState.Raising:
                timeSpent = 0;
                start = reBarrier.rotation;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                goal = raisedRotation;
                movementSound.PlaySound();
                break;
            case BarierState.Paused:
                movementSound.StopSound();
                break;
        }
        state = toSet;
    }

    void SetLightsToFreePath()
    {
        lightRenderer.material = roadFreeLightMaterial;
    }

    void SetLightsToBlockedPath()
    {
        lightRenderer.material = roadBlockedLightMaterial;
    }

    private void CollisionEnter(Collision collision)
    {
        interupted = true;
    }
}
