using NBG.Core;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EmptySpaceChecker
{
    readonly Transform spawnPoint;

    public EmptySpaceChecker(Transform spawnPoint)
    {
        this.spawnPoint = spawnPoint;
    }

    public bool CheckIfFits(IReadOnlyList<BoxBounds> bounds)
    {
        bool canFit = true;

        foreach (var item in bounds)
        {
            if (Physics.CheckBox(spawnPoint.TransformPoint(item.center), item.extents, spawnPoint.rotation * item.rotation))
            {
                canFit = false;
                break;
            }
        }

        return canFit;
    }

    Collider[] overlapHits = new Collider[128];
    HashSet<Collider> collidersToSkip = new HashSet<Collider>();
    List<Collider> intermediate = new List<Collider>();
    public bool CheckIfFits(IReadOnlyList<BoxBounds> bounds, IReadOnlyList<GameObject> toSkip)
    {
        bool canFit = true;
        collidersToSkip.Clear();

        for (int i = 0; i < toSkip.Count; i++)
        {
            intermediate.Clear();
            toSkip[i].GetComponentsInChildren(intermediate);

            collidersToSkip.UnionWith(intermediate);
        }

        foreach (var item in bounds)
        {
            int hits = Physics.OverlapBoxNonAlloc(spawnPoint.TransformPoint(item.center), item.extents, overlapHits, spawnPoint.rotation * item.rotation);
            for (int i = 0; i < hits; i++)
            {
                if(!collidersToSkip.Contains(overlapHits[i]))
                {
                    canFit = false;
                    break;
                }
            }
        }

        return canFit;
    }
}
