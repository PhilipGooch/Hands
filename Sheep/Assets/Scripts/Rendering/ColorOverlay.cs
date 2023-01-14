using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Threading.Tasks;

public class ColorOverlay : Overlay
{
    [SerializeField]
    Color targetColor = Color.white;
    [SerializeField]
    float fadeSpeed = 0.5f;

    readonly int colorProperty = Shader.PropertyToID("_Color");
    readonly int stereoVPMatrixId = Shader.PropertyToID("unity_StereoMatrixVP");
    float currentAlpha = 0f;
    float targetAlpha = 1f;
    float originalFarClipPlane = 700f;

    Matrix4x4[] stereoIdentity = new Matrix4x4[2] { Matrix4x4.identity, Matrix4x4.identity };
    Matrix4x4[] stereoOriginal = new Matrix4x4[2];

    bool FadedOut => Mathf.Approximately(targetAlpha, 0f) && Mathf.Approximately(currentAlpha, 0f);
    bool FadedIn => Mathf.Approximately(targetAlpha, 1f) && Mathf.Approximately(currentAlpha, 1f);
    protected override string QuadName => "Color Overlay Quad";

    protected override void Awake()
    {
        base.Awake();
        originalFarClipPlane = targetCamera.farClipPlane;
        enabled = false;
    }

    protected void OnDisable()
    {
        if (fullscreenQuad != null)
        {
            fullscreenQuad.SetActive(false);
        }
        targetCamera.farClipPlane = originalFarClipPlane;
    }

    Matrix4x4 GetStereoVPMatrix(Camera camera, Camera.StereoscopicEye eye)
    {
        return camera.GetStereoViewMatrix(eye) * camera.GetStereoProjectionMatrix(eye);
    }

    public async Task FadeIn()
    {
        enabled = true;
        targetAlpha = 1f;
        await WaitForConditionAsync.Create(() => FadedIn);
    }

    public async Task FadeOut()
    {
        enabled = true;
        targetAlpha = 0;
        await WaitForConditionAsync.Create(() => FadedOut);
    }
    
    public void Hide()
    {
        enabled = false;
        targetAlpha = 0;
        currentAlpha = 0;
    }

    private void Update()
    {
        if (FadedOut)
        {
            enabled = false;
        }
        else if (FadedIn)
        {
        }
        else
        {
            bool fadingIn = targetAlpha == 1;
            int fadeSign = fadingIn ? 1 : -1;
            currentAlpha += (Time.deltaTime / fadeSpeed) * fadeSign;
            currentAlpha = Mathf.Clamp01(currentAlpha);
        }
    }

    private void LateUpdate()
    {
        UpdateQuad();
    }

    void UpdateQuad()
    {
        fullscreenQuad.SetActive(currentAlpha > 0f);
        if (fullscreenQuad.activeSelf)
        {
            Color finalColor = targetColor;
            finalColor.a = currentAlpha;
            materialInstance.SetColor(colorProperty, finalColor);
        }

        if (Mathf.Approximately(currentAlpha, 1f))
        {
            // Stop rendering everything behind the overlay
            targetCamera.farClipPlane = targetCamera.nearClipPlane + 1f;
        }
        else
        {
            targetCamera.farClipPlane = originalFarClipPlane;
        }
    }
}
