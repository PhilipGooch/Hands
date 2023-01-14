using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using NBG.Core;
using VR.System;
using Recoil;
using NBG.Locomotion;

public class SheepManager : MonoBehaviour
{
    //public GameObject vrSetup;
    //public GameObject nonvrSetup;
    public Sheep sheepPrefab;
    public Sheep blackSheepPrefab;
    List<Sheep> all = new List<Sheep>();
    public int count = 5;
    [SerializeField]
    float radius = 1;
    public int respawnDepth = 15;
    public int blackSheepCount = 0;
    [SerializeField]
    SheepCheckpoint startCheckpoint;
    [SerializeField]
    SheepCheckpoint blackSheepSpawnCheckpoint;

    public const int MAX = 64;
    const int respawnRaycastDistance = 10;
    const float sheepRadius = 0.25f;
    Vector3 respawnUpwardsOffset = Vector3.up * 2;


    List<Rigidbody> playerHeldObjects = new List<Rigidbody>();
    List<ThreateningObject> threateningObjects = new List<ThreateningObject>();
    List<BallLocomotion> sheepLocomotions = new List<BallLocomotion>();
    BallLocomotionSystem locomotionGroup;

    //public static bool isVR;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        //isVR = SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess;
        //vrSetup.SetActive(isVR);
        //nonvrSetup.SetActive(!isVR);\
        count = Mathf.Min(MAX, count);
        //Sheep instantiation is rather slow. Make sure to not do it on the same frame we load a level.
        yield return new WaitForFixedUpdate();

        for (int i = 0; i < count; i++)
        {
            SpawnSheep(sheepPrefab, i, startCheckpoint ? startCheckpoint.CheckpointPosition : transform.position);
            all[i].checkpoint = startCheckpoint;
            sheepLocomotions.Add(all[i].SheepLocomotion);
            Respawn(all[i], 0, i, count);
            // Instantiate one sheep per frame for better performance
            yield return new WaitForFixedUpdate();
        }

        for (int i = count; i < count + blackSheepCount; i++)
        {
            SpawnSheep(blackSheepPrefab, i, blackSheepSpawnCheckpoint.CheckpointPosition);
            all[i].checkpoint = blackSheepSpawnCheckpoint;
            sheepLocomotions.Add(all[i].SheepLocomotion);
            Respawn(all[i], 0, i, blackSheepCount);
            yield return new WaitForFixedUpdate();
        }

        Sheep.Separate(all);

