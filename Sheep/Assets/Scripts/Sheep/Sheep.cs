//#define FLOCKING
//#define PAIRSCARE
using System;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Recoil;
using NBG.Locomotion;

public partial class Sheep : MonoBehaviour
{
    [System.NonSerialized]
    public int id;

    public Rigidbody head;
    public ReBody reHead;
    public Rigidbody tail;
    public ReBody reTail;
    [SerializeField]
    BallLocomotion ballLoco;
    Vector3 headInitialPos;
    Vector3 tailInitialPos;

    public static float radius = .125f;
    public const float separationDist = .2f;
    public const float targetLen = .2f;

    // instance config
    public float bravery;
    public SmoothRandom braveryRnd = new SmoothRandom();
    public SmoothRandom focusRnd = new SmoothRandom();
    public SmoothRandom stretchRnd = new SmoothRandom();

    // motion
    public Vector3 relaxHead;
    public Vector3 relaxTail;
    public Vector3 scare;
    public float sheepSpeedModifier = 1f;

    // info
    public float speed => ballLoco.CurrentMovementSpeed;
    public float angularVelocity => ballLoco.CurrentTurnSpeed;
    public bool Grounded => ballLoco.OnGround;
    public bool Jumping => ballLoco.Jumping;
    public BallLocomotion SheepLocomotion => ballLoco;
    public float threatLevel;
    public Vector3 sheepNormal;
    public SheepCheckpoint checkpoint;

    public bool NeedsRespawn { get; set; } = false;
    bool isRespawning = false;
    Vector3 targetRespawnPosition = Vector3.zero;
    const float distanceForRespawnColliderEnabling = 2f;
    [SerializeField]
    [Tooltip("If optional, the sheep will not count towards the level goal. This should be used for optional achievement animals like a black sheep or some farm animals.")]
    bool optional = false;
    public bool Optional
    {
        get { return optional; }
        private set { optional = value; }
    }

    [Flags]
    public enum SheepScareTypes
    {
        Herding = 1 << 0,
        Poking = 1 << 1
    }

    public SheepScareTypes sheepScareTypes = SheepScareTypes.Herding;

    // behavior data
    public Sheep neighbor;
    public Vector3 wanderTarget;
    public float wanderSpeed;
    public Vector3 wallGradient;

    public static bool debug = false;
    public Transform debugSphere;
    public Transform debugSphere2;

    const int maxFramesToRecallThreateningContact = 5;
    int framesSinceThreatContact = maxFramesToRecallThreateningContact;
    public bool WasRecentlyTouchedByThreateningObject { get { return framesSinceThreatContact < maxFramesToRecallThreateningContact; } }

    #region neighbors
    public List<Sheep> neighbors = new List<Sheep>();

    public void AddNeighbor(Sheep sheep)
    {
        neighbors.Add(sheep);
    }

    public void RemoveNeighbor(Sheep sheep)
    {
        neighbors.Remove(sheep);
    }
    #endregion

    #region State machine
    public enum SheepState
    {
        Scared,
        Idle,
        TurnToPlayer,
        Follow,
        Wander
    }

