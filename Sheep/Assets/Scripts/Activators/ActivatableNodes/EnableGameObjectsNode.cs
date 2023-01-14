using NBG.LogicGraph;
using System.Collections.Generic;
using UnityEngine;

public class EnableGameObjectsNode : MonoBehaviour
{
    [SerializeField]
    List<GameObject> targets;

    [NodeAPI("SetActiveState")]
    public bool ActiveState
    {
        set
        {
            foreach (var target in targets)
            {
                target.SetActive(value);
            }
        }
    }
}
