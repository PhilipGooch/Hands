using I2.Loc;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Switch3D : UIElement
{
    [SerializeField]
    UIText label;
    [SerializeField]
    Button3D rightButton;
    [SerializeField]
    Button3D leftButton;
    [SerializeField]
    UIText valueText;
    [SerializeField]
    Image additionalImage;

    string[] values;
    Action<int> onSwitchStateChanged;

    int currentState = 0;
    int totalValuesCount;

    bool localized = true;

    Vector2 originalLabelSize = Vector2.zero;

    public void Initialize(string localizationKey, int initialState, string[] values, Action<int> onSwitchStateChanged, Sprite additionalSprite = null, bool localizedValues = true)
    {
        this.values = values;
        this.localized = localizedValues;
        this.onSwitchStateChanged = onSwitchStateChanged;

        var labelRect = this.label.GetComponent<RectTransform>();

        originalLabelSize = labelRect.sizeDelta;

        totalValuesCount = values.Length;

        this.label.SetText(localizationKey);
        this.label.RegisterOnLocalizeCallback(this.ResizeValueText);

        if (additionalSprite != null)
        {
            additionalImage.sprite = additionalSprite;
            additionalImage.gameObject.SetActive(true);
            labelRect.sizeDelta = new Vector2(originalLabelSize.x - additionalImage.GetComponent<RectTransform>().sizeDelta.x - 0.1f, originalLabelSize.y);
        }
        else
        {
            additionalImage.gameObject.SetActive(false);
            labelRect.sizeDelta = originalLabelSize;
        }

        ResizeValueText();

        leftButton.onClick += () => OnStateChanged(-1);
        rightButton.onClick += () => OnStateChanged(1);

        OnStateChanged(initialState);
    }

    string[] KeysToText(string[] valueLocalizationKeys)
    {
        string[] texts = new string[valueLocalizationKeys.Length];
        for (int i = 0; i < valueLocalizationKeys.Length; i++)
        {
            texts[i] = LocalizationManager.GetTranslation(valueLocalizationKeys[i]);
        }
        return texts;
    }

    void OnStateChanged(int change)
    {
        currentState += change;

        if (currentState >= totalValuesCount)
            currentState = 0;
        else if (currentState < 0)
            currentState = totalValuesCount - 1;

        valueText.SetText(values[currentState], localized);
        onSwitchStateChanged?.Invoke(currentState);
    }

    void ResizeValueText()
    {
        if (localized)
            valueText.AdjustValueTextBoxSizeToLongestValue(KeysToText(values));
        else
            valueText.AdjustValueTextBoxSizeToLongestValue(values);
    }

    public Switch3D Create(Vector3 position, string label, int initialState, Action<int> onSwitchValueChanged, string[] values, Transform parent, Sprite additionalSprite = null, bool localizedValues = true)
    {
        var switchInstance = Instantiate(this, position, parent.transform.rotation, parent);
        switchInstance.Initialize(label, initialState, values, onSwitchValueChanged, additionalSprite, localizedValues);
        return switchInstance;
    }
}
