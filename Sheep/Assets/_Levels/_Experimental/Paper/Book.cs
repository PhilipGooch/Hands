using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

[ExecuteInEditMode]
public class Book : MonoBehaviour
{
    public static Book Instance;

    public const float Epsilon = 1;

    [SerializeField]
    [Range(0, 89.999f)] // <--- experiment making epsilon smaller again. problems only seem to happen at theta = 90.
    float theta;

    public static float Theta;

    [SerializeField]
    bool refresh;
    [SerializeField]
    bool hideTransformGizmos;

    public BaseFold baseFold;
    public List<VFold> vFolds;

    public GameObject LeftSphere, RightSphere, CenterSphere;

    void OnEnable()
    {
        Instance = this;
        vFolds = new List<VFold>();
        Refresh();
    }

    public void Refresh()
    {
        ClearConsole();
        baseFold = FindObjectOfType<BaseFold>(); // <--- remove FindObjectOfType.
        vFolds.Clear();
        foreach (VFold vFold in FindObjectsOfType<VFold>())
        {
            vFolds.Add(vFold);
        }
        if (baseFold == null)
        {
            Debug.Log("Add BaseFold to scene.");
        }
    }

    void Update()
    {
        foreach (VFold vFold in vFolds)
        {
            if(vFold == null)
            {
                Debug.Log("Refresh.");
            }
        }
        if (hideTransformGizmos)
        {
            Tools.current = Tool.None;
        }
        if (refresh)
        {
            Refresh();
            refresh = false;
        }
        Theta = theta;
    }

    public static void ClearConsole()
    {
        Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
        Type type = assembly.GetType("UnityEditor.LogEntries");
        MethodInfo methodInfo = type.GetMethod("Clear");
        methodInfo.Invoke(new object(), null);
    }

    public void OnDestroy()
    {
        ClearConsole();
    }
}
