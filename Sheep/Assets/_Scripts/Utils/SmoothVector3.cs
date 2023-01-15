using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothVector3 
{
    public SmoothVector3(int window)
    {
        this.window = window;
    }
    public int window=10;
    Queue<Vector3> history = new Queue<Vector3>();
    //public Vector3 value;
    public Vector3 Enque(Vector3 vec)
    {
        history.Enqueue(vec);
        if (history.Count > window)
            history.Dequeue();
        var result = Vector3.zero;
        foreach (var h in history)
            result += h;
        return result / history.Count;
    }
    public void Reset()
    {
        history.Clear();

    }
}
