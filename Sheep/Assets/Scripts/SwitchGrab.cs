using NBG.LogicGraph;
using UnityEngine;

public class SwitchGrab : MonoBehaviour
{
    [SerializeField]
    Rigidbody ungrabRigidbody;
    [SerializeField]
    Rigidbody grabRigidbody;

    [NodeAPI("SwitchGrab")]
    public void UngrabThis()
    {
        var hand = Player.Instance.GetHandThatIsGrabbingBody(ungrabRigidbody);
        hand?.InterceptGrab(grabRigidbody, grabRigidbody.position);

    }
}
