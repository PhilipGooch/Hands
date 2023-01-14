using NBG.LogicGraph;
using UnityEngine;

[RequireComponent(typeof(HoleSocket))]
public class ActiveSocketNode : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    HoleSocket socket;

    [SerializeField]
    float power;

    [NodeAPI("Power")]
    public float Power
    {
        set
        {
            power = value;
            socket.Power = power;
        }
    }
    private void Start()
    {
        socket.Power = power;
    }
    private void OnValidate()
    {
        if (socket == null)
        {
            socket = GetComponent<HoleSocket>();
        }
    }
}
