using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrainRotatablePlatform : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
{
    [SerializeField]
    private SemaphoreManager semaphoreManager;
    [SerializeField]
    private int rotationIncrements = 90;
    [SerializeField]
    private float rotationDuration = 1;
    [SerializeField]
    AdaptiveAudioSource rotationSound;
    [SerializeField]
    List<GameObject> blockableActivatorObjects;
    [HideInInspector]
    [SerializeField]
    new private Rigidbody rigidbody;
    private ReBody reBody;

    List<IBlockableInteractable> blockableInteractables = new List<IBlockableInteractable>();

    //Train part has to have both ends on the platform in order for the platform to rotate
    //Also train has to be stationary
    public bool AllowRotation
    {
        get
        {
            if (rotating)
                return false;

            foreach (var item in activeTrainParts)
            {
                if (!item.Value.BothConnected || item.Key.IsMoving || !item.Key.CanMove)
                    return false;
            }
            return true;
        }
    }

    public bool Enabled => true;

    private bool rotating;

    private Dictionary<TrainBase, AttachedRigidbody> activeTrainParts = new Dictionary<TrainBase, AttachedRigidbody>();
    private List<RigidbodyRotationAroundPivot> rigidbodyRotationAroundPivots = new List<RigidbodyRotationAroundPivot>();

    private int rotationDirection;
    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();

        if (rotationSound.audioSource == null)
            rotationSound.audioSource = GetComponent<AudioSource>();
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
        OnFixedUpdateSystem.Register(this);
    }

    public void OnAfterLevelLoaded()
    {

    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        OnFixedUpdateSystem.Unregister(this);
    }


    private void Start()
    {
        foreach (var item in blockableActivatorObjects)
        {
            var blockable = item.GetComponentInChildren<IBlockableInteractable>();
            if (blockable != null)
                blockableInteractables.Add(blockable);

        }

        reBody = new ReBody(rigidbody);
    }

    public void AddConnection(TrainBase trainBase, Transform detector)
    {
        if (!activeTrainParts.ContainsKey(trainBase))
        {
            activeTrainParts.Add(trainBase, new AttachedRigidbody());
        }

        activeTrainParts[trainBase].AddConnection(detector);
    }

    public void RemoveConnection(TrainBase trainBase, Transform detector)
    {
        if (activeTrainParts.ContainsKey(trainBase))
        {
            var attachedRigidbody = activeTrainParts[trainBase];
            attachedRigidbody.RemoveConnection(detector);

            if (attachedRigidbody.ConnectionsCount == 0)
                activeTrainParts.Remove(trainBase);
        }
    }

    [NodeAPI("RotatePlatform")]
    public void Rotate(int rotationDirection)
    {
        if (AllowRotation)
        {
            this.rotationDirection = rotationDirection;
            rotating = true;
            LockAttachedRigidbodies();

            semaphoreManager.Lower(LowerBariersCallback);
        }
    }

    void IOnFixedUpdate.OnFixedUpdate()
    {
        bool shouldBlock = false;
        if (rotating || !AllowRotation)
            shouldBlock = true;

        foreach (var item in blockableInteractables)
        {
            if (item != null)
                item.ActivatorBlocked = shouldBlock;
        }
    }

    private IEnumerator Rotation()
    {
        rotationSound.PlaySound();
        Quaternion rotationChange = Quaternion.AngleAxis(rotationIncrements, transform.up * rotationDirection);
        Quaternion goal = reBody.rotation * rotationChange;
        Quaternion start = reBody.rotation;

        SetupAttachedRigidbodies(rotationChange);

        //rotate platform and attached rigidbodies
        for (float t = 0; t < rotationDuration; t += Time.fixedDeltaTime)
        {
            float lerp = t / rotationDuration;

            Quaternion rotation = Quaternion.Lerp(start, goal, lerp);
            reBody.rotation = rotation;
            RotateAttachedRigidbodies(lerp);

            yield return new WaitForFixedUpdate();
        }

        reBody.rotation = goal;
        RotateAttachedRigidbodies(1);

        semaphoreManager.Raise(UnlockAttachedRigidbodies);

        rotating = false;
        rotationSound.StopSound();
    }

    void SetupAttachedRigidbodies(Quaternion rotationChange)
    {
        rigidbodyRotationAroundPivots.Clear();

        foreach (var item in activeTrainParts)
        {
            InterceptGrab(item.Key.Rigidbody);
            rigidbodyRotationAroundPivots.Add(new RigidbodyRotationAroundPivot(item.Key.Anchor, rotationChange, transform.position));
        }
    }

    void RotateAttachedRigidbodies(float lerp)
    {
        foreach (var item in rigidbodyRotationAroundPivots)
        {
            item.ChangePositionAndRotation(lerp);
        }
    }

    private void InterceptGrab(Rigidbody toDetach)
    {
        var hand = Player.Instance.GetHandThatIsGrabbingBody(toDetach);

        if (hand != null)
            hand.InterceptGrab(null, Vector3.zero);
    }

    private void LowerBariersCallback(bool successful)
    {
        if (successful)
            StartCoroutine(Rotation());
        else
        {
            semaphoreManager.Raise((successful) =>
            {
                rotating = false;
                UnlockAttachedRigidbodies(successful);
            });
        }
    }

    private void LockAttachedRigidbodies()
    {
        foreach (var item in activeTrainParts)
        {
            item.Key.LockMovement();
        }
    }

    private void UnlockAttachedRigidbodies(bool successful)
    {
        foreach (var item in activeTrainParts)
        {
            item.Key.UnlockMovement();
        }
    }

}

internal class AttachedRigidbody
{
    private HashSet<Transform> connections = new HashSet<Transform>();

    internal bool BothConnected => connections.Count == 2;
    internal int ConnectionsCount => connections.Count;

    internal void AddConnection(Transform transform)
    {
        connections.Add(transform);
    }

    internal void RemoveConnection(Transform transform)
    {
        connections.Remove(transform);
    }
}

internal struct RigidbodyRotationAroundPivot
{
    internal Rigidbody rigidbody;
    internal ReBody reBody;

    internal Quaternion startRotation;
    internal Quaternion goalRotation;

    internal Vector3 startPosition;
    internal Vector3 goalPosition;

    internal RigidbodyRotationAroundPivot(Rigidbody rigidbody, Quaternion rotationChange, Vector3 pivotPos)
    {
        this.rigidbody = rigidbody;
        reBody = new ReBody(rigidbody);
        startRotation = reBody.rotation;
        goalRotation = reBody.rotation * rotationChange;

        startPosition = reBody.position;
        goalPosition = rotationChange * (reBody.position - pivotPos) + pivotPos;
    }

    internal void ChangePositionAndRotation(float lerp)
    {
        Quaternion newRotation = Quaternion.Lerp(startRotation, goalRotation, lerp);
        Vector3 newPosition = Vector3.Lerp(startPosition, goalPosition, lerp);

        reBody.rotation = newRotation;
        reBody.position = newPosition;
    }
}
