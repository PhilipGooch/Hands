using NBG.LogicGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreSample.SteamDemo
{
    public class ScaleParticlesFromActivation : MonoBehaviour
    {
        float ActivationValue;
        float startparticleSize;

        [SerializeField]
        new ParticleSystem particleSystem;

        private void OnValidate()
        {
            if (particleSystem == null)
                particleSystem = GetComponent<ParticleSystem>();
        }

        [NodeAPI("SetActivationValue")]
        public void SetActivationValue(float activationValue)
        {
            ActivationValue = activationValue;
        }

        void Start()
        {
            startparticleSize = particleSystem.main.startSize.constant;
        }

        void Update()
        {
            var main = particleSystem.main;
            var size = main.startSize;
            size.constant = startparticleSize * ActivationValue;
            main.startSize = size;

        }
    }
}
