using NBG.LogicGraph;
using UnityEngine;

public class AnimatorTriggerNode : MonoBehaviour
{
    [SerializeField]
    Animator animator;
    [SerializeField]
    string animationTrigger;

    int triggerId;

    void Awake()
    {
        triggerId = Animator.StringToHash(animationTrigger);
    }

    [NodeAPI("TriggerAnimation")]
    public void TriggerAnimation()
    {
        if (animator)
        {
            animator.SetTrigger(triggerId);
        }
    }

    private void OnValidate()
    {
        if (!animator)
        {
            animator = GetComponent<Animator>();
        }
    }
}
