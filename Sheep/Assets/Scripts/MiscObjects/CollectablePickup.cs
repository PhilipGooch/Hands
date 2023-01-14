using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectablePickup : MonoBehaviour
{
    List<GameObject> sheep = new List<GameObject>();
    int sheepLayer;
    void Start()
    {
        sheepLayer = LayerMask.NameToLayer("SheepHead");
    }


    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == sheepLayer)
        {
            AudioManager.instance.PlayCollect();
            Destroy(gameObject);
        }
    }
}