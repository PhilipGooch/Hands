using UnityEngine;
using System;

public class Slider3D : UIElement
{
    public UIText label;

    [SerializeField]
    Button3D sliderIndicatorButton;
    [SerializeField]
    RectTransform sliderRect;
    [SerializeField]
    Button3D sliderBarButton;
    [SerializeField]
    MeshRenderer activeBar;
    [SerializeField]
    MeshRenderer inactiveBar;

    float sliderExtents;

    Action<float> onSliderValueChanged;

    int cutoffId = Shader.PropertyToID("_YCutoff");

    public void Initialize(string localizationKey, Action<float> onSliderValueChanged, float initialPosition)
    {
        var invertId = Shader.PropertyToID("_Invert");

        activeBar.material.SetFloat(invertId, 1);
        inactiveBar.material.SetFloat(invertId, 0);

        sliderIndicatorButton.onDrag += OnSliderChange;
        sliderBarButton.onClickPosition += OnSliderChange;

        sliderExtents = sliderRect.sizeDelta.x / 2f;

        this.label = GetComponentInChildren<UIText>();
        this.label.SetText(localizationKey);
        this.onSliderValueChanged = onSliderValueChanged;

        SetSliderPosition(initialPosition);

    }

    void OnSliderChange(Vector3 position)
    {
        var localPosition = sliderRect.InverseTransformPoint(position);
        var positionOnLine = Vector3.Project(localPosition, Vector3.right);
        positionOnLine = Vector3.ClampMagnitude(positionOnLine, sliderExtents);

        var leftCornerLocal = Vector3.right * -sliderExtents;
        var localDiff = positionOnLine - leftCornerLocal;
        var progress = localDiff.magnitude / (sliderExtents * 2f);
        SetSliderPosition(progress);
    }

    public void SetSliderPosition(float value)
    {
        activeBar.material.SetFloat(cutoffId, value);
        inactiveBar.material.SetFloat(cutoffId, 1 - value);

        var positionLocal = value * Vector3.right * sliderExtents * 2f - Vector3.right * sliderExtents;
        var position = sliderRect.TransformPoint(positionLocal);
        sliderIndicatorButton.transform.position = position;
        onSliderValueChanged?.Invoke(value);
    }

    public Slider3D Create(Vector3 position, string localizationKey, float initialPosition, Action<float> onSliderValueChanged, Transform parent)
    {
        var sliderInstance = Instantiate(this, position, parent.transform.rotation, parent);
        sliderInstance.Initialize(localizationKey, onSliderValueChanged, initialPosition);
        return sliderInstance;
    }
}
