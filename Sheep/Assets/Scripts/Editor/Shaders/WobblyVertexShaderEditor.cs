using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor;

internal class WobblyVertexShaderEditor : BaseShaderGUI
{
    // Properties
    private SimpleLitGUI.SimpleLitProperties litProperties;
    MaterialProperty wobbleSpeedProperty;
    MaterialProperty wobbleStrengthProperty;
    MaterialProperty wobbleDensityProperty;
    MaterialProperty bitangentOffsetProperty;
    MaterialProperty wobbleDirectionProperty;
    MaterialProperty recalculateNormalProperty;


    // collect properties from the material properties
    public override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);
        litProperties = new SimpleLitGUI.SimpleLitProperties(properties);
        wobbleSpeedProperty = FindProperty("_WobbleSpeed", properties, true);
        wobbleStrengthProperty = FindProperty("_WobbleStrength", properties, true);
        wobbleDensityProperty = FindProperty("_WobbleDensity", properties, true);
        bitangentOffsetProperty = FindProperty("_BitangentOffset", properties, true);
        wobbleDirectionProperty = FindProperty("_WobbleDirection", properties, true);
        recalculateNormalProperty = FindProperty("_RecalculateNormals", properties, true);
    }

    // material changed check
    public override void MaterialChanged(Material material)
    {
        if (material == null)
            throw new ArgumentNullException("material");
        SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
        if (recalculateNormalProperty.floatValue > 0)
        {
            material.EnableKeyword("_RECALCULATE_NORMALS");
        }
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
        base.DrawSurfaceOptions(material);
        bool needNormalRecalculation = recalculateNormalProperty.floatValue > 0;
        needNormalRecalculation = EditorGUILayout.Toggle("Recalculate Normals", needNormalRecalculation);
        recalculateNormalProperty.floatValue = needNormalRecalculation ? 1 : 0;
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in blendModeProp.targets)
                MaterialChanged((Material)obj);
        }
    }

    // material main surface inputs
    public override void DrawSurfaceInputs(Material material)
    {
        base.DrawSurfaceInputs(material);
        SimpleLitGUI.Inputs(litProperties, materialEditor, material);
        materialEditor.FloatProperty(wobbleSpeedProperty, "Wobble Speed");
        materialEditor.FloatProperty(wobbleStrengthProperty, "Wobble Strength");
        materialEditor.FloatProperty(wobbleDensityProperty, "Wobble Density");
        materialEditor.FloatProperty(bitangentOffsetProperty, "Bitangent Offset");
        materialEditor.VectorProperty(wobbleDirectionProperty, "Wobble Direction");


        DrawEmissionProperties(material, true);
        DrawTileOffset(materialEditor, baseMapProp);
    }

    // material main advanced options
    public override void DrawAdvancedOptions(Material material)
    {
        SimpleLitGUI.Advanced(litProperties);
        base.DrawAdvancedOptions(material);
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

        MaterialChanged(material);
    }
}

