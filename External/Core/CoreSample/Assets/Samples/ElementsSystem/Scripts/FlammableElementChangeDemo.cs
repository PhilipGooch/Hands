using NBG.Core;
using NBG.ElementsSystem;
using UnityEngine;

namespace CoreSample.ElementsSystemDemo
{
    /// <summary>
    /// Example script of how to use flammable objects inside other scripts
    /// </summary>
    public class FlammableElementChangeDemo : MonoBehaviour, IManagedBehaviour
    {
        new Renderer renderer;
        Material material;
        IFlammable flammable;
        Color startingColor;

        [SerializeField]
        ParticleSystem igniteParticles;
        [SerializeField]
        ParticleSystem extinguishParticles;
        [SerializeField]
        Color heatedColor;
        [SerializeField]
        Color burnedOutColor;

        const string materialName = "_BaseColor";

        void OnExtinguish(bool withEffects)
        {
            if (withEffects)
                extinguishParticles.Emit(10);

            material.SetColor(materialName, startingColor);
        }

        void OnIgnite(bool withEffects)
        {
            if (withEffects)
                igniteParticles.Emit(10);
        }

        void OnBurnOut(bool withEffects)
        {
            material.SetColor(materialName, burnedOutColor);
        }

        void OnBurningAmountChanged(float amount)
        {
            if (flammable.IsBurning && !flammable.IsBurnedOut)
            {
                material.SetColor(materialName, Color.Lerp(startingColor, heatedColor, amount));
            }
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            renderer = GetComponent<Renderer>();
            material = renderer.material;
            startingColor = material.GetColor(materialName);
            flammable = GetComponent<IFlammable>();

            flammable.OnIgnited += OnIgnite;
            flammable.OnExtinguished += OnExtinguish;
            flammable.OnBurnedOut += OnBurnOut;
            flammable.OnBurningAmountChanged += OnBurningAmountChanged;
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
        }
    }
}
