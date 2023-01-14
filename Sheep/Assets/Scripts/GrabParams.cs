using UnityEngine;

[CreateAssetMenu(fileName = "GrabParams", menuName = "Grab Parameters", order = 1)]
public class GrabParams : ScriptableObject
{
    public GrabParams fallback;
    private GrabParams _baseParams => fallback!=null?fallback:defaults;
        

    [SerializeField]
    private bool _overrideMassMultiplier = false;
    [SerializeField]
    [Tooltip("Carrying object with reduced mass will have less impact on environment during collisions")]
    private float _massMultiplier = 1;
    public float massMultiplier => _overrideMassMultiplier||this== defaults ? _massMultiplier : _baseParams.massMultiplier;

    [SerializeField]
    private bool _overrideVelocityTracking = false;
    [Tooltip("How closely hand velocity should be tracked. 0 - smoother tracking lagging a bit behind, 1 - quick response, but overshooting on stop.")]
    [SerializeField]
    private float _velocityTracking = .5f;
    public float velocityTracking => _overrideVelocityTracking || this == defaults ? _velocityTracking : _baseParams.velocityTracking;

    [SerializeField]
    private bool _overrideMaxLinearVelocity = false;
    [Tooltip("Maximum linear velocity at anchor, default 30m/s, use less for heavy objects")]
    [SerializeField]
    private float _maxLinearVelocity = 100;
    public float maxLinearVelocity => _overrideMaxLinearVelocity || this == defaults ? _maxLinearVelocity : _baseParams.maxLinearVelocity;

    [SerializeField]
    private bool _overrideMaxAngularVelocity = false;
    [Tooltip("Maximum angular velocity at anchor. heavier objects should use smaller values. When 0, only linear constraints are used.")]
    [SerializeField]
    private float _maxAngularVelocity = 100;
    public float maxAngularVelocity => _overrideMaxAngularVelocity || this == defaults ? _maxAngularVelocity : _baseParams.maxAngularVelocity;

    [SerializeField]
    private bool _overrideMaxLinearAcceleration = false;
    [Tooltip("Maximum linear acceleration for moving body. Good value 500, smaller to feel heavy. If 0 will be calculated based on object mass.")]
    [SerializeField]
    private float _maxLinearAcceleration = 0;
    public float maxLinearAcceleration => _overrideMaxLinearAcceleration || this == defaults ? _maxLinearAcceleration : _baseParams.maxLinearAcceleration;

    [SerializeField]
    private bool _overrideMaxAngularAcceleration = false;
    [Tooltip("Maximum angular acceleration for moving body. Good value 30, smaller to feel heavy. If 0 will be calculated from linear acceleration and distance of anchor point from center.")]
    [SerializeField]
    private float _maxAngularAcceleration = 0;
    public float maxAngularAcceleration => _overrideMaxAngularAcceleration || this == defaults ? _maxAngularAcceleration : _baseParams.maxAngularAcceleration;

    [Tooltip("When true - angular parameters are used, otherwise only linear.")]
    public bool angularControl = true;

    [SerializeField]
    private bool _overrideReanchor = false;
    [SerializeField]
    [Tooltip("If anchor can slide on object whem facing resistance. Prevents surprising motion when relaxing objects that were pushing against other objects.")]
    private bool _reanchor = false;
    public bool reanchor => _overrideReanchor || this == defaults ? _reanchor : _baseParams.reanchor;


    [SerializeField]
    private bool _overrideSnapToCollider = false;
    [SerializeField]
    [Tooltip("Limit anchor motion within collider. If false anchor can move outside the object being carried.")]
    private bool _snapToCollider = true;
    public bool snapToCollider => _overrideSnapToCollider || this == defaults ? _snapToCollider : _baseParams.snapToCollider;


    [SerializeField]
    private bool _overrideLinearAnchorRange = false;
    [SerializeField]
    [Tooltip("Joint error threshold at which re-ahncoring starts.")]
    private float _linearAnchorRange = .1f;
    public float linearAnchorRange => _overrideLinearAnchorRange || this == defaults ? _linearAnchorRange : _baseParams.linearAnchorRange;


    [SerializeField]
    private bool _overrideLinearAnchorSpeed = false;
    [SerializeField]
    [Tooltip("Maximum speed of anchor movement. Bigger values will adapt to environment quicker. Smaller will keep original grip for longer.")]
    private float _linearAnchorSpeed = 10;
    public float linearAnchorSpeed => _overrideLinearAnchorSpeed || this == defaults ? _linearAnchorSpeed : _baseParams.linearAnchorSpeed;


    [SerializeField]
    private bool _overrideAngularAnchorRange = false;
    [SerializeField]
    [Tooltip("Joint error threshold at which re-ahncoring starts.")]
    private float _angularAnchorRange = .01f;
    public float angularAnchorRange => _overrideAngularAnchorRange || this == defaults ? _angularAnchorRange : _baseParams.angularAnchorRange;


    [SerializeField]
    private bool _overrideAngularAnchorSpeed = false;
    [SerializeField]
    [Tooltip("Maximum speed of anchor orientation movement. Bigger values will adapt to environment quicker. Smaller will keep original grip for longer.")]
    private float _angularAnchorSpeed = 1;
    public float angularAnchorSpeed => _overrideAngularAnchorSpeed || this == defaults ? _angularAnchorSpeed : _baseParams.angularAnchorSpeed;



    //public bool hasAngularControl => maxAngularVelocity > 0;

    static GrabParams _defaults;
    public static GrabParams defaults
    {
        get
        {
            if (_defaults == null)
            {
                _defaults = ScriptableObject.CreateInstance<GrabParams>();
            }
            return _defaults;
        }
    }

    static GrabParams _sheepParams;
    public static GrabParams sheepParams
    {
        get
        {
            if (_sheepParams == null)
            {
                _sheepParams = ScriptableObject.CreateInstance<GrabParams>();
                _sheepParams.angularControl = false;
                _sheepParams._overrideReanchor = true;
                _sheepParams._reanchor = false;
            }
            return _sheepParams;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Search and destroy any circle-ref
        var encounters = new System.Collections.Generic.List<GrabParams>();
        encounters.Add(this);
        var parent = fallback;
        while(parent != null)
        {
            if (encounters.Contains(parent))
            {
                Debug.LogErrorFormat(this, "Circular reference for {0} grab params detected! This is not allowed, resetting the fallback for {0} to null.", name);
                fallback = null;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                break;
            }
            encounters.Add(parent);
            parent = parent.fallback;
        }
    }
#endif

}
