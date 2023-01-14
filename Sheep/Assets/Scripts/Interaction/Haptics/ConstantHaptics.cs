using UnityEngine;

public class ConstantHaptics : HapticsBase
{
    [SerializeField]
    private float hapticsTimeInterval = 0.5f;
    [SerializeField]
    private float hapticsIntervalMulti = 1;
    public float HapticsIntervalMulti
    {
        get
        {
            return hapticsIntervalMulti;
        }
        set
        {
            hapticsIntervalMulti = value;
        }
    }

    private float timeSinceLastHaptics;

    private void Update()
    {
        if (timeSinceLastHaptics >= hapticsTimeInterval * HapticsIntervalMulti)
        {
            TryVibrate();
            timeSinceLastHaptics = 0;
        }
        timeSinceLastHaptics += Time.deltaTime;
    }
}
