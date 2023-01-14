using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor;

internal class PainterlyShaderEditor : BaseShaderGUI
{
    // Properties
    private LitGUI.LitProperties litProperties;

    MaterialProperty stencilComp;
    MaterialProperty stencilRef;
    MaterialProperty stencilPass;

    const string noiseKeyword = "_NOISE_ON";

    // collect properties from the material properties
    public override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);
        litProperties = new LitGUI.LitProperties(properties);
        stencilComp = FindProperty("_StencilComp", properties, false);
        stencilRef = FindProperty("_StencilMask", properties, false);
        stencilPass = FindProperty("_StencilPass", properties, false);
    }

    // material changed check
    public override void ValidateMaterial(Material material)
    {
        if (material == null)
            throw new ArgumentNullException("material");

        SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
        material.DisableKeyword(noiseKeyword);
        material.DisableKeyword("_NORMALMAP");
        material.DisableKeyword("_SPECULAR_COLOR");

        // Offset metallic materials for better batching
        int batchOffset = 0;
        if (material.GetTexture("_MetallicGlossMap") != null)
        {
            batchOffset = 1;
        }
        material.renderQueue += batchOffset;
    }

    // material main surface options
    public override void DrawSurfaceOptions(Material material)
    {
        if (material == null)
            throw new ArgumentNullException("material");

        // Use default labelWidth
        EditorGUIUtility.labelWidth = 0f;

        // Detect any changes to the material
        EditorGUI.BeginChangeCheck();
        if (litProperties.workflowMode != null)
        {
            DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, Enum.GetNames(typeof(LitGUI.WorkflowMode)));
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in blendModeProp.targets)
                ValidateMaterial((Material)obj);
        }
        base.DrawSurfaceOptions(material);
        material.doubleSidedGI = EditorGUILayout.Toggle("Double Sided GI", material.doubleSidedGI);
    }

    // material main surface inputs
    public override void DrawSurfaceInputs(Material material)
    {
        base.DrawSurfaceInputs(material);
        LitGUI.Inputs(litProperties, materialEditor, material);
        DrawEmissionProperties(material, true);
        DrawTileOffset(materialEditor, baseMapProp);
    }

    // material main advanced options
    public override void DrawAdvancedOptions(Material material)
    {
        if (litProperties.reflections != null && litProperties.highlights != null)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
            materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            if (EditorGUI.EndChangeCheck())
            {
                ValidateMaterial(material);
            }
        }

        base.DrawAdvancedOptions(material);
        DrawStencilSettings(material);
    }

    void DrawStencilSettings(Material material)
    {
        stencilComp.floatValue = (int)(CompareFunction)EditorGUILayout.EnumPopup("Stencil Comparison", (CompareFunction)stencilComp.floatValue);
        var stencilEnabled = (int)stencilComp.floatValue > 0;
        
        if (stencilEnabled)
        {
            stencilRef.floatValue = EditorGUILayout.IntField("Stencil Mask", (int)stencilRef.floatValue);
            stencilPass.floatValue = (int)(StencilOp)EditorGUILayout.EnumPopup("Stencil Pass", (StencilOp)stencilPass.floatValue);
        }
        else
        {
            stencilRef.floatValue = 0f;
            stencilPass.floatValue = 0f;
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        if (material == null)
            throw new ArgumentNullException("material");

        // _Emission property is lost after assigning Standard shader to the material
        // thus transfer it before assigning the new shader
        if (material.HasProperty("_Emission"))
        {
            material.SetColor("_EmissionColor", material.GetColor("_Emission"));
        }

        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
        {
            SetupMaterialBlendMode(material);
            return;
        }

        SurfaceType surfaceType = SurfaceType.Opaque;
        BlendMode blendMode = BlendMode.Alpha;
        if (oldShader.name.Contains("/Transparent/Cutout/"))
        {
            surfaceType = SurfaceType.Opaque;
            material.SetFloat("_AlphaClip", 1);
        }
        else if (oldShader.name.Contains("/Transparent/"))
        {
            // NOTE: legacy shaders did not provide physically based transparency
            // therefore Fade mode
            surfaceType = SurfaceType.Transparent;
            blendMode = BlendMode.Alpha;
        }
        material.SetFloat("_Surface", (float)surfaceType);
        material.SetFloat("_Blend", (float)blendMode);

        if (oldShader.name.Equals("Standard (Specular setup)"))
        {
            material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
            Texture texture = material.GetTexture("_SpecGlossMap");
            if (texture != null)
                material.SetTexture("_MetallicSpecGlossMap", texture);
        }
        else
        {
            material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
            Texture texture = material.GetTexture("_MetallicGlossMap");
            if (texture != null)
                material.SetTexture("_MetallicSpecGlossMap", texture);
        }

        ValidateMaterial(material);
    }
}

