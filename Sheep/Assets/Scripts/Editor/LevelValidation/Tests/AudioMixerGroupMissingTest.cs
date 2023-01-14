using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMixerGroupMissingTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;
    public override string Name => "There are no empty Output Audio Mixer Group fields";
    public override string Category => "Audio";

    List<GameObject> violations = new List<GameObject>();

    protected override Result OnRun(ILevel level)
    {
        FindViolations(level);

        var result = new Result();

        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    void FindViolations(ILevel level)
    {
        violations.Clear();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            var audioSources = root.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                if (audioSource.outputAudioMixerGroup == null)
                {
                    PrintLog("No Output Audio Mixer Group found", audioSource);
                    violations.Add(audioSource.gameObject);
                }
            }

            var audioPools = root.GetComponentsInChildren<BaseAudioPool>();
            foreach (var pool in audioPools)
            {
                if (pool.mixerGroup == null)
                {
                    PrintLog("No Output Audio Mixer Group found", pool);
                    violations.Add(pool.gameObject);
                }
            }
        }
    }
}
