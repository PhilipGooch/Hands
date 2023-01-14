using UnityEngine;

[RequireComponent(typeof(HandTool))]
public class Carbine : MonoBehaviour
{
    [SerializeField]
    ConfigurableJoint joint;
    [HideInInspector]
    [SerializeField]
    HandTool handTool;

    bool clicking;
    float activation;

    Vector3 openRotation;
    Vector3 startRotation;

    private void OnValidate()
    {
        if (handTool == null)
            handTool = GetComponent<HandTool>();
    }

    private void Start()
    {
        startRotation = Vector3.zero;
        openRotation = new Vector3(34, 0, 0);

        handTool.OnActivationChange += ActivationChange;
    }

    void ActivationChange(float activation)
    {
        this.activation = Mathf.Clamp01(activation);
    }

    void FixedUpdate()
    {
        if (activation > 0)
        {
            clicking = true;
            joint.targetRotation = Quaternion.Lerp(Quaternion.Euler(startRotation), Quaternion.Euler(openRotation), activation);
        }
        else if (activation == 0 && clicking)
        {
            clicking = false;
            joint.targetRotation = Quaternion.Euler(startRotation);
        }
    }
}
