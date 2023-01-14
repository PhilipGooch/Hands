using UnityEngine;
using TMPro;
using System;

public class LabelVButton3D : UIElement
{
    [SerializeField]
    Button3D button;
    [SerializeField]
    UIText label;

    public void Initialize(string localizationKey, string btnText, Action onClick)
    {
        button.SetPrimaryText(btnText);
        button.onClick += onClick;
        label.SetText(localizationKey);
    }

    public void SetTexts(string labelLocalizationKey, string btnTextLocalizationKey)
    {
        if (!string.IsNullOrEmpty(labelLocalizationKey))
            label.SetText(labelLocalizationKey);

        if (!string.IsNullOrEmpty(btnTextLocalizationKey))
            button.SetPrimaryText(btnTextLocalizationKey);
    }

    public void SetParameterInText(string parameterName, string parameterValue)
    {
        button.SetParameterInPrimaryText(parameterName, parameterValue);
    }

    public LabelVButton3D Create(Vector3 position, string labelLocalizationKey, string btnTextLocalizationKey, Action onClick, Transform parent)
    {
        var labelVButtonInstance = Instantiate(this, position, parent.transform.rotation, parent);
        labelVButtonInstance.Initialize(labelLocalizationKey, btnTextLocalizationKey, onClick);
        return labelVButtonInstance;
    }
}
