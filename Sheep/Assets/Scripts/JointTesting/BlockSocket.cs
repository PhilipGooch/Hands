using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BlockSocket : MonoBehaviour
{
    public BlockSocket TargetSocket { get; private set; }
    public Block ParentBlock { get; private set; }
    public float WorldDiameter { get; private set; }
    public BlockConnection Connection { get; set; }

    [Tooltip("The twist angle that the socket will snap to. Must be a divisor of 360.")]
    [Range(1, 360)]
    public int TwistSnapAngle = 90;

    protected new Rigidbody rigidbody;

    [SerializeField]
    Vector3 normal;
    public Vector3 Normal => normal;

    public delegate void AddConnectionDelegate(BlockSocket block, BlockSocket otherBlock);
    public event AddConnectionDelegate OnAddConnection;
    public delegate void RemoveConnectionDelegate(BlockConnection connection);
    public event RemoveConnectionDelegate OnRemoveConnection;

    protected virtual void Awake()
    {
        Debug.Assert(TwistSnapAngle >= 1, "TwistSnapAngle must be greater than or equal to 1.");
        Debug.Assert(360 % TwistSnapAngle == 0, "TwistSnapAngle must be a divisor of 360.");
        rigidbody = GetComponentInParent<Rigidbody>();
        ParentBlock = GetComponentInParent<Block>();
    }

    protected virtual void Start()
    {
        WorldDiameter = GetComponent<SphereCollider>().radius * transform.lossyScale.x * 2;
    }

    private void OnTriggerEnter(Collider other)
    {
        BlockSocket otherSocket = other.GetComponent<BlockSocket>();
        if (otherSocket)
        {
            if (!TargetSocket && !otherSocket.TargetSocket)
            {
                Debug.Assert(Connection == null);
                TargetSocket = otherSocket;
                otherSocket.TargetSocket = this;
                OnAddConnection?.Invoke(this, TargetSocket);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BlockSocket otherSocket = other.GetComponent<BlockSocket>();
        if (otherSocket && otherSocket == TargetSocket)
        {
            Debug.Assert(TargetSocket);
            Debug.Assert(TargetSocket.TargetSocket == this);
            Debug.Assert(Connection != null);
            Debug.Assert(TargetSocket.Connection != null);
            Debug.Assert(Connection.A == this && Connection.B == TargetSocket ||
                         Connection.B == this && Connection.A == TargetSocket);
            OnRemoveConnection?.Invoke(Connection);
            otherSocket.TargetSocket = null;
            TargetSocket = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (Connection != null && Connection.Locked)
        {
            Gizmos.color = Color.black;
            float size = 1.1f;
            float thickness = 0.1f;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.LookRotation(normal), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size, size, thickness));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
