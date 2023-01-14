using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Recoil;
using NBG.Core;

public class ConsumableConverter : MonoBehaviour
{
    [SerializeField]
    TriggerEventSender eventSender;
    [SerializeField]
    GameObject convertedItemToSpawn;
    [SerializeField]
    Transform convertedItemSpawnLocation;
    [SerializeField]
    float spawnVelocity = 0f;
    [SerializeField]
    bool teleportDiscardedItems = true;
    [SerializeField]
    Transform discardTeleportLocation;
    [SerializeField]
    float discardVelocity = 0f;
    [SerializeField]
    int itemsNeededToSpawnConvertedObject = 3;
    [SerializeField]
    SingleShotParticleEffect discardSmokeEffect;

    [SerializeField]
    float maxObjectDiameter = 1f;

    int itemCounter = 0;

    private void Awake()
    {
        eventSender.onTriggerEnter += OnObjectEnter;
    }

    private void OnDestroy()
    {
        eventSender.onTriggerEnter -= OnObjectEnter;
    }

    void OnObjectEnter(Collider target)
    {
        var consumable = target.GetComponentInParent<Consumable>();
        if (consumable != null)
        {
            ConsumeItem(consumable);
        }
        else
        {
            DiscardItem(target);
        }
    }

    void ConsumeItem(Consumable consumable)
    {
        itemCounter++;
        consumable.Consume();

        if (itemCounter >= itemsNeededToSpawnConvertedObject)
        {
            itemCounter -= itemsNeededToSpawnConvertedObject;
            SpawnConvertedItem();
        }
    }

    void SpawnConvertedItem()
    {
        if (convertedItemToSpawn != null)
        {
            var objectInstance = Instantiate(convertedItemToSpawn, convertedItemSpawnLocation.position, convertedItemSpawnLocation.rotation);
            SetBodyVelocity(objectInstance, convertedItemSpawnLocation.forward * spawnVelocity);
        }
    }

    List<Rigidbody> tempRigidbodies = new List<Rigidbody>();

    void DiscardItem(Collider target)
    {
        if (ColliderCanFit(target))
        {
            var destructible = target.GetComponentInParent<DestructibleObject>();
            if (destructible)
            {
                destructible.DestroyObject(eventSender.transform.position, eventSender.transform.up);
            }
            else if (teleportDiscardedItems)
            {
                var entity = target.GetComponentInParent<InteractableEntity>();
                if (entity)
                {
                    entity.Teleport(discardTeleportLocation.position, discardTeleportLocation.rotation);
                    SetBodyVelocity(entity.gameObject, discardTeleportLocation.forward * discardVelocity);
                    if (discardSmokeEffect)
                    {
                        discardSmokeEffect.Create(eventSender.transform.position);
                    }
                }
            }
        }
    }

    void SetBodyVelocity(GameObject target, Vector3 velocity)
    {
        target.GetComponentsInChildren(tempRigidbodies);
        foreach (var rig in tempRigidbodies)
        {
            var reBody = new ReBody(rig);
            reBody.velocity = Vector3.zero;
            reBody.angularVelocity = Vector3.zero;
            reBody.velocity = velocity;
        }
    }

    bool ColliderCanFit(Collider target)
    {
        var boxBounds = new BoxBounds(target);
        return ((Vector3)boxBounds.size).sqrMagnitude < maxObjectDiameter * maxObjectDiameter;
    }
}
