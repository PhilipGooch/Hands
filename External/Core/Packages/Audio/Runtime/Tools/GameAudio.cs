using UnityEngine;
using UnityEngine.Audio;

namespace NBG.Audio
{
    public class GameAudio : MonoBehaviour
    {
        public AudioMixer mainMixer;
        public UISFX UISfx;
        public bool debug = false;
        public static GameAudio instance;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this.gameObject);
        }

        public void SetMasterLevel(float level)
        {
            mainMixer.SetFloat("MasterVolume", level);      //Options.LogMap(level, 0, 1, -80, -24, 0)); 
        }

        public void SetFXLevel(float level)
        {
            mainMixer.SetFloat("FXVolume", level);          // Options.LogMap(level, 0, 0.8f, 1, -80, 0, 6));  
        }

        public void SetMusicLevel(float level)
        {
            mainMixer.SetFloat("MusicVolume", level);       //Options.LogMap(level, 0, 0.8f, 1, -80, 0, 6);
        }

        public void SetVoiceLevel(float level)
        {
            mainMixer.SetFloat("VoiceVolume", 0);           //Options.LogMap(level, 0, 0.8f, 1, -80, 0, 6));
        }

        public void SetReverbLevel(float level)
        {
            reverbLevel = level;
            mainMixer.SetFloat("ReverbVolume", reverbLevel + currentMainVerb);
        }

#pragma warning disable
        float musicLevel;
        float reverbLevel;
        float ambienceLevel;
        float effectsLevel;
        float effectsLowpass;
        float physicsLevel = 0;
        float characterLevel = -8;
#pragma warning enable

        // ambience submix
        float transitionPhase = 1;
        float transitionSpeed = 0;
        float fromMainVerb, toMainVerb, currentMainVerb = 0;
        float fromMusic, toMusic, currentMusic = 0;
        float fromAmbience, toAmbience, currentAmbience = 0;
        float fromEffects, toEffects, currentEffects = 0;
        float fromEffectsLowpass, toEffectsLowpass, currentEffectsLowpass = 0;
        float fromPhysics, toPhysics, currentPhysics = 0;
        float fromCharacter, toCharacter, currentCharacter = 0;

        public void SetAmbienceZoneMix(AmbienceZone zone, float transitionDuration)
        {
            transitionPhase = 0;
            transitionSpeed = 1 / transitionDuration;

            fromMainVerb = currentMainVerb;
            fromMusic = currentMusic;
            fromAmbience = currentAmbience;
            fromEffects = currentEffects;
            fromEffectsLowpass = currentEffectsLowpass;
            fromPhysics = currentPhysics;
            fromCharacter = currentCharacter;

            toMainVerb = zone.mainVerbLevel;
            toMusic = zone.musicLevel;
            toAmbience = zone.ambienceLevel;
            toEffects = zone.effectsLevel;
            toEffectsLowpass = zone.effectsLowpass;
            toPhysics = zone.physicsLevel;
            toCharacter = zone.characterLevel;

            if (fromEffectsLowpass != toEffectsLowpass)
            {
                if (zone.transitionAudioSource != null && !zone.transitionAudioSource.isPlaying && Time.timeSinceLevelLoad > 5)
                    zone.transitionAudioSource.PlayOneShot(zone.transitionAudioSource.clip);
            }

            currentEffectsLowpass = toEffectsLowpass;
        }

        //DO NOT EVEN THINK IN RESTORING THE UPDATE THAT WAS WRITTEN HERE. - Joaquin.

        public void FixMixerOnAudioSources()
        {
            AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.outputAudioMixerGroup)
                {
                    // Extract the ghost group's name from the ghost main mixer
                    AudioMixerGroup outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                    string groupName = outputAudioMixerGroup.name;

                    // Use group and mixer names to find the real group among the actual MAIN MIXER
                    var matchingGroups = GameAudio.instance.mainMixer.FindMatchingGroups(groupName);
                    if (matchingGroups.Length > 0)
                        audioSource.outputAudioMixerGroup = matchingGroups[0];
                }
            }
        }

    }
}
