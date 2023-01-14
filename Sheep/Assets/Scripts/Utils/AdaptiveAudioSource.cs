using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[System.Serializable]
internal class AdaptiveAudioSource
{
    [SerializeField]
    internal AudioSource audioSource;
    [Range(0, 1)]
    [SerializeField]
    private float lowestPitchValue = 0.5f;
    [Range(0, 10)]
    [SerializeField]
    private float pitchMulti = 2;
    [Tooltip("Needed to signal some specific behavior i.e. object reaching its movement limit")]
    [Range(0, 10)]
    [SerializeField]
    private float reachedLimitPitchMulti = 3;
   

    internal void PlaySound()
    {
        audioSource.Play();
    }

    internal void StopSound()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// Updates volume and pitch
    /// </summary>
    /// <param name="value"> from -1 to 1 </param>
    internal void UpdateSound(float value)
    {
        var absValue = Mathf.Abs(value);
        audioSource.pitch = GetPitchValue(absValue, false);
        audioSource.volume = absValue;
    }

    /// <summary>
    /// Updates volume and pitch
    /// </summary>
    /// <param name="value"> from -1 to 1 </param>
    /// <param name="isNearEnd"> Used to indicate if some kind of limit is reached to change the pitch of sound, i.e.: object on rails has reached rail end. </param>
    internal void UpdateSound(float value, bool isNearEnd)
    {
        var absValue = Mathf.Abs(value);
        audioSource.pitch = GetPitchValue(absValue, isNearEnd);
        audioSource.volume = absValue;
    }

    private float GetPitchValue(float absValue, bool isAtLimit)
    {
        if (!isAtLimit)
            return Mathf.Clamp(absValue * pitchMulti, lowestPitchValue, 1);
        else
            return Mathf.Clamp(absValue * reachedLimitPitchMulti, lowestPitchValue, reachedLimitPitchMulti);
    }

}
