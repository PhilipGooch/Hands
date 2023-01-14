using NBG.Core;
using NBG.ElementsSystem;
using UnityEngine;

namespace CoreSample.ElementsSystemDemo
{
    /// <summary>
    /// Example script of how to use heatable objects inside other scripts
    /// </summary>
    public class HeatableElementChangeDemo : MonoBehaviour, IManagedBehaviour
    {
        new Renderer renderer;
        Material material;
        IHeatable heatable;
        Color startingColor;

        [SerializeField]
        ParticleSystem coolDownParticles;
        [SerializeField]
        ParticleSystem heatUpParticles;
        [SerializeField]
        Color heatedColor;

        const string materialName = "_BaseColor";

        void OnCoolDown(bool withEffects)
        {
            if (withEffects)
                coolDownParticles.Emit(10);
        }

        void OnHeatUp(bool withEffects)
        {
            if (withEffects)
                heatUpParticles.Emit(10);
        }

        void OnHeatableChanged(float amount)
        {
            material.SetColor(materialName, Color.Lerp(startingColor, heatedColor, amount));
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            renderer = GetComponent<Renderer>();
            material = renderer.material;
            startingColor = material.GetColor(materialName);
            heatable = GetComponent<IHeatable>();

            heatable.OnHeated += OnHeatUp;
            heatable.OnCooledDown += OnCoolDown;
            heatable.OnHeatAmountChanged += OnHeatableChanged;
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
        }
    }
}
