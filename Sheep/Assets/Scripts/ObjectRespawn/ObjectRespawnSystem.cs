using System.Collections.Generic;
using UnityEngine;
using NBG.Actor;
using Unity.Mathematics;
using Recoil;

public class ObjectRespawnSystem : MonoBehaviour
{
    private static ObjectRespawnSystem instance;
    public static ObjectRespawnSystem Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("ObjectRespawnSystem");
                instance = go.AddComponent<ObjectRespawnSystem>();
            }
            return instance;
        }
    }

    ObjectRespawnPoint[] possibleRespawnPoints = null;

    Camera mainCamera => Player.Instance.mainCamera;

    //dictionary, in case multiple different objects from different spawn points want to respawn this turn.
    Dictionary<ActorComponent, List<RigidTransform>> respawnThisFrame = new Dictionary<ActorComponent, List<RigidTransform>>();

    private void Awake()
    {
        possibleRespawnPoints = FindObjectsOfType<ObjectRespawnPoint>();
        foreach (var respawnPoint in possibleRespawnPoints)
        {
            respawnPoint.onSpawn += ReadyToRespawn;
        }
    }

    private void OnDestroy()
    {
        instance = null;
        foreach (var respawnPoint in possibleRespawnPoints)
        {
            respawnPoint.onSpawn -= ReadyToRespawn;
        }
    }

    private void FixedUpdate()
    {
        foreach (var item in respawnThisFrame)
        {
            var minSqrDistance = float.MaxValue;

            int minDistanceID = 0;

            for (int i = 0; i < item.Value.Count; i++)
            {
                var sqrDistance = Vector3.SqrMagnitude(mainCamera.transform.position - (Vector3)item.Value[i].pos);

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    minDistanceID = i;
                }
            }

            ActorSystem.Main.SetActorSpawnPlacement(item.Key, World.environmentId, item.Value[minDistanceID]);
            ActorSystem.Main.RequestSpawn(item.Key);

            foreach (var respawnPoint in possibleRespawnPoints)
            {
                respawnPoint.RemoveFromQueue(item.Key);
            }
        }

        respawnThisFrame.Clear();
    }

    public void AddToRespawnQueue(ActorComponent toRespawn, ObjectRespawnPoint[] allowedRespawnPoints = null)
    {
        ActorSystem.Main.Despawn(toRespawn);

        //Since there is always going to be a camera, this variable can be treated as camera position
        var fallbackRespawnPosition = new Vector3(0, 30, 0);

        if (mainCamera != null)
        {
            fallbackRespawnPosition = mainCamera.transform.position;
        }

        var pointsToCheck = possibleRespawnPoints;
        if (allowedRespawnPoints != null && allowedRespawnPoints.Length > 0)
        {
            pointsToCheck = allowedRespawnPoints;
        }

        if (pointsToCheck != null && pointsToCheck.Length > 0)
        {
            foreach (var respawner in pointsToCheck)
            {
                respawner.AddToQueue(toRespawn);
            }
        }
        else
        {
            Debug.LogWarning("No viable respawn points found!! Spawning near player");
            ReadyToRespawn(toRespawn, new RigidTransform(Quaternion.identity, fallbackRespawnPosition));
        }
    }

    private void ReadyToRespawn(ActorComponent toRespawn, RigidTransform location)
    {
        if (respawnThisFrame.ContainsKey(toRespawn))
        {
            respawnThisFrame[toRespawn].Add(location);
        }
        else
        {
            respawnThisFrame.Add(toRespawn, new List<RigidTransform>() { location });
        }
    }
}
