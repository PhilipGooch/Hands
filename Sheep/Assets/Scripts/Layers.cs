using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum Layers
{
    All = -1,
    Default = 1 << 0,
    Pushable = 1 << 3,
    Water = 1 << 4,
    UI = 1 << 5,
    TeleportationGrid = 1 << 6,
    SheepHead = 1 << 8,
    SheepTail = 1 << 9,
    SheepSensor = 1 << 10,
    SheepPush = 1 << 11,
    Hand = 1 << 12,
    Wall = 1 << 13,
    Object = 1 << 14,
    // 15
    Projectile = 1 << 16,
    IgnoreWalls = 1 << 17,
    NoCollision = 1 << 19,
    Sheep = SheepHead | SheepTail,
    Walls = Default | Wall,
    Climbable = Walls | IgnoreWalls | Object | Default,
    KinematicCollisions =
       Default |
        //   SheepHead |
        SheepTail |
        Wall |
        Object |
        Projectile |
        IgnoreWalls,
}

public static class LayerUtils
{
    public static bool IsPartOfLayer(int layerToCheck, int layerMask)
    {
        return ((1 << layerToCheck) & layerMask) > 0;
    }

    public static bool IsPartOfLayer(int layerToCheck, Layers layerMask)
    {
        return IsPartOfLayer(layerToCheck, (int)layerMask);
    }

    public static int GetLayerNumber(int layerMask)
    {
        var number = 0;
        while(layerMask > 0)
        {
            layerMask /= 2;
            number++;
        }
        return number-1;
    }

    public static int GetLayerNumber(Layers layerMask)
    {
        return GetLayerNumber((int)layerMask);
    }
}
