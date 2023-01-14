using NBG.Wind;
using Recoil;
using UnityEditor;
using UnityEngine;
public class Balloon : MonoBehaviour, IWindMultiplier, IWindReceiver
{
    [SerializeField]
    Transform start;

    [SerializeField]
    float liftLossIncreasePerTouchingObj = 1f;
    [SerializeField]
    float dragDecreasePerTouchingObj = 2f;

    [SerializeField]
    float maximumLift = 5f;
    [SerializeField]
    float minimumLift = 0;
    [SerializeField]
    float liftGain = 1f;
    [SerializeField]
    float liftLoss = 1f;
    [SerializeField]
    float windMultiplier = 1f;
    [SerializeField]
    float scaleIncrease = 1f;
    [SerializeField]
    Collider airFillZone;
    [SerializeField]
    int forceMulti;
    [SerializeField]

    float currentLift;
    [SerializeField]
    float maxDistanceFromSpawn;

    [SerializeField]
    AnimationCurve speedFalloff;
    [SerializeField]
    AnimationCurve opositeForce;

    float normalisedDistToSpawn;

    new Rigidbody rigidbody;
    ReBody reBody;
    IsTouchedByGrabbedObj isTouchedByGrabbedObj;

    Vector3 dir;
    Vector3 wind;

    Vector3 startScale = Vector3.one;
    Vector3 startPosition;


    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        isTouchedByGrabbedObj = GetComponent<IsTouchedByGrabbedObj>();
        currentLift = minimumLift;
        startScale = transform.localScale;
        startPosition = start.position;

    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
        SubscribeToEvents();
    }
  
    void FixedUpdate()
    {
        UpdateLift();
        UpdateScale();
        UpdateNormalisedDistToSpawn();
        Move();

        var forceToAdd = -Physics.gravity * (reBody.mass * (currentLift + 1f)) + (dir + GetOppositeForce());

        reBody.AddForce(forceToAdd, ForceMode.Force);
    }
    void SubscribeToEvents()
    {
        isTouchedByGrabbedObj.onGrabbedTouchStarted += OnTouchingStarted;
        isTouchedByGrabbedObj.onGrabbedTouchEnded += OnTouchingEnded;
    }
    void OnTouchingStarted()
    {

        reBody.drag -= dragDecreasePerTouchingObj;
        liftLoss += liftLossIncreasePerTouchingObj;
        liftLoss = Mathf.Abs(liftLoss);
    }
    void OnTouchingEnded()
    {
        reBody.drag += dragDecreasePerTouchingObj;
        liftLoss -= liftLossIncreasePerTouchingObj;
        liftLoss = Mathf.Abs(liftLoss);
    }

    public void OnReceiveWind(Vector3 wind)
    {
        this.wind = wind.normalized;
    }

    public float GetWindMultiplier(Vector3 windDirection)
    {
        return windMultiplier;
    }

    Vector3 DirectionToSpawn()
    {
        return (startPosition - transform.position).normalized;
    }
    void UpdateNormalisedDistToSpawn()
    {
        normalisedDistToSpawn = Vector3.Distance(transform.position, startPosition) / maxDistanceFromSpawn;

    }
    void UpdateLift()
    {
        currentLift -= liftLoss * Time.fixedDeltaTime;
        currentLift = Mathf.Clamp(currentLift, minimumLift, maximumLift);
    }

    void UpdateScale()
    {
        transform.localScale = startScale + startScale * scaleIncrease * (currentLift - minimumLift) / (maximumLift - minimumLift);
    }

    public void FillAir(float amount)
    {
        currentLift += amount * liftGain * Time.fixedDeltaTime;
    }

    public void Move()
    {
        dir = wind * GetForceMulti(wind);
        wind = Vector3.zero;
    }
    Vector3 GetOppositeForce()
    {
        return DirectionToSpawn() * GetOppositeForceMulti();
    }
    float GetOppositeForceMulti()
    {
        float adjustedMulti = opositeForce.Evaluate(normalisedDistToSpawn);
        return forceMulti * adjustedMulti;

    }
    float GetForceMulti(Vector3 windDir)
    {
        float windAndSpawnDot = Vector3.Dot(windDir, DirectionToSpawn());
        float adjustedMulti;

        if (windAndSpawnDot > 0) // blowing towards the spawn
        {
            adjustedMulti = 1;
        }
        else // blowing away for the spawn
        {
            adjustedMulti = speedFalloff.Evaluate(normalisedDistToSpawn);

        }

        return forceMulti * adjustedMulti * 3;
    }


    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(start.position, maxDistanceFromSpawn);
    }

   
}
