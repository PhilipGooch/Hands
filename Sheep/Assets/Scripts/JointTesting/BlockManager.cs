using System.Collections.Generic;
using UnityEngine;

public class BlockManager : SingletonBehaviour<BlockManager>
{
    HashSet<Block> blocks = new HashSet<Block>();
    List<BlockConnection> connections = new List<BlockConnection>();

    // Data structures used for graph traversal.
    Queue<Block> blocksToVisit = new Queue<Block>();
    Queue<Block> visitedBlocks = new Queue<Block>();

    // When two hands are grabbing, the main hand is the second hand to grab.
    // If a group of blocks is grabbed by both hands, the main hand that will remove blocks from that group.
    Hand mainHand;

    [SerializeField]
    [Tooltip("Sets the max drive force for each block's socket joints. " +
             "Determines how much force will be applied when blocks are snapping into position.")]
    float driveForce;

    [SerializeField]
    [Tooltip("Sets the max angular drive force for each block's socket joints. " +
             "Determines how much angular force will be applied when blocks are snapping into position.")]
    float angularDriveForce;

    [SerializeField]
    [Tooltip("Dictates how close sockets need to be in order for connections to lock.")]
    protected float lockDistance;

    protected override void Awake()
    {
        base.Awake();
    }

    public void AddBlock(Block block)
    {
        Debug.Assert(!blocks.Contains(block));
        blocks.Add(block);
        block.onGrab += OnGrab;
        block.onRelease += OnRelease;
        foreach (BlockSocket socket in block.Sockets)
        {
            socket.OnAddConnection += OnAddConnection;
            socket.OnRemoveConnection += OnRemoveConnection;
        }
    }

    public void RemoveBlock(Block block)
    {
        Debug.Assert(blocks.Contains(block));
        blocks.Remove(block);
        block.onGrab -= OnGrab;
        foreach (BlockSocket socket in block.Sockets)
        {
            socket.OnAddConnection -= OnAddConnection;
            socket.OnRemoveConnection -= OnRemoveConnection;
        }
    }

    public void AddConnection(BlockSocket a, BlockSocket b)
    {
        foreach (BlockConnection connection in connections)
        {
            if ((connection.A == a && connection.B == b) ||
                (connection.B == a && connection.A == b))
            {
                Debug.Assert(false, "Connection already exists.");
            }
        }
        BlockConnection c = new BlockConnection(a, b);
        connections.Add(c);
        a.Connection = c;
        b.Connection = c;
    }

    public void RemoveConnection(BlockConnection connection)
    {
        Debug.Assert(connections.Contains(connection));
        connection.RemoveJoint();
        connections.Remove(connection);
        connection.A.Connection = null;
        connection.B.Connection = null;
    }

    void FixedUpdate()
    {
        foreach (BlockConnection connection in connections)
        {
            Block blockA = connection.A.ParentBlock;
            Block blockB = connection.B.ParentBlock;
            if (connection.CloseEnoughToLock(lockDistance))
            {
                if ((blockA.IsGrabbed && blockB.IsGrabbed) ||
                    (blockA.IsGrabbed && blockA.GetHand() == mainHand && !blockB.IsGrabbed && BlockConnectedToGrabbedOrKinematicGroup(blockB, blockA)) ||
                    (blockB.IsGrabbed && blockB.GetHand() == mainHand && !blockA.IsGrabbed && BlockConnectedToGrabbedOrKinematicGroup(blockA, blockB)))
                {
                    connection.Unlock();
                }
                else
                {
                    connection.Lock();
                }
            }
            else
            {
                connection.Unlock();
            }
            connection.UpdateDriveForces(driveForce, angularDriveForce);
        }
    }

    void FindConnectedBlocks(Block initialBlock, Block blockToIgnore = null)
    {
        blocksToVisit.Clear();
        visitedBlocks.Clear();
        blocksToVisit.Enqueue(initialBlock);
        while (blocksToVisit.Count > 0)
        {
            var currentBlock = blocksToVisit.Dequeue();
            visitedBlocks.Enqueue(currentBlock);
            foreach (BlockSocket socket in currentBlock.Sockets)
            {
                if (socket.Connection != null && socket.Connection.Locked)
                {
                    Debug.Assert(socket.TargetSocket);
                    Block connectedBlock = socket.TargetSocket.ParentBlock;
                    if (connectedBlock == blockToIgnore)
                    {
                        continue;
                    }
                    if (!visitedBlocks.Contains(connectedBlock) && !blocksToVisit.Contains(connectedBlock))
                    {
                        blocksToVisit.Enqueue(connectedBlock);
                    }
                }
            }
        }
    }

    bool BlockConnectedToGrabbedOrKinematicGroup(Block initialBlock, Block blockToIgnore)
    {
        FindConnectedBlocks(initialBlock, blockToIgnore);
        foreach (Block visitedBlock in visitedBlocks)
        {
            if (visitedBlock.IsKinematic || visitedBlock.IsGrabbed)
            {
                return true;
            }
        }
        return false;
    }

    void OnGrab(Block grabbedBlock, Hand hand)
    {
        mainHand = hand;
        //SetTreePhysics(); // Disabled until SimpleTreePhysics bug fix.
    }

    void OnRelease(Block grabbedBlock, Hand hand)
    {
        if (GetBlockGrabbedByHand(hand.otherHand))
        {
            mainHand = hand.otherHand;
        }
        else
        {
            mainHand = null;
        }
        //SetTreePhysics(); // Disabled until SimpleTreePhysics bug fix.
    }

    void OnAddConnection(BlockSocket a, BlockSocket b)
    {
        AddConnection(a, b);
        //SetTreePhysics(); // Disabled until SimpleTreePhysics bug fix.
    }

    void OnRemoveConnection(BlockConnection connection)
    {
        RemoveConnection(connection);
        //SetTreePhysics(); // Disabled until SimpleTreePhysics bug fix.
    }

    Block GetBlockGrabbedByHand(Hand hand)
    {
        return hand ? hand.attachedBody ? hand.attachedBody.GetComponent<Block>() : null : null;
    }

    void ResetTreePhysics()
    {
        foreach (Block block in blocks)
        {
            block.ConnectedBodies.Clear();
        }
    }

    void SetTreePhysics()
    {
        ResetTreePhysics();
        Block rightHandBlock = GetBlockGrabbedByHand(Player.Instance.rightHand);
        Block leftHandBlock = GetBlockGrabbedByHand(Player.Instance.leftHand);
        if (leftHandBlock)
        {
            FindConnectedBlocks(leftHandBlock);
            foreach (Block block in visitedBlocks)
            {
                leftHandBlock.ConnectedBodies.Add(block.RecoilBody);
            }
        }
        if (rightHandBlock)
        {
            FindConnectedBlocks(rightHandBlock);
            foreach (Block block in visitedBlocks)
            {
                rightHandBlock.ConnectedBodies.Add(block.RecoilBody);
            }
        }
    }
}
