using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

namespace NBG.Audio
{
    public class Ambience : MonoBehaviour
    {
        public static Ambience instance;
        [System.NonSerialized] public AmbienceZone activeZone;
        AmbienceSource[] sources;
        AmbienceZone[] zones;
        List<AmbienceZone> activeZones = new List<AmbienceZone>();
        AudioListener audioListener;
        [SerializeField] bool debug = false;
        [SerializeField] string actualZone = "";

        public void OnEnable()
        {
            instance = this;
            sources = GetComponentsInChildren<AmbienceSource>();
        }

        void Start()
        {
            StartCoroutine(GetListenerPosition());
        }

        public void TransitionToZone(AmbienceZone zone, float duration)
        {
            if (activeZone != null)
                if (activeZone.transitionExit != null)
                    activeZone.transitionExit.Play();

            activeZone = zone;
            for (int i = 0; i < sources.Length; i++)
            {
                var volume = 0f;
                for (int j = 0; j < zone.sources.Length; j++)
                    if (zone.sources[j] == sources[i])
                        volume = zone.volumes[j];
                sources[i].FadeVolume(volume, duration);
            }
            if (zone.transitionEnter != null) zone.transitionEnter.Play();

            GameAudio.instance.SetAmbienceZoneMix(zone, duration);
        }

        public void EnterZone(AmbienceZone trigger)
        {
            if (activeZones.Contains(trigger)) return;
            activeZones.Add(trigger);
            CalculateActiveZoneTrigger();
        }

        public void LeaveZone(AmbienceZone trigger)
        {
            if (!activeZones.Contains(trigger)) return;
            activeZones.Remove(trigger);
            CalculateActiveZoneTrigger();
        }

        private void CalculateActiveZoneTrigger()
        {
            if (activeZones.Count == 0) return;
            AmbienceZone bestZone = activeZones[0];
            for (int i = 1; i < activeZones.Count; i++)
                if (activeZones[i].priority < bestZone.priority)
                    bestZone = activeZones[i];

            if (activeZone == bestZone)
                return;

            TransitionToZone(bestZone, bestZone.transitionDuration);
        }

        private void CalculateActiveZone()
        {
            if (audioListener == null) return;

            var bestPriority = int.MinValue;
            AmbienceZone bestZone = null;

            if (zones == null)
                zones = GetComponentsInChildren<AmbienceZone>();

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].sphereCollider != null)
                {
                    if (Vector3.Distance(zones[i].sphereCollider.transform.position, audioListener.transform.position) < zones[i].sphereCollider.radius)
                    {
                        bestZone = zones[i];
                        bestPriority = zones[i].priority;
                    }
                }
                else if (zones[i].boxCollider != null)
                {
                    if (zones[i].boxCollider.bounds.Contains(audioListener.transform.position) && zones[i].priority > bestPriority)
                    {
                        bestZone = zones[i];
                        bestPriority = zones[i].priority;
                    }
                }
            }

            if (bestZone == null) return;

            if (debug)
                actualZone = bestZone.name;

            if (activeZone == bestZone)
                return;

            TransitionToZone(bestZone, bestZone.transitionDuration);
        }

        private void Update()
        {
            CalculateActiveZone();
        }

        IEnumerator GetListenerPosition()
        {
            while (audioListener == null)
            {
                yield return new WaitForSeconds(0.2f);
                audioListener = GameObject.FindObjectOfType<AudioListener>();
            }
        }
    }
}
