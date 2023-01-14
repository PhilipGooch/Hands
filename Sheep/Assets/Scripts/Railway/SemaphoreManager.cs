using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SemaphoreManager : MonoBehaviour
{
    private List<Semaphore> semaphores;

    [SerializeField]
    private float raiseDuration;
    [SerializeField]
    private float lowerDuration;

    private Action<bool> callback;
    private int bariersMoved;

    private void Start()
    {
        semaphores = GetComponentsInChildren<Semaphore>().ToList();
    }

    public void Lower(Action<bool> onLower = null)
    {
        callback = onLower;
        bariersMoved = 0;
        for (int i = 0; i < semaphores.Count; i++)
        {
            semaphores[i].Lower(lowerDuration, RotationCallback);
        }
    }

    public void Raise(Action<bool> onRaise = null)
    {
        callback = onRaise;
        bariersMoved = 0;

        for (int i = 0; i < semaphores.Count; i++)
        {
            semaphores[i].Raise(raiseDuration, RotationCallback);
        }
    }

    private void RotationCallback(bool interupted)
    {
        bariersMoved++;
        if (interupted)
        {
            callback?.Invoke(false);
        }
        else if (bariersMoved == semaphores.Count)
        {
            callback?.Invoke(true);
        }
    }
}
