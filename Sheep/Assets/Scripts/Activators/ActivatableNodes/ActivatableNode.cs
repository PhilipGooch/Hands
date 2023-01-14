using NBG.LogicGraph;
using UnityEngine;

public class ActivatableNode : MonoBehaviour
{
    [SerializeField]
    private float startActivation = 0;

    private float activationValue;
    public float ActivationValue
    {
        get
        {
            return activationValue;
        }
        protected set
        {
            activationValue = Mathf.Clamp01(value);
        }
    }

    [NodeAPI("SetActivationValue")]
    public void SetActivationValue(float activationValue)
    {
        ActivationValue = activationValue;
    }

    protected virtual void Awake()
    {
        ActivationValue = startActivation;
    }
}
