using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepCheckpoint : MonoBehaviour
{
    [SerializeField]
    public List<SheepCheckpoint> previousCheckpoints = new List<SheepCheckpoint>();

    public float MaxRadius { get; private set; } = 1f;
    [SerializeField]
    float respawnHeight = 5f;

    public event Action onCheckpointReached;

    public float RespawnHeight
    {
        get { return respawnHeight; }
        private set { respawnHeight = value; }
    }

    public Vector3 CheckpointPosition
    {
        get => transform.position;
    }

    private void Awake()
    {
        MaxRadius = Mathf.Min(transform.lossyScale.x, transform.lossyScale.z) / 2f;
    }

    private void OnTriggerEnter(Collider other)
    {
        var sheep = other.GetComponentInParent<Sheep>();
        if (sheep != null)
        {
            var currentCheckpoint = sheep.checkpoint;
            //first checkpoint of the level
            if (currentCheckpoint == null || currentCheckpoint.previousCheckpoints.Count == 0 || !currentCheckpoint.IsPreviousCheckpoint(this, 0))
            {
                sheep.checkpoint = this;
                onCheckpointReached?.Invoke();
            }
        }
    }

    bool IsPreviousCheckpoint(SheepCheckpoint target, int depth)
    {
        if (depth > 64)
        {
            Debug.LogError("Possible infinite cycle for sheep checkpoints!");
            return false;
        }
        if (previousCheckpoints.Contains(target))
        {
            return true;
        }
        else
        {
            foreach (var checkpoint in previousCheckpoints)
            {
                if (checkpoint != null)
                {
                    if (checkpoint.IsPreviousCheckpoint(target, depth + 1))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

#if UNITY_EDITOR
    static Color editorZoneColor = new Color(190f / 255, 252f / 255, 73f / 255, 0.5f);

    private void OnDrawGizmos()
    {
        var oldMatrix = Gizmos.matrix;
        var oldColor = Gizmos.color;

        Gizmos.color = editorZoneColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = editorZoneColor;
        DrawPreviousCheckpointConnections(0);
        DrawSpawnPositionInTheAir();
    }

    void DrawPreviousCheckpointConnections(int depth)
    {
        if (depth > 64)
        {
            Debug.LogError("Infinite cycle in respawn points detected!");
            return;
        }
        foreach (var checkpoint in previousCheckpoints)
        {
            if (checkpoint != null)
            {
                Gizmos.DrawLine(transform.position, checkpoint.transform.position);
                checkpoint.DrawPreviousCheckpointConnections(depth + 1);
            }
        }
    }

    void DrawSpawnPositionInTheAir()
    {
        var oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * RespawnHeight, transform.rotation, transform.lossyScale);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMatrix;
    }
#endif
}
