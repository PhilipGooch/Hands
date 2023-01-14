using UnityEngine;
using System;

public class HorizontalNavigationButtons : MonoBehaviour
{
    [SerializeField]
    Button3D leftButton;
    [SerializeField]
    Button3D rightButton;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Reset()
    {
        ResetButton(leftButton);
        ResetButton(rightButton);
    }

    public void SetupLeftButton(Button3D leftMost, Action onClick)
    {
        SetupButton(leftButton, leftMost, onClick, ButtonPlacementSide.LEFT);
    }

    public void SetupRightButton(Button3D rightMost, Action onClick)
    {
        SetupButton(rightButton, rightMost, onClick, ButtonPlacementSide.RIGHT);
    }

    void SetupButton(Button3D target, Button3D other, Action onClick, ButtonPlacementSide side)
    {
        target.PlaceNextToAnotherButton(other, side);

        target.onClick += onClick;
        target.gameObject.SetActive(true);

    }

    void ResetButton(Button3D target)
    {
        target.ClearOnClick();
        target.gameObject.SetActive(false);
    }

}
