using System.Collections.Generic;
using UnityEngine;

public class ChainDestruction : MonoBehaviour
{
    [SerializeField]
    DestructibleObject otherDestructible;

    [Tooltip("Destroy listed objects when assigned destructible gets destroyed")]
    [SerializeField]
    List<DestructibleObject> destroyOnOtherDestruction;

    private void Start()
    {
        otherDestructible.onDestroyed += OnDestroyed;
    }

    private void OnDestroy()
    {
        otherDestructible.onDestroyed -= OnDestroyed;
    }

    private void OnDestroyed()
    {
        foreach (var item in destroyOnOtherDestruction)
        {
            item.DestroyObject(item.transform.position, Vector3.zero);
        }

        destroyOnOtherDestruction.Clear();
    }
}
