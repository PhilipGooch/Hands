using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIUtilities
{
    public static Vector3 GetHorizontalPositionForButton(Vector3 buttonSize, Transform center, int buttonIndex, int buttonCount, float spacing)
    {
        var buttonSizeVector = new Vector3(buttonSize.x, 0f, 0f);
        var spacingVector = new Vector3(spacing, 0f, 0f);
        var fullSize = buttonSizeVector * buttonCount + spacingVector * (buttonCount - 1);
        var startPos = -fullSize * 0.5f + buttonSizeVector * 0.5f;
        var localPos = startPos + buttonSizeVector * buttonIndex + spacingVector * (buttonIndex - 1);
        //place object on a curve
        localPos.z = localPos.x * localPos.x * -0.04f;
        //get consistent height variation
        localPos.y += Mathf.Cos(buttonIndex * Mathf.PI) * 0.5f;
        return center.position + center.rotation * localPos;
    }

    public static Vector3 GetVerticalPositionForButton(Vector3 buttonSize, Transform center, int buttonIndex, int buttonCount, float spacing)
    {
        var buttonSizeVector = new Vector3(0f, buttonSize.y, 0f);
        var spacingVector = new Vector3(0f, spacing, 0f);
        var fullSize = buttonSizeVector * buttonCount + spacingVector * (buttonCount - 1);
        var startPos = fullSize * 0.5f - buttonSizeVector * 0.5f;
        var localPos = startPos - buttonSizeVector * buttonIndex - spacingVector * (buttonIndex - 1);
        return center.position + center.rotation * localPos;
    }
}
