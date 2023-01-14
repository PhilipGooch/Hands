using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VR.System;

public class UnderwaterEffect : ScriptableRendererFeature
{
    [System.Serializable]
    public class EffectSettings
    {
        // we're free to put whatever we want here, public fields will be exposed in the inspector
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material underwaterEffectMaterial;
    }

    // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
    public EffectSettings settings = new EffectSettings();

    UnderwaterBlitPass effectPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.IsEnabled && Application.isPlaying)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.isActiveAndEnabled && camera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Base)
            {
                var colorTarget = renderer.cameraColorTarget;
                effectPass.Setup(colorTarget, settings, renderer);
                renderer.EnqueuePass(effectPass);
            }
        }
    }

    public override void Create()
    {
        effectPass = new UnderwaterBlitPass(settings.WhenToInsert);
    }
}

class UnderwaterBlitPass : ScriptableRenderPass
{
    RenderTargetIdentifier cameraColorTargetIdent;
    ScriptableRenderer renderer;
    Material materialInstance;
    UnderwaterEffect.EffectSettings settings;

    int blurSizeId = Shader.PropertyToID("_BlurSize");
    int firstId = Shader.PropertyToID("_TempOne");
    int secondId = Shader.PropertyToID("_TempTwo");
    int sourceId = Shader.PropertyToID("_MainTex");
    int eyeStateId = Shader.PropertyToID("_EyeState");
    int colorId = Shader.PropertyToID("_Color");
    int maxDistanceId = Shader.PropertyToID("_MaxDistance");
    int minDistanceId = Shader.PropertyToID("_MinDistance");

    public UnderwaterBlitPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }

    public void Setup(RenderTargetIdentifier cameraColorTargetIdent, UnderwaterEffect.EffectSettings settings, ScriptableRenderer renderer)
    {
        this.cameraColorTargetIdent = cameraColorTargetIdent;
        if (materialInstance == null)
        {
            materialInstance = new Material(settings.underwaterEffectMaterial);
        }
        this.settings = settings;
        this.renderer = renderer;
    }

    // called each frame before Execute, use it to set up things the pass will need
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // create a temporary render texture that matches the camera
        //cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
        cmd.GetTemporaryRT(firstId, cameraTextureDescriptor);
        cmd.GetTemporaryRT(secondId, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        
        bool vrEnabled = VRSystem.Instance != null && Player.Initialized;
        var player = Player.Instance;
        if (player != null && (player.RightEyeUnderwater || player.LeftEyeUnderwater))
        {
            var activeRT = RenderTexture.active;
            // fetch a command buffer to use
            CommandBuffer cmd = CommandBufferPool.Get("Underwater Post-Effect");

            var camera = renderingData.cameraData.camera;
            var additionalData = renderingData.cameraData.camera.GetUniversalAdditionalCameraData();

            cmd.SetGlobalVector(eyeStateId, new Vector2(player.LeftEyeUnderwater ? 1f : 0f, player.RightEyeUnderwater ? 1f : 0f));

            cmd.SetGlobalFloat(maxDistanceId, player.UnderwaterParameters.maxViewDistance);
            cmd.SetGlobalFloat(minDistanceId, player.UnderwaterParameters.minViewDistance);
            cmd.SetGlobalColor(colorId, player.UnderwaterParameters.underwaterColor);
            // HACK: we're setting the depth texture attachment as the depth texture, since we rendered the water into that texture
            //cmd.SetGlobalTexture(depthId, renderer.cameraDepthTarget);
            BlitWorkaround(cmd, renderingData.cameraData.renderer.cameraColorTarget, firstId, materialInstance, 1);

            int fromTexture = firstId;
            int toTexture = secondId;

            var blur = 0;
            while (blur < player.UnderwaterParameters.blurAmount)
            {
                int blurAmount = blur + 1;
                cmd.SetGlobalFloat(blurSizeId, blurAmount);
                BlitWorkaround(cmd, fromTexture, toTexture, materialInstance, 0);
                //Swap RTs
                var temp = fromTexture;
                fromTexture = toTexture;
                toTexture = temp;
                blur++;
            }

            cmd.SetGlobalFloat(blurSizeId, 0);
            BlitWorkaround(cmd, fromTexture, cameraColorTargetIdent, materialInstance, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    // DO NOT USE BLIT, IT DOES NOT WORK IN XR
    // https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.1/manual/renderer-features/how-to-fullscreen-blit-in-xr-spi.html
    // Do not use the cmd.Blit method in URP XR projects because that method has compatibility issues with the URP XR integration.
    // Using cmd.Blit might implicitly enable or disable XR shader keywords, which breaks XR SPI rendering.
    void BlitWorkaround(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier target, Material material, int pass)
    {
        cmd.SetGlobalTexture(sourceId, source);
        cmd.SetRenderTarget(new RenderTargetIdentifier(target, 0, CubemapFace.Unknown, -1), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
    }

    // called after Execute, use it to clean up anything allocated in Configure
    public override void FrameCleanup(CommandBuffer cmd)
    {
        //cmd.ReleaseTemporaryRT(tempTexture.id);
        cmd.ReleaseTemporaryRT(firstId);
        cmd.ReleaseTemporaryRT(secondId);
    }
}
