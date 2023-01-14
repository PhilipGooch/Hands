/*
 * This is a modified copy of "ParticlesSimpleLitShader.cs" from URP Package
 * It's modified to go in pair with the "NBGParticlesSimpleLit.shader"
 */

using System;
using System.Collections.Generic;
using UnityEngine;

// EDIT by NBG START: Added usings below
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
// EDIT by NBG END

internal class ParticleNBGSimpleLitShaderEditor : BaseShaderGUI
{
    // Properties
    private SimpleLitGUI.SimpleLitProperties shadingModelProperties;
    private ParticleGUI.ParticleProperties particleProps;

    // EDIT by NBG START: Added additional particle shader properties to expose
    private MaterialProperty zTest;
    // EDIT by NBG START END

    // List of renderers using this material in the scene, used for validating vertex streams
    List<ParticleSystemRenderer> m_RenderersUsingThisMaterial = new List<ParticleSystemRenderer>();

    public override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);
        shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);
        particleProps = new ParticleGUI.ParticleProperties(properties);

        // EDIT by NBG START: Finding additional expose properties
        zTest = FindProperty("_ZTest", properties, true);
        // EDIT by NBG END
    }

    public override void MaterialChanged(Material material)
    {
        if (material == null)
            throw new ArgumentNullException("material");

        SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
    }

    public override void DrawSurfaceOptions(Material material)
    {
        // Detect any changes to the material
        EditorGUI.BeginChangeCheck();
        {
            base.DrawSurfaceOptions(material);
            DoPopup(ParticleGUI.Styles.colorMode, particleProps.colorMode, Enum.GetNames(typeof(ParticleGUI.ColorMode)));

            // EDIT by NBG START: Displaying extra properties under "Sheep custom" category
            EditorGUILayout.LabelField("Sheep Custom", EditorStyles.boldLabel);
            DoPopup(new GUIContent("Depth test"), zTest, Enum.GetNames(typeof(UnityEngine.Rendering.CompareFunction)));
            // EDIT by NBG END
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in blendModeProp.targets)
                MaterialChanged((Material)obj);
        }
    }

    public override void DrawSurfaceInputs(Material material)
    {
        base.DrawSurfaceInputs(material);
        SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
        DrawEmissionProperties(material, true);
    }

    public override void DrawAdvancedOptions(Material material)
    {
        SimpleLitGUI.Advanced(shadingModelProperties);
        EditorGUI.BeginChangeCheck();
        {
            materialEditor.ShaderProperty(particleProps.flipbookMode, ParticleGUI.Styles.flipbookMode);
            ParticleGUI.FadingOptions(material, materialEditor, particleProps);
            ParticleGUI.DoVertexStreamsArea(material, m_RenderersUsingThisMaterial, true);

            if (EditorGUI.EndChangeCheck())
            {
                MaterialChanged(material);
            }
        }

        DrawQueueOffsetField();
    }

    public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
    {
        CacheRenderersUsingThisMaterial(material);
        base.OnOpenGUI(material, materialEditor);
    }

    void CacheRenderersUsingThisMaterial(Material material)
    {
        m_RenderersUsingThisMaterial.Clear();

        ParticleSystemRenderer[] renderers = UnityEngine.Object.FindObjectsOfType(typeof(ParticleSystemRenderer)) as ParticleSystemRenderer[];
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            if (renderer.sharedMaterial == material)
                m_RenderersUsingThisMaterial.Add(renderer);
        }
    }
}
