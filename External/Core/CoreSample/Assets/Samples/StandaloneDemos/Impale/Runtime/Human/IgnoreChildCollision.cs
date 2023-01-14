using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class IgnoreChildCollision : MonoBehaviour
    {
        private void OnEnable()
        {
            var collidersToIgnore = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < collidersToIgnore.Length; i++)
                for (int j = i + 1; j < collidersToIgnore.Length; j++)
                    Physics.IgnoreCollision(collidersToIgnore[j], collidersToIgnore[i], true);
        }
    }
}
