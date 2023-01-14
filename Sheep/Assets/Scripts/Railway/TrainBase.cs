using Recoil;
using UnityEngine;

public class TrainBase : MonoBehaviour
{
    [SerializeField]
    ConfigurableJoint lockJoint;
    [HideInInspector]
    [SerializeField]
    protected new Rigidbody rigidbody;
    protected ReBody reBody;

    protected float kMovementThreshold = 0.01f;
    internal bool IsMoving => reBody.velocity.sqrMagnitude > kMovementThreshold;
    protected bool WasMovingLastFrame { get; set; }
    public bool CanMove { get; set; } = true;

    internal Rigidbody Anchor => lockJoint.connectedBody;
    internal Rigidbody Rigidbody => rigidbody;

    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();
    }

    virtual protected void Start()
    {
        reBody = new ReBody(rigidbody);
        WasMovingLastFrame = false;
    }

    internal virtual void LockMovement()
    {
        LockJoint();
        CanMove = false;
    }

    internal virtual void UnlockMovement()
    {
        UnlockJoint();
        WasMovingLastFrame = false;
        CanMove = true;
    }

    protected void LockJoint()
    {
        MoveAnchor();
        lockJoint.xMotion = ConfigurableJointMotion.Locked;
    }

    protected void UnlockJoint()
    {
        lockJoint.xMotion = ConfigurableJointMotion.Free;
    }

    protected void MoveAnchor()
    {
        lockJoint.connectedBody.transform.position = transform.position;
    }
}
