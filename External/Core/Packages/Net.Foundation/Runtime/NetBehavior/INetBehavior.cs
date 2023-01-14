using NBG.Core.Streams;

namespace NBG.Net
{
    public interface INetBehavior
    {
        void OnNetworkAuthorityChanged(NetworkAuthority authority);
    }

    public interface INetStreamer
    {
        /// <summary>
        /// Serialize state to stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        void CollectState(IStreamWriter stream);

        /// <summary>
        /// Deserialize state from stream.
        /// </summary>
        /// <param name="state">Source stream.</param>
        void ApplyState(IStreamReader state);

        /// <summary>
        /// Deserialize state from two streams, which represent two frames.
        /// Frames are not necessarily sequential.
        /// </summary>
        /// <param name="state0">Source stream 1.</param>
        /// <param name="state1">Source stream 2.</param>
        /// <param name="mix">Lerp value.</param>
        /// <param name="timeBetweenFrames">Delta time of the given two frames. Expected to be a multiple of Time.fixedDeltaTime under normal circumstances.</param>
        void ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames);

        /// <summary>
        /// Manual compression.
        /// Pack state0 to state1 transition delta.
        /// </summary>
        /// <param name="state0">Base state stream.</param>
        /// <param name="state1">Target state stream.</param>
        /// <param name="delta">Delta stream.</param>
        void CalculateDelta(IStreamReader state0, IStreamReader state1, IStreamWriter delta);

        /// <summary>
        /// Manual compression.
        /// Unpack state0 to state1 transition delta;
        /// </summary>
        /// <param name="state0">Base state stream.</param>
        /// <param name="delta">Delta stream.</param>
        /// <param name="result">Target state stream.</param>
        void AddDelta(IStreamReader state0, IStreamReader delta, IStreamWriter result);
    }
}
