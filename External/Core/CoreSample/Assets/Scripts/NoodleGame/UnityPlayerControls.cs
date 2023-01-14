using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnityPlayerControls : Controls, IPlayerControls<InputDevice>
{
    public float[] controllerPitchJitterArray;

    public bool ContainsDevice(InputDevice device)
    {
        if (!devices.HasValue) return false;
        var list = devices.Value;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == device) return true;
        return false;
    }
    public void RemoveDevice(InputDevice device)
    {
        PlayerManager.reenableJoinActionFix = true;
        if (!devices.HasValue) return;
        var list = devices.Value;
        var newList = new List<InputDevice>();
        for (int i = 0; i < list.Count; i++)
            if (list[i] != device) newList.Add(list[i]);
        devices = newList.ToArray();
    }
    public void AddDevice(InputDevice device)
    {
        PlayerManager.reenableJoinActionFix = true;

        if (!devices.HasValue)
            devices = new InputDevice[] { device };
        else
        {

            var list = devices.Value;
            var newList = new List<InputDevice>(list);
            newList.Add(device);
            devices = newList.ToArray();
        }
    }
   
}
