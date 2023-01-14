using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using NBG.Core;

public class ObjectRespawnPoint : MonoBehaviour
{
    private EmptySpaceChecker emptySpaceChecker;

    private List<ActorComponent> respawnQueue = new List<ActorComponent>();

    public event Action<ActorComponent, RigidTransform> onSpawn;

    private void Start()
    {
        emptySpaceChecker = new EmptySpaceChecker(transform);
    }

    private void FixedUpdate()
    {
        if (respawnQueue.Count > 0)
        {
            var firstInLine = respawnQueue[0];
            bool canSpawn = CheckIfFits(firstInLine.BoundsCache);
            if (canSpawn)
            {
                respawnQueue.RemoveAt(0);
                onSpawn?.Invoke(firstInLine, new RigidTransform(transform.rotation, transform.position));
            }
        }
    }

    public bool CheckIfFits(IReadOnlyList<BoxBounds> boxBounds)
    {
        return emptySpaceChecker.CheckIfFits(boxBounds);
    }

    public void AddToQueue(ActorComponent respawnTarget)
    {
        if (!respawnQueue.Contains(respawnTarget))
        {
            respawnQueue.Add(respawnTarget);
        }
    }

    internal void RemoveFromQueue(ActorComponent toRespawn)
    {
        respawnQueue.Remove(toRespawn);
    }

    private void OnDrawGizmosSelected()
    {
        if (respawnQueue.Count > 0)
        {
            var firstInLine = respawnQueue[0];
            bool canSpawn = emptySpaceChecker.CheckIfFits(firstInLine.BoundsCache);

            if (canSpawn)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;

            var oldMatrix = Gizmos.matrix;
            foreach (var item in firstInLine.BoundsCache)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(item.center), transform.rotation * item.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, item.size);
            }
            Gizmos.matrix = oldMatrix;
        }
    }
}
