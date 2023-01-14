using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    [SerializeField]
    Collider thisCollider;
    [SerializeField]
    List<Collider> toIgnore;

    void Start()
    {
        foreach (var collider in toIgnore)
        {
            Physics.IgnoreCollision(collider, thisCollider);
        }
    }
}
