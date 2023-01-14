using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderwaterOverlay : Overlay
{
    [SerializeField]
    [Range(0f,1f)]
    float maxAlpha = 0.25f;
    readonly int colorProperty = Shader.PropertyToID("_Color");
    readonly int eyeStateId = Shader.PropertyToID("_EyeState");

    protected override string QuadName => "Underwater Overlay Quad";

    private void LateUpdate()
    {
        UpdateQuad();
    }

    [System.Diagnostics.Conditional("OCULUSVR")]
    void UpdateQuad()
    {
        var player = Player.Instance;
        if (player != null && (player.RightEyeUnderwater || player.LeftEyeUnderwater))
        {
            var color = player.UnderwaterParameters.underwaterColor;
            color.a = Mathf.Min(color.a, maxAlpha);
            Shader.SetGlobalColor(colorProperty, color);
            Shader.SetGlobalVector(eyeStateId, new Vector2(player.LeftEyeUnderwater ? 1f : 0f, player.RightEyeUnderwater ? 1f : 0f));
            fullscreenQuad.SetActive(true);
        }
        else
        {
            fullscreenQuad.SetActive(false);
        }
    }
}
