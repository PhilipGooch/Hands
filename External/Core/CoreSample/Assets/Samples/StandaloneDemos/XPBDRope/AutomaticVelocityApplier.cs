using NBG.Core;
using Recoil;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutomaticVelocityApplier : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
{
    [SerializeField]
    private bool applyVelocity = true;
    [SerializeField]
    private Vector3 velocityDirection = new Vector3(0, 0, 1);
    [SerializeField]
    private bool alternateDirection;
    [SerializeField]
    [Tooltip("If true then uses sine curve for velocity, if false rounds sine velocity turning it to 0, 1, 0, -1, 0...")]
    private bool smoothDirectionChange = true;
    [SerializeField]
    private float timeSineScale = 1f;
    [SerializeField]
    private float velocityStrength = 3f;
    [SerializeField]
    private bool useRecoil = false;

    private Rigidbody rb;
    private int bodyId = -1;

    bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

    void IManagedBehaviour.OnLevelLoaded()
    {
        rb = GetComponent<Rigidbody>();
        OnFixedUpdateSystem.Register(this);
    }

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
        bodyId = ManagedWorld.main.FindBody(rb);
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        OnFixedUpdateSystem.Unregister(this);
    }

    private float GetWeightedForceDirection()
    {
        float swing = (alternateDirection ? Mathf.Sin(Time.fixedTime * timeSineScale) : 1f);

        if (!smoothDirectionChange)
            swing = Mathf.Round(swing);


        return swing * velocityStrength;
    }

    void IOnFixedUpdate.OnFixedUpdate()
    {
        if (!useRecoil || !applyVelocity)
            return;

        World.main.AddLinearVelocity(bodyId, velocityDirection * (GetWeightedForceDirection() * World.main.dt));
    }

    private void FixedUpdate()
    {
        if (useRecoil || !applyVelocity)
            return;

        rb.AddForce(velocityDirection * (GetWeightedForceDirection() * World.main.dt), ForceMode.VelocityChange);
    }
}
