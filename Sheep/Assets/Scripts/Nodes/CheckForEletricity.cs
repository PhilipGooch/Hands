using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CheckForEletricity : MonoBehaviour
{
    public List<CollisionEventsSender> collisionEventSenders;

    [NodeAPI("OnElectricityStateChanged")]
    public event Action<bool> onElectricityStateChanged;

    Dictionary<int, int> activeCollisions;

    private void Start()
    {
        activeCollisions = new Dictionary<int, int>();
        for (int i = 0; i < collisionEventSenders.Count; i++)
        {
            collisionEventSenders[i].onCollisionEnter += CollisionEnter;
            collisionEventSenders[i].onCollisionExit += CollisionExit;
        }

        onElectricityStateChanged?.Invoke(false);
    }

    void CollisionEnter(Collision collision)
    {
        InteractableEntity entity = collision.gameObject.GetComponent<InteractableEntity>();

        if (entity != null)
        {
            if (entity.physicalMaterial.TransfersElectricCurrent)
            {
                int id = collision.gameObject.GetInstanceID();

                if (activeCollisions.ContainsKey(id))
                    activeCollisions[id]++;
                else
                    activeCollisions.Add(id, 1);

                onElectricityStateChanged?.Invoke(CheckIfAllNodesAreTouchedBySameObj());
            }
        }
    }

    void CollisionExit(Collision collision)
    {
        InteractableEntity entity = collision.gameObject.GetComponent<InteractableEntity>();

        if (entity != null)
        {
            if (entity.physicalMaterial.TransfersElectricCurrent)
            {
                int id = collision.gameObject.GetInstanceID();

                if (activeCollisions.ContainsKey(id))
                {
                    activeCollisions[id]--;
                    if (activeCollisions[id] == 0)
                        activeCollisions.Remove(id);
                }

                onElectricityStateChanged?.Invoke(CheckIfAllNodesAreTouchedBySameObj());
            }
        }
    }

    bool CheckIfAllNodesAreTouchedBySameObj()
    {
        int maxCountOfSameObjs = 0;

        foreach (KeyValuePair<int, int> pair in activeCollisions)
        {
            if (pair.Value > maxCountOfSameObjs)
                maxCountOfSameObjs = pair.Value;
        }

        return maxCountOfSameObjs == collisionEventSenders.Count;
    }
}
