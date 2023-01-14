using UnityEngine;
using UnityEditor;

namespace NBG.Audio.Editor
{
    public static class AudioSourceExtensions
    {
        [MenuItem("CONTEXT/Transform/No Brakes AudioSource")]
        public static void RealisticAudioSetup(MenuCommand command)
        {
            Undo.RecordObject(command.context, "No Brakes AudioSource");
            ((Transform)command.context).RealisticAudioSetup();
            EditorUtility.SetDirty(command.context);
        }

        public static void RealisticAudioSetup(this Transform transform)
        {
            // Get reference or Adds the AudioSource and AudioLowPassFilter of this gameobject.
            AudioSource audioSource = transform.gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = transform.gameObject.AddComponent<AudioSource>();

            // Setup constant parameters.
            audioSource.spatialBlend = 1;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 2;
            audioSource.maxDistance = 20;

            // Set realistic curve for lowering audio level with distance.
            var animCurve = new AnimationCurve(
                new Keyframe(2, 1f),
                //new Keyframe(8, .5f),
                new Keyframe(audioSource.maxDistance, 0f)
            );
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            animCurve.SmoothTangents(1, .025f);
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animCurve);

            // Set spread curve. When listener is close to source spread=0.5(180), farther away until spread=0.15(27) more stereo jump 3D. 
            // This avoids sound jump from L to R when being close to sound, and creates a more natural progressive stereo effect.
            var spreadCurve = new AnimationCurve(
                new Keyframe(0, 0.5f),
                new Keyframe(audioSource.maxDistance, 0.15f)
            );
            audioSource.SetCustomCurve(AudioSourceCurveType.Spread, spreadCurve);
        }
    }
}
