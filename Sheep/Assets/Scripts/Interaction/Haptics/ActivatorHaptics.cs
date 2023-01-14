using UnityEngine;

public class ActivatorHaptics : HapticsBase
{
    [SerializeField]
    int clicksFromFullActivation = 10;

    //ObjectActivator activator;

    float lastValue;
    float deltaWithoutVibration;
    float deltaToVibrate;

    protected override void Start()
    {
        base.Start();

        active = true;

        //activator = GetComponent<ObjectActivator>();

        deltaToVibrate = 1f / clicksFromFullActivation;
        //lastValue = activator.ActivationAmount;
    }

    void FixedUpdate()
    {
        //deltaWithoutVibration += Mathf.Abs(lastValue - activator.ActivationAmount);
        //lastValue = activator.ActivationAmount;
        if (deltaWithoutVibration > deltaToVibrate)
        {
            deltaWithoutVibration -= deltaToVibrate;
            TryVibrate();
        }
    }
}
