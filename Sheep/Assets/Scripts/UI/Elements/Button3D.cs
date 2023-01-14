using UnityEngine;
using System;
using NBG.Core;

enum ClickSound
{
    POSITIVE,
    NEGATIVE
}

public enum ButtonPlacementSide
{
    RIGHT = -1,
    LEFT = 1
}

public class Button3D : UIElement
{
    [SerializeField]
    ClickSound clickSound;
    [SerializeField]
    protected Material hoverMaterial;

    public event Action onClick;
    public event Action<Vector3> onClickPosition;

    public event Action<Vector3> onDrag;

    bool hoveredLastFrame = false;
    bool hovered = false;

    bool initialized;

    protected Material originalMaterial;

    [SerializeField]
    protected new MeshRenderer renderer;
    [SerializeField]
    new Collider collider;

    UIText mainTxt;

    protected UIText MainTxt
    {
        get
        {
            if (mainTxt == null)
                mainTxt = GetComponentInChildren<MainText>(true);

            return mainTxt;
        }
    }

    UIText secondaryTxt;
    protected UIText SecondaryTxt
    {
        get
        {
            if (secondaryTxt == null)
                secondaryTxt = GetComponentInChildren<SecondaryText>(true);

            return secondaryTxt;
        }
    }

    private void Awake()
    {
        Initialize();
    }

    public virtual void OnEnable()
    {
        hoveredLastFrame = false;
        hovered = false;
    }

    public virtual void OnDisable()
    {
        renderer.material = originalMaterial;
    }

    public void SetPrimaryText(string mainTextLocalizationKey)
    {
        MainTxt.SetText(mainTextLocalizationKey);
    }

    public void SetSecondaryText(string secondaryTextLocalizationKey)
    {
        SecondaryTxt.SetText(secondaryTextLocalizationKey);
    }

    public void SetParameterInPrimaryText(string parameterName, string parameterValue)
    {
        MainTxt.SetTextParameter(parameterName, parameterValue);
    }

    public void SetParameterInSecondaryText(string parameterName, string parameterValue)
    {
        SecondaryTxt.SetTextParameter(parameterName, parameterValue);
    }

    public override void SetInteractable(bool interactable, bool changeChildren = true)
    {
        base.SetInteractable(interactable, changeChildren);

        collider.gameObject.SetActive(interactable);
    }

    public virtual void HoverStart()
    {
        if (hoverMaterial != null)
        {
            renderer.material = hoverMaterial;
        }

    }

    public virtual void HoverEnd()
    {
        if (hoverMaterial != null)
        {
            renderer.material = originalMaterial;
        }

    }

    public virtual void Hover()
    {
        if (!interactable)
            return;


        hovered = true;

        if (!hoveredLastFrame)
        {
            HoverStart();
        }
        //onHover
    }

    public virtual void Click(Vector3 position)
    {
        if (!interactable)
            return;

        PlayClickSound();

        onClick?.Invoke();
        onClickPosition?.Invoke(position);
    }

    public void Drag(Vector3 position)
    {
        if (!interactable)
            return;

        onDrag?.Invoke(position);
    }

    public void ClearOnClick()
    {
        onClick = null;
        onClickPosition = null;
    }

    void PlayClickSound()
    {
        switch (clickSound)
        {
            case ClickSound.POSITIVE:
                AudioManager.instance.PlayUIClickPositive();
                break;

            case ClickSound.NEGATIVE:
                AudioManager.instance.PlayUIClickNegative();
                break;
        }
    }

    public BoxBounds GetBoxBounds()
    {
        return new BoxBounds(collider);
    }

    public Bounds GetBounds()
    {
        return collider.bounds;
    }

    const float kAdditionalZOffset = 0.7f;
    const float kAdditionalXOffset = 0.3f;

    public void PlaceNextToAnotherButton(Button3D nextTo, ButtonPlacementSide side)
    {
        var bounds = nextTo.GetBounds();
        var center = bounds.center;
        var extents = bounds.extents;
        var pos = new Vector3(
            center.x - extents.x - kAdditionalZOffset,
            transform.position.y,
            center.z + extents.z * (int)side + kAdditionalXOffset * (int)side
        );
        transform.position = pos;
    }

    void Update()
    {
        if (!interactable)
            return;

        if (hoveredLastFrame && !hovered)
        {
            HoverEnd();
        }
        hoveredLastFrame = hovered;
        hovered = false;
    }

    void Initialize()
    {
        if (!initialized)
        {
            initialized = true;

            if (renderer == null)
                renderer = GetComponentInChildren<MeshRenderer>();

            originalMaterial = renderer.sharedMaterial;
        }
    }


    #region Create

    public Button3D Create(Quaternion rotation, Transform parent)
    {
        var buttonInstance = Instantiate(this, Vector3.zero, rotation, parent);
        buttonInstance.Initialize();

        return buttonInstance;
    }

    public Button3D Create(Vector3 position, Transform parent)
    {
        var buttonInstance = Instantiate(this, position, parent.transform.rotation, parent);
        buttonInstance.Initialize();

        return buttonInstance;
    }

    public Button3D Create(Vector3 position, Action onClick, Transform parent)
    {
        var buttonInstance = Instantiate(this, position, parent.transform.rotation, parent);
        buttonInstance.Initialize();

        buttonInstance.onClick += onClick;

        return buttonInstance;
    }

    public Button3D Create(Vector3 position, string localizationKey, Action onClick, Transform parent)
    {

        var buttonInstance = Create(position, onClick, parent);

        MainText txt = buttonInstance.GetComponentInChildren<MainText>();
        if (txt != null)
            txt.SetText(localizationKey);

        return buttonInstance;
    }

    public Button3D Create(Vector3 position, string localizationKey, Transform parent)
    {

        var buttonInstance = Create(position, parent);

        MainText txt = buttonInstance.GetComponentInChildren<MainText>();
        if (txt != null)
            txt.SetText(localizationKey);

        return buttonInstance;
    }

    #endregion
}