        if (all.Count > 0)
        {
            locomotionGroup = new BallLocomotionSystem(sheepLocomotions, (int)Layers.Climbable);
            locomotionGroup.AddLocomotionHandler(new Flocking(2f, 0.8f, 0.1f, 0.3f));
            locomotionGroup.AddLocomotionHandler(new ObstacleAvoidance((int)Layers.Climbable, 4f, 9, 20f));
            locomotionGroup.AddLocomotionHandler(new EdgeAvoidance((int)Layers.Climbable, all[0].SheepLocomotion.MaxClimbableSlope, 30f, 3f, 0.5f, 0.85f, 1f, 1.5f));
        }
    }

    void SpawnSheep(Sheep prefab, int id, Vector3 position)
    {
        all.Add(Instantiate(prefab, position, Quaternion.identity, transform));
        all[id].id = id;
        all[id].name = "Sheep " + id;
        RigidbodyRegistration.RegisterHierarchy(all[id].gameObject);
    }

    private void OnEnable()
    {
        Hand.onHandFreeTrigger += OnHandFreeTrigger;
        Hand.onAttachObject += OnPlayerObjectPickup;
        Hand.onDetachObject += OnPlayerObjectRelease;
    }

    private void OnDisable()
    {
        Hand.onHandFreeTrigger -= OnHandFreeTrigger;
        Hand.onAttachObject -= OnPlayerObjectPickup;
        Hand.onDetachObject -= OnPlayerObjectRelease;
    }

    void Respawn(Sheep s, float height, int sheepNumber, int maxSheep, float randomFactor = 1f)
    {
        var progress = sheepNumber / (float)maxSheep;
        var offsetAngle = 360f * progress;
        var spawnRadiusWithRandomOffset = (radius * 0.5f + Random.value * radius * 0.5f) * randomFactor;
        spawnRadiusWithRandomOffset = Mathf.Min(spawnRadiusWithRandomOffset, s.checkpoint ? s.checkpoint.MaxRadius : radius);
        var offset = SheepMath3d.VectorFromAngle(offsetAngle * Mathf.Deg2Rad, Vector3.forward, Vector3.up) * spawnRadiusWithRandomOffset;
        var spawnPosition = s.checkpoint ? s.checkpoint.CheckpointPosition : transform.position;
        var upwardsOffset = s.checkpoint ? s.checkpoint.RespawnHeight * Vector3.up : respawnUpwardsOffset;
        var raycastDistance = s.checkpoint ? s.checkpoint.RespawnHeight + 5f : respawnRaycastDistance;
        if (Physics.SphereCast(spawnPosition + offset + upwardsOffset, sheepRadius, Vector3.down, out var hit, raycastDistance, (int)Layers.Climbable, QueryTriggerInteraction.Ignore))
        {
            s.Respawn(hit.point, hit.point + Vector3.up * height, Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up));
            s.NeedsRespawn = false;
        }
    }

    void PlayDespawnParticles(Vector3 pos, Vector3 size)
    {
        GameParameters.Instance.despawnEffect.Create(pos, Quaternion.identity, size);
    }

    public void PlaceSheep(int number, Vector3 position, Quaternion rotation)
    {
        all[number].Place(position, rotation);
    }

    void OnHandFreeTrigger(Hand hand, float trigger)
    {
        if (hand.IsThreat && trigger > 0)
        {
            Threat.AddRegularThreat(new BoxBoundThreat(hand.HandPositions.ThreatPosition, trigger));
        }
    }

    void OnPlayerObjectPickup(Rigidbody body)
    {
        if (!playerHeldObjects.Contains(body))
        {
            var colliders = body.GetComponentsInChildren<Collider>(true);
            var threateningObject = body.gameObject.AddComponent<ThreateningObject>();
            var threatSettings = new ThreateningObject.ThreatSettings(true, true, true);
            var threatConfig = body.GetComponent<ThreatConfiguration>();
            if (threatConfig != null)
            {
                threatSettings = threatConfig.threatSettings;
            }
            threateningObject.Initialize(colliders, threatSettings);
            threateningObjects.Add(threateningObject);
        }
        playerHeldObjects.Add(body);
    }

    void OnPlayerObjectRelease(Rigidbody body)
    {
        playerHeldObjects.Remove(body);
        if (!playerHeldObjects.Contains(body))
        {
            var threats = body.GetComponents<ThreateningObject>();
            foreach (var threat in threats)
            {
                if (threateningObjects.Contains(threat))
                {
                    threateningObjects.Remove(threat);
                    Destroy(threat);
                }
            }
        }
    }

    List<Collider> tempColliderList = new List<Collider>();

    private void FixedUpdate()
    {
        //for (int i = 0; i < count; i++)
        //    all[i].threats.Clear();

        for (int i = threateningObjects.Count - 1; i >= 0; i--)
        {
            var obj = threateningObjects[i];
            // Threat destroyed
            if (obj == null)
            {
                threateningObjects.RemoveAt(i);
                continue;
            }

            if (obj.isActiveAndEnabled)
            {
                tempColliderList.Clear();
                obj.GetAllThreateningColliders(tempColliderList);
                foreach (var collider in tempColliderList)
                {
                    Threat.AddRegularThreat(new BoxBoundThreat(collider, 0.2f, 9f));
                }
            }
        }

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].reHead.position.y < -respawnDepth || all[i].NeedsRespawn)
            {
                PlayDespawnParticles(all[i].reHead.position, Vector3.one);

                var respawnHeight = all[i].checkpoint ? all[i].checkpoint.RespawnHeight : respawnDepth;
                Respawn(all[i], respawnHeight, i, all.Count, 0.1f);
            }
        }

        Sheep.Process(all);
        if (locomotionGroup != null)
        {
            locomotionGroup.UpdateLocomotion();
        }
        Threat.ClearAllThreats();
    }

    private void OnDestroy()
    {
        if (locomotionGroup != null)
        {
            locomotionGroup.Dispose();
            locomotionGroup = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        var startPos = transform.position + respawnUpwardsOffset;
        if (startCheckpoint != null)
        {
            startPos = startCheckpoint.CheckpointPosition + Vector3.up * startCheckpoint.RespawnHeight;
        }
        if (GetSpawnPosition(startPos, out var hit))
        {
            DrawSpawnGizmo(startPos, hit, Color.white);
        }

        if (blackSheepCount > 0 && blackSheepSpawnCheckpoint != null)
        {
            startPos = blackSheepSpawnCheckpoint.CheckpointPosition + Vector3.up * blackSheepSpawnCheckpoint.RespawnHeight;
            if (GetSpawnPosition(startPos, out hit))
            {
                DrawSpawnGizmo(startPos, hit, Color.black);
            }
        }
    }

    bool GetSpawnPosition(Vector3 position, out RaycastHit hit)
    {
        return Physics.SphereCast(position, sheepRadius, Vector3.down, out hit, respawnRaycastDistance, (int)Layers.Walls, QueryTriggerInteraction.Ignore);
    }

    void DrawSpawnGizmo(Vector3 position, RaycastHit hit, Color color)
    {
        var middle = (position + hit.point) / 2f;
        var size = radius * 2;
        Gizmos.color = color;
        Gizmos.DrawWireCube(middle, new Vector3(size, hit.distance, size));
    }

    //private void OnDrawGizmos()
    //{
    //    Sheep.DrawFlocks();
    //}
}
