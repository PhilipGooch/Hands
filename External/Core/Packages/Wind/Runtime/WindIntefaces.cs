using UnityEngine;

namespace NBG.Wind
{
    /// <summary>
    /// Allows multiplying force which object recieves from wind
    /// </summary>
    public interface IWindMultiplier
    {
        float GetWindMultiplier(Vector3 windDirection);
    }

    /// <summary>
    /// Allows for custom handling of wind
    /// </summary>
    public interface IWindReceiver
    {
        void OnReceiveWind(Vector3 wind);
    }
}
