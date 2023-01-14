using UnityEngine;
using NBG.LogicGraph;
using System;
using System.Collections.Generic;

public class ProximityActivator : MonoBehaviour
{
    [NodeAPI("On activation")]
    public event Action onActivation;

    [NodeAPI("On deactivation")]
    public event Action onDeactivation;

    bool activated;

    [NodeAPI("Is activated")]
    public bool IsActivated => activated;





    [SerializeField]
    List<GameObject> activatingObjects;

    int activatingObjectsInProximity = 0;


    bool IsActivatingObjectValid(GameObject activatingObject)
    {
        return activatingObjects.Contains(activatingObject);
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject activatingObject = other.gameObject;
        if (IsActivatingObjectValid(activatingObject))
        {
            activatingObjectsInProximity++;
            if (activatingObjectsInProximity == 1)
            {
                activated = true;
                onActivation?.Invoke();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        GameObject activatingObject = other.gameObject;
        if (IsActivatingObjectValid(activatingObject))
        {
            activatingObjectsInProximity--;
            if (activatingObjectsInProximity == 0)
            {
                activated = false;
                onDeactivation?.Invoke();
            }
        }
    }
}
