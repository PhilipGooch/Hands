#if NBG_ENABLE_EVENT_ORDER_CHECKER
using UnityEngine;

public class UnityEventOrderChecker : MonoBehaviour
{
    const string _prefix = "[UnityEventOrderChecker] ";

    static UnityEventOrderChecker() => Debug.Log(_prefix + "Static Constructor");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Subs() => Debug.Log(_prefix + "SubsystemRegistration");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)] static void AfterAsm() => Debug.Log(_prefix + "AfterAssembliesLoaded");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)] static void BeforeSlash() => Debug.Log(_prefix + "BeforeSplashScreen");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void BeforeScene() => Debug.Log(_prefix + "BeforeSceneLoad");
    private void Awake() => Debug.Log(_prefix + "Awake");
    private void OnEnable() => Debug.Log(_prefix + "OnEnable");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void AfterScene() => Debug.Log(_prefix + "AfterSceneLoad");
    [RuntimeInitializeOnLoadMethod] static void DefaultLog() => Debug.Log(_prefix + "RuntimeInitialize default");
    void Start() => Debug.Log("Start");
}
#endif
