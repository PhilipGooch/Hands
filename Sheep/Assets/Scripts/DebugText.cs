using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    public TMPro.TMP_Text text;
    static DebugText instance;
    private void Awake()
    {
        instance = this;
    }

    public static void Log(string txt)
    {
        if (instance != null)
            instance.text.text = txt;
    }

}
