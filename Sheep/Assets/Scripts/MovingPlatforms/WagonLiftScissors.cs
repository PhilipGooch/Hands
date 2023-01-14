using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WagonLiftScissors : MonoBehaviour
{
    [SerializeField]
    Animator animator;
    [SerializeField]
    WagonLiftPlatform wagonLiftPlatform;

    int heightID = Animator.StringToHash("Height");

    void FixedUpdate()
    {
        animator?.SetFloat(heightID, wagonLiftPlatform.GetNormalizedLinearPosition());
    }
}
