using NBG.LogicGraph;
using UnityEngine;

public class ElectricCar : MonoBehaviour, IGrabNotifications
{
    public GameObject alarm;
    public GameObject radio;

    public MeshRenderer lights;
    public Material lightsOnMat;
    public Material lightsOffMat;

    private void Start()
    {
        SetAlarmState(false);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        SetAlarmState(true);
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            SetAlarmState(false);
        }
    }

    private void SetAlarmState(bool state)
    {
        alarm.gameObject.SetActive(state);
    }

    [NodeAPI("UpdatePowerState")]
    public void UpdatePowerState(bool power)
    {
        if (power)
        {
            lights.material = lightsOnMat;
        }
        else
        {
            lights.material = lightsOffMat;
        }

        radio.SetActive(power);
    }
}
