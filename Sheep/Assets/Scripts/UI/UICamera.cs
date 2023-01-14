using UnityEngine;
using System;
using System.Threading.Tasks;

public class UICamera : SingletonBehaviour<UICamera>
{
    new public Camera camera;
    public ColorOverlay uiColorOverlay;
    public ColorOverlay fullscreenFadeOverlay;

    int activeUIElements = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    public void AddActiveUIElement()
    {
        activeUIElements++;
        ToggleCameraIfNeeded();
    }

    public void RemoveActiveUIElement()
    {
        activeUIElements--;
        ToggleCameraIfNeeded();
    }

    public async void FadeInUI()
    {
        AddActiveUIElement();
        await uiColorOverlay.FadeIn();
    }

    public async void FadeOutUI()
    {
        await uiColorOverlay.FadeOut();
        RemoveActiveUIElement();
    }

    public void HideUI()
    {
        uiColorOverlay.Hide();
        RemoveActiveUIElement();
    }

    public async Task FadeToBlack()
    {
        AddActiveUIElement();
        await fullscreenFadeOverlay.FadeIn();
    }

    public async Task FadeFromBlack()
    {
        await fullscreenFadeOverlay.FadeOut();
        RemoveActiveUIElement();
    }

    void ToggleCameraIfNeeded()
    {
        if (activeUIElements > 0)
            camera.enabled = true;
        else
            camera.enabled = false;

        if (activeUIElements < 0)
        {
            Debug.LogError("Too many inactive UI camera elements: " + activeUIElements);
        }
    }
}
