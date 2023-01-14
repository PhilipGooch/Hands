using NBG.LogicGraph;
using UnityEngine;

public class EnableComponent : MonoBehaviour
{
    public Behaviour target;

    [NodeAPI("SetActiveState")]
    public bool ActiveState
    {
        set
        {
            target.enabled = value;
        }
    }
}