    public SheepState state = SheepState.Idle;
    float stateTimer = 0;
    void CalculateState()
    {
        // scare is priority state
        if (scare.magnitude > .01f)
        {
            state = SheepState.Scared;
            return;
        }

        stateTimer -= Time.fixedDeltaTime;
        var rnd = UnityEngine.Random.value;

        if (state == SheepState.Scared)
            stateTimer = 0;
        if (state == SheepState.TurnToPlayer && !FacePlayer())
            stateTimer = 0;
        if (state == SheepState.Follow && !SheepFollow.Process(this))
            stateTimer = 0;
        if (state == SheepState.Wander && !SheepWander.Process(this))
            stateTimer = 0;

        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer < 0) // transition
        {
            if (rnd > 1.9f) // disable wandering
            {
                if (SheepWander.Begin(this) && SheepWander.Process(this))
                {
                    state = SheepState.Wander;
                    stateTimer = UnityEngine.Random.Range(2, 10);
                    return;
                }
            }
            else 
            if (rnd > .85f)
            {
                if (BeginFacePlayer() && FacePlayer())
                {
                    // if within certain radius????
                    state = SheepState.TurnToPlayer;
                    stateTimer = UnityEngine.Random.Range(2, 10);
                    return;
                }
            }
            else if (rnd > .7f)
            {
                if (SheepFollow.Begin(this) && SheepFollow.Process(this))

                {
                    // if can find neighbor
                    state = SheepState.Follow;
                    stateTimer = UnityEngine.Random.Range(2, 10);
                    return;
                }
            }

            state = SheepState.Idle;
            stateTimer = UnityEngine.Random.Range(10, 30);
        }

    }

    void UpdateInternalState()
    {
        if (framesSinceThreatContact < maxFramesToRecallThreateningContact)
        {
            framesSinceThreatContact++;
        }

        if (isRespawning)
        {
            var distanceFromTarget = targetRespawnPosition - reHead.position;
            if (distanceFromTarget.magnitude < distanceForRespawnColliderEnabling)
            {
                isRespawning = false;
                head.GetComponent<Collider>().enabled = true;
                tail.GetComponent<Collider>().enabled = true;
            }
        }
    }

    public void ReportCollision(bool isHead,  Vector3 groundVelocity)
    {
    }

    bool BeginFacePlayer()
    {
        return true;
    }

    bool FacePlayer()
    {
        var camPos = Camera.main.transform.position;
        var camDir = camPos - reHead.position;
        RelaxLookAt(camDir, 10, 120);
        return true;
    }
    public void ForceTurnToPlayer()
    {
        state = SheepState.TurnToPlayer;
        stateTimer = UnityEngine.Random.Range(2, 10);
    }
    #endregion

    public static List<Sheep> all = new List<Sheep>();
    private void OnEnable()
    {
        all.Add(this);
    }
    private void OnDisable()
    {
        all.Remove(this);
    }
    private void Awake()
    {
        if (!debug)
            debugSphere = debugSphere2 = null;

        reHead = new ReBody(head);
        reTail = new ReBody(tail);
        //Physics.gravity = new Vector3(0, -33, 0);
        reHead.maxAngularVelocity = 50;
        reTail.maxAngularVelocity = 50;
        radius = head.GetComponent<SphereCollider>().radius * head.transform.lossyScale.x;
        headInitialPos = transform.InverseTransformPoint(head.transform.position);
        tailInitialPos = transform.InverseTransformPoint(tail.transform.position);

        bravery = UnityEngine.Random.Range(.8f, 1.2f);
    }

    private void Start()
    {
        reHead.AllowSleeping = false;
        reTail.AllowSleeping = false;
    }

    internal void Place(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        reHead.SetBodyPlacementImmediate(transform.TransformPoint(headInitialPos), rot);
        reTail.SetBodyPlacementImmediate(transform.TransformPoint(tailInitialPos), rot);
        reHead.velocity = Vector3.zero;
        reTail.velocity = Vector3.zero;
        ballLoco.SetRotation(rot);
    }

    internal void Respawn(Vector3 finalPosition, Vector3 positionInAir, Quaternion rotation)
    {
        GameParameters.Instance.sheepSpawnEffect.Create(positionInAir);
        Place(positionInAir, rotation);
        var diff = finalPosition - positionInAir;
        if (diff.sqrMagnitude > Mathf.Pow(distanceForRespawnColliderEnabling, 2))
        {
            isRespawning = true;
            targetRespawnPosition = finalPosition;
            tail.GetComponent<Collider>().enabled = false;
            head.GetComponent<Collider>().enabled = false;
        }
    }

    public static void Separate(IList<Sheep> all)
    {
        for (int i = 0; i < all.Count; i++)
            all[i].BeginStep();

        SheepSeparation.SeparateAll(all);

        for (int i = 0; i < all.Count; i++)
            all[i].SetRelaxedPositions();
    }


    public class ThreatLevelComparer : IComparer<Sheep>
    {
        public int Compare(Sheep x, Sheep y)
        {
            if (x.threatLevel < y.threatLevel) return 1;
            if (x.threatLevel > y.threatLevel) return -1;
            return 0;
            
        }
    }
    public static ThreatLevelComparer comparer = new ThreatLevelComparer();
    static List<Sheep> sortedSheep = new List<Sheep>();
    public static void Process(IList<Sheep> all)
    {

        for (int i = 0; i < all.Count; i++)
            all[i].BeginStep();

        {
            UnityEngine.Profiling.Profiler.BeginSample("Scare");
            SheepScareIterative.ScareAll(all);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        UnityEngine.Profiling.Profiler.BeginSample("Update Sheep State");
        // calculate state
        for (int i = 0; i < all.Count; i++)
        {
            all[i].UpdateInternalState();
            all[i].CalculateState();
        }
        UnityEngine.Profiling.Profiler.EndSample();

        sortedSheep.Clear();
        sortedSheep.AddRange(all);
        sortedSheep.Sort(comparer);


        for (int i = 0; i < sortedSheep.Count; i++)
        {
            var loco = sortedSheep[i].ballLoco;
            loco.SetInput(sortedSheep[i].scare * sortedSheep[i].sheepSpeedModifier, false);
        }
    }

    public void NotifyAboutTouchedThreateningObject()
    {
        framesSinceThreatContact = 0;
    }

    #region Relaxation and motion

    public void BeginStep()
    {

        relaxHead = reHead.position;
        relaxTail = reTail.position;
        relaxTail = SetDistance(relaxHead, relaxTail, targetLen);

        braveryRnd.Step(Time.fixedDeltaTime);
        focusRnd.Step(Time.fixedDeltaTime);
        stretchRnd.Step(Time.fixedDeltaTime);
    }

    public void RelaxMoveFull(Vector3 offset)
    {
        offset.AssertIsFinite();
        relaxHead += offset;
        relaxTail += offset;
        relaxTail.AssertIsFinite();
        relaxHead .AssertIsFinite();
    }

    public void RelaxMoveHead(Vector3 offset)
    {
        offset.AssertIsFinite();
        relaxHead += offset;
        relaxTail = SetDistance(relaxHead, relaxTail, targetLen);
        relaxTail.AssertIsFinite();
        relaxHead.AssertIsFinite();
    }
    public void RelaxMoveTail(Vector3 offset)
    {
        offset.AssertIsFinite();
        relaxTail += offset;
        relaxHead = SetDistance(relaxTail, relaxHead, targetLen);
        relaxTail.AssertIsFinite();
        relaxHead.AssertIsFinite();
    }

    private void RelaxLookAt(Vector3 lookAt, float spring, float maxAngularDeg)
    {
        lookAt = lookAt.ZeroY();
        if (lookAt.magnitude < .5f) return;

        var pivot = Vector3.Lerp(relaxHead, relaxTail, .5f);

        var dir = (relaxHead - relaxTail).ZeroY().normalized;

        var angle = Math3d.SignedVectorAngle(dir, lookAt, Vector3.up);

        var toRotate = Mathf.Clamp(angle * spring, -maxAngularDeg, maxAngularDeg) * Time.fixedDeltaTime;
        relaxHead += (dir.RotateYDeg(toRotate) - dir) * targetLen / 2;
        relaxTail = SetDistance(relaxHead, pivot, targetLen);
        relaxTail.AssertIsFinite();
        relaxHead.AssertIsFinite();
    }

    static List<SecondaryAnimationChain> compList = new List<SecondaryAnimationChain>();
    public void SetRelaxedPositions()
    {
        reHead.position = relaxHead;
        reTail.position = relaxTail;
        GetComponentsInChildren<SecondaryAnimationChain>(compList);
        for (int i = 0; i < compList.Count; i++)
            compList[i].Restart();

    }

    // closes point
    public Vector3 ClosestPointOnSheep(Vector3 pos) 
    {
        var npos = relaxTail;// - scare * Time.fixedDeltaTime;
        var ndir = relaxHead * 1.5f - relaxTail * .5f - npos;
        var t1 = Vector3.Dot(pos - npos, ndir) / ndir.sqrMagnitude;
        t1 = Mathf.Clamp01(t1);
        var projected = npos + ndir * t1;
        Debug.DrawLine(pos, projected, Color.cyan);
        return projected;
    }

    public static Vector3 SetDistance(Vector3 origin, Vector3 target, float dist)
    {
        return origin + (target - origin).normalized * dist;
    }

    #endregion
}
