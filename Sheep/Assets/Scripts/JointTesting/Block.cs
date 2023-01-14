using System.Collections.Generic;
using UnityEngine;
using Recoil;

public class Block : MonoBehaviour, IGrabNotifications//, ITreePhysics // Disabled until SimpleTreePhysics bug fix.
{
    BlockSocket[] sockets;
    List<ReBody> connectedBodies;
    ReBody recoilBody;
    SimpleTreePhysics simpleTreePhysics;

    public BlockSocket[] Sockets => sockets;
    public List<ReBody> ConnectedBodies => connectedBodies;
    public ReBody RecoilBody => recoilBody;
    public bool IsKinematic => recoilBody.rigidbody.isKinematic; // BUG: index out of bounds if using recoilBody.isKinematic.
    public bool IsGrabbed => Player.Instance.GetHandThatIsGrabbingBody(recoilBody.rigidbody);

    public delegate void GrabbedDelegate(Block block, Hand hand);
    public event GrabbedDelegate onGrab;
    public delegate void ReleasedDelegate(Block block, Hand hand);
    public event ReleasedDelegate onRelease;

    protected virtual void Awake()
    {
        sockets = GetComponentsInChildren<BlockSocket>();
        recoilBody = new ReBody(GetComponent<Rigidbody>());
        connectedBodies = new List<ReBody>();
        simpleTreePhysics = new SimpleTreePhysics(connectedBodies);
        connectedBodies.Add(recoilBody);
    }

    protected virtual void Start()
    {
        BlockManager.Instance.AddBlock(this);
    }

    private void OnDestroy()
    {
        BlockManager.Instance.RemoveBlock(this);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            onGrab?.Invoke(this, hand);
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            onRelease?.Invoke(this, hand);
        }
    }

    public Hand GetHand()
    {
        return Player.Instance.GetHandThatIsGrabbingBody(recoilBody.rigidbody);
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        simpleTreePhysics.AddTreeAcceleration(worldPos, acc);
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        return simpleTreePhysics.CalculateTreeVelocity(worldPos);
    }

    public bool TreePhysicsActive()
    {
        return true;
    }
}
