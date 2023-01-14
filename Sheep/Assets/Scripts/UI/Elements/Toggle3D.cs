using UnityEngine;
using System;

public class Toggle3D : UIElement
{

    [SerializeField]
    Material onMaterial;
    [SerializeField]
    Material offMaterial;
    [SerializeField]
    UIText label;

    [SerializeField]
    Button3D switchBarButton;
    [SerializeField]
    MeshRenderer switchBarRenderer;
    [SerializeField]
    RectTransform switchBarRect;

    [SerializeField]
    RectTransform stateIndicator;

    float sliderExtents;

    bool currentValue = false;
    Action<bool> onToggleStateChanged;


    public void Initialize(string localizationKey, bool value, Action<bool> onToggleStateChanged)
    {
        this.onToggleStateChanged = onToggleStateChanged;
        currentValue = value;
        switchBarButton.onClick += OnToggle;

        label.SetText(localizationKey);

        sliderExtents = switchBarRect.sizeDelta.x / 2f;

        RepaintToggle();
    }

    public void SetValue(bool newValue)
    {
        currentValue = newValue;
        RepaintToggle();
    }

    void OnToggle()
    {
        currentValue = !currentValue;
        onToggleStateChanged?.Invoke(currentValue);
        RepaintToggle();
    }

    void RepaintToggle()
    {
        if (currentValue)
        {
            switchBarRenderer.material = onMaterial;
            SetIndicatorPosition(1);

        }
        else
        {
            switchBarRenderer.material = offMaterial;
            SetIndicatorPosition(0);
        }
    }

    void SetIndicatorPosition(int progress)
    {
        var positionLocal = progress * Vector3.right * sliderExtents * 2f - Vector3.right * sliderExtents;
        var position = switchBarRect.TransformPoint(positionLocal);
        stateIndicator.position = position;
    }

    public Toggle3D Create(Vector3 position, string localizationKey, bool value, Action<bool> onToggle, Transform parent)
    {
        var toggleButtonInstance = Instantiate(this, position, parent.transform.rotation, parent);
        toggleButtonInstance.Initialize(localizationKey, value, onToggle);
        return toggleButtonInstance;
    }
}
