using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace NBG.Audio
{
    public class Reverb : MonoBehaviour
    {
        public static Reverb instance;
        public AudioMixer mainMixer;
        [HideInInspector] public List<ReverbZone> zones = new List<ReverbZone>();

        void OnEnable()
        {
            instance = this;
        }

        public void ZoneEntered(ReverbZone zone)
        {
            zones.Add(zone);
        }

        public void ZoneLeft(ReverbZone zone)
        {
            zones.Remove(zone);
        }

        void Update()
        {
            if (Time.frameCount % 2 == 0) return;

            float weight = 0;
            float level = 0;
            float delay = 0;
            float diffusion = 0;
            float lowPass = 0;
            float highPass = 0;

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null)
                {
                    zones.RemoveAt(i);
                    i--;
                    continue;
                }
                var thisWeight = zones[i].GetWeight(Camera.main.transform.position + Vector3.forward);

                weight += thisWeight;
                level += thisWeight * zone.level;
                delay += thisWeight * zone.delay;
                diffusion += thisWeight * zone.diffusion;
                lowPass += thisWeight * Mathf.Log10(zone.lowPass);
                highPass += thisWeight * Mathf.Log10(zone.highPass);
            }

            if (weight != 0)
            {
                level /= weight;
                delay /= weight;
                diffusion /= weight;
                lowPass /= weight;
                highPass /= weight;

                mainMixer.SetFloat("ReverbVolume", level);
                mainMixer.SetFloat("ReverbDelay", delay);
                mainMixer.SetFloat("ReverbDiffusion", diffusion);
                mainMixer.SetFloat("ReverbHighpass", Mathf.Pow(10, highPass));
                mainMixer.SetFloat("ReverbLowpass", Mathf.Pow(10, lowPass));
            }
        }
    }
}
