using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Attenuation)), CanEditMultipleObjects]
public class AttenuationEditor : Editor
{
    public enum BusType
    {
        EffectClose = 1,
        EffectMedium = 2,
        EffectFar = 3,
        AmbienceClose = 11,
        AmbienceMedium = 12,
        AmbienceFar = 13,
        Custom = 100
    }


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Apply")) 
        foreach (Attenuation target in targets)
        {
            var source = target.GetComponent<AudioSource>();
            if (source != null)
                target.Apply(source);
        }
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        if (GUILayout.Button("AmbienceClose")) foreach (Attenuation target in targets) ApplyPreset(target,BusType.AmbienceClose);
        if (GUILayout.Button("AmbienceMedium")) foreach (Attenuation target in targets) ApplyPreset(target, BusType.AmbienceMedium);
        if (GUILayout.Button("AmbienceFar")) foreach (Attenuation target in targets) ApplyPreset(target, BusType.AmbienceFar);
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        if (GUILayout.Button("EffectsClose")) foreach (Attenuation target in targets) ApplyPreset(target, BusType.EffectClose);
        if (GUILayout.Button("EffectMedium")) foreach (Attenuation target in targets) ApplyPreset(target, BusType.EffectMedium);
        if (GUILayout.Button("EffectFar")) foreach (Attenuation target in targets) ApplyPreset(target, BusType.EffectFar);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

    }

    public void ApplyPreset(Attenuation target, BusType type)
    {
        switch (type)
        {
            case BusType.EffectClose:
                target.maxDistance = 30;
                target.falloffStart = 1;
                target.falloffPower = .4f;
                target.lpStart = 1;
                target.lpPower = .5f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.EffectMedium:
                target.maxDistance = 50;
                target.falloffStart = 1;
                target.falloffPower = .5f;
                target.lpStart = 2;
                target.lpPower = .6f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.EffectFar:
                target.maxDistance = 100;
                target.falloffStart = 2;
                target.falloffPower = .8f;
                target.lpStart = 4;
                target.lpPower = .8f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.AmbienceClose:
                target.maxDistance = 30;
                target.falloffStart = 1;
                target.falloffPower = .5f;
                target.lpStart = 2;
                target.lpPower = .5f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.AmbienceMedium:
                target.maxDistance = 50;
                target.falloffStart = 3;
                target.falloffPower = .6f;
                target.lpStart = 3;
                target.lpPower = .8f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.AmbienceFar:
                target.maxDistance = 100;
                target.falloffStart = 5;
                target.falloffPower = .7f;
                target.lpStart = 5;
                target.lpPower = .8f;
                //target.spreadNear = 0.5f;
                //target.spreadFar = 0;
                //target.spatialNear = 0.5f;
                //target.spatialFar = 1;
                break;
            case BusType.Custom:
                break;
            default:
                break;
        }
        var source = target.GetComponent<AudioSource>();
        if (source != null)
            target.Apply(source);
    }
}