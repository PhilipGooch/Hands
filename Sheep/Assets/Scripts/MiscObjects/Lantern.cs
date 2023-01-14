using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lantern : MonoBehaviour
{
    [SerializeField]
    SheepCheckpoint sheepCheckpoint;

    new SkinnedMeshRenderer renderer;
    void Start()
    {
        if (sheepCheckpoint != null)
            sheepCheckpoint.onCheckpointReached += LightTheLantern;
        renderer = GetComponent<SkinnedMeshRenderer>();
    }

    void LightTheLantern()
    {
        renderer.material.EnableKeyword("_EMISSION");
    }
}
