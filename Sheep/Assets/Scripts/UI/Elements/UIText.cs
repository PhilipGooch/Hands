using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using NBG.Core;

public class UIText : UIElement
{
    [HideInInspector]
    [SerializeField]
    TMP_Text tmpText;
    TMP_Text Text
    {
        get
        {
            if (tmpText == null)
                tmpText = GetComponentInChildren<TMP_Text>();

            if (tmpText == null)
                Debug.LogError("GameObject OR its children do not contain TMP_Text component", gameObject);

            return tmpText;
        }
    }

    [HideInInspector]
    [SerializeField]
    Localize textLocalization;
    Localize TextLocalization
    {
        get
        {
            if (textLocalization == null)
                textLocalization = GetComponentInChildren<Localize>();

            if (textLocalization == null)
                Debug.LogError("GameObject OR its children do not contain Localize component", gameObject);

            return textLocalization;
        }
    }

    [HideInInspector]
    [SerializeField]
    LocalizationParamsManager paramsLocalization;
    LocalizationParamsManager ParamsLocalization
    {
        get
        {
            if (paramsLocalization == null)
                paramsLocalization = GetComponentInChildren<LocalizationParamsManager>();

            if (paramsLocalization == null)
                Debug.LogError("GameObject OR its children do not contain LocalizationParamsManager component", gameObject);

            return paramsLocalization;
        }
    }

    private void OnValidate()
    {
        {
            if (paramsLocalization == null)
                paramsLocalization = GetComponentInChildren<LocalizationParamsManager>();
        }

        {
            if (textLocalization == null)
                textLocalization = GetComponentInChildren<Localize>();

            if (textLocalization == null)
                Debug.LogError($"GameObject {gameObject.GetFullPath()} OR its children do not contain Localize component", gameObject);
        }

        {
            if (tmpText == null)
                tmpText = GetComponentInChildren<TMP_Text>();

            if (tmpText == null)
                Debug.LogError($"GameObject {gameObject.GetFullPath()} OR its children do not contain TMP_Text component", gameObject);
        }
    }

    /// <summary>
    /// Sets localized text using term key OR sets direct value
    /// </summary>
    /// <param name="value"></param>
    public void SetText(string value, bool localized = true)
    {
        if (localized)
            TextLocalization.SetTerm(value);
        else
            Text.SetText(value);
    }

    public void SetTextParameter(string parameterKey, string parameterValue)
    {

        if (paramsLocalization == null)
            Debug.LogError("GameObject OR its children do not contain LocalizationParamsManager component", gameObject);
        else
            ParamsLocalization.SetParameterValue(parameterKey, parameterValue);
    }

    public void AdjustValueTextBoxSizeToLongestValue(string[] values)
    {
        Vector2 longest = Vector2.zero;
        for (int i = 0; i < values.Length; i++)
        {
            var size = GetSizeOfText(values[i]);
            if (size.x > longest.x)
                longest = size;
        }

        SetTextBoxSize(longest);
    }

    public void SetTextBoxSize(Vector2 size)
    {
        Text.rectTransform.sizeDelta = size;
    }

    public Vector2 GetSizeOfText(string text)
    {
        var size = Text.GetPreferredValues(text);
        return size;
    }

    public override void SetInteractable(bool interactable, bool changeChildren = true)
    {
        base.SetInteractable(interactable, changeChildren);

        Text.color = interactable ? originalColor : GameParameters.Instance.disabledUIElementsColor;
    }

    public UIText Create(string text, Transform parent)
    {
        var textInstance = Instantiate(this, parent.transform.position, parent.transform.rotation, parent);
        textInstance.SetText(text);
        return textInstance;
    }

    public void RegisterOnLocalizeCallback(UnityAction onLocalize)
    {
        TextLocalization.LocalizeEvent.AddListener(onLocalize);
    }

    public void UnregisterOnLocalizeCallback(UnityAction onLocalize)
    {
        TextLocalization.LocalizeEvent.RemoveListener(onLocalize);
    }
}
