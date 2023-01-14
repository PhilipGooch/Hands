using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorUtilities : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Randomizes a float value with the name CycleOffset on the animator")]
    bool randomizeCycleOffset = true;

    int offsetId = Animator.StringToHash("CycleOffset");
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetFloat(offsetId, Random.value);
    }
}
