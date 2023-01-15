using UnityEngine;
using VR.System;

public class Bootstrapper : MonoBehaviour
{
    public static Bootstrapper Instance;

    [SerializeField]
    Player player;
    [SerializeField]
    VRSystem vrSystem;

    void Awake()
    {
        Instance = this;
        vrSystem.Initialize();
        player.Initialize();
        transform.parent = null;
        DontDestroyOnLoad(this);
        vrSystem.Vibrate(2, 3, 999, 999, HandDirection.Right);
    }
}
