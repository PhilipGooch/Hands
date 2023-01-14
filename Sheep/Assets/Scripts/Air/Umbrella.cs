using Recoil;
using UnityEngine;

[RequireComponent(typeof(HandTool))]
public class Umbrella : MonoBehaviour
{
    [SerializeField]
    GameObject foldedUmbrella;
    [SerializeField]
    GameObject unfoldedUmbrella;
    [HideInInspector]
    [SerializeField]
    HandTool handTool;

    new Rigidbody rigidbody;
    ReBody reBody;

    bool folded = true;

    private void OnValidate()
    {
        if (handTool == null)
            handTool = GetComponent<HandTool>();
    }

    protected void Awake()
    {
        rigidbody = GetComponentInParent<Rigidbody>();
        handTool.OnActivationChange += ActivationChange;
    }

    private void Start()
    {
        reBody = new ReBody(rigidbody);
    }

    public void ActivationChange(float pressure)
    {
        if (pressure > 0)
        {
            ToggleUmbrella();
        }
    }

    void ToggleUmbrella()
    {
        folded = !folded;
        foldedUmbrella.SetActive(folded);
        unfoldedUmbrella.SetActive(!folded);
        UpdateHandInertiaTensor();
    }

    void UpdateHandInertiaTensor()
    {
        // Folding the umbrella changes the inertia tensor but the hand stores the initial tensor value
        // We need to update it in order to avoid jittering on water and similar places
        if (handTool.MainHand != null)
        {
            reBody.ResetInertiaTensor();
            handTool.MainHand.attachedTensor = reBody.inertiaTensor;
            if (handTool.TwoHanded)
            {
                handTool.MainHand.otherHand.attachedTensor = reBody.inertiaTensor;
            }
        }
    }
}
