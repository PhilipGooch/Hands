using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class SheepPostEffects : MonoBehaviour
{
    [System.Serializable]
    public class EffectSettings
    {
        // we're free to put whatever we want here, public fields will be exposed in the inspector
        public bool IsEnabled = true;
        public bool tonemappingEnabled = true;
        public bool colorGradingEnabled = true;
        public Texture2D defaultLUT;
    }

    [SerializeField]
    EffectSettings settings = new EffectSettings();
    int tonemapperGammaId = Shader.PropertyToID("_SheepTonemapperGamma");
    int lutTextureId = Shader.PropertyToID("_SheepLUT");
    int lutContributionId = Shader.PropertyToID("_LUTContribution");

    private void LateUpdate()
    {
        UpdateEffects();
    }

    void UpdateEffects()
    {
        var tonemappingGamma = 1.0f;
        if (settings.IsEnabled && settings.tonemappingEnabled)
        {
            var data = GetPostEffectData<TonemappingData>();
            if (data)
            {
                tonemappingGamma = data.Gamma;
            }
        }
        Shader.SetGlobalFloat(tonemapperGammaId, tonemappingGamma);

        var lutTexture = settings.defaultLUT;
        var lutContribution = 0f;
        if (settings.IsEnabled && settings.colorGradingEnabled)
        {
            var data = GetPostEffectData<ColorGradingData>();
            if (data)
            {
                lutTexture = data.LUT;
                lutContribution = data.Contribution;
            }
        }

        Shader.SetGlobalTexture(lutTextureId, lutTexture);
        Shader.SetGlobalFloat(lutContributionId, lutContribution);
    }

    T GetPostEffectData<T>() where T : SingletonBehaviour<T>
    {
        T result = null;
        if (Application.isPlaying)
        {
            result = SingletonBehaviour<T>.Instance;
        }
        else
        {
#if UNITY_EDITOR
            result = FindObjectOfType<T>();
#endif
        }

        return result;
    }
}
