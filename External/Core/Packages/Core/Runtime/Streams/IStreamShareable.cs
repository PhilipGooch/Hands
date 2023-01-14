using UnityEngine;

namespace NBG.Core.Streams
{
    public interface IStreamShareable
    {
        IStream Stream { get; }

        IStream AcquireShare();
        void ReleaseShare();
    }

    internal sealed class StreamShareable : IStreamShareable
    {
        IStream _stream;
        bool _shareInUse = false;

        public StreamShareable(IStream stream)
        {
            _stream = stream;
        }

        public IStream Stream => _stream;

        public IStream AcquireShare()
        {
            Debug.Assert(_shareInUse == false, "NetStream is already in use! Acquire failed.\n");
            _shareInUse = true;
            return _stream;
        }

        public void ReleaseShare()
        {
            Debug.Assert(_shareInUse == true, "NetStream is not in use! Release failed.");
            _shareInUse = false;
        }
    }

    public static class StreamShareableExtensions
    {
        public static IStreamShareable MakeShareable(this IStream stream)
        {
            var share = new StreamShareable(stream);
            return share;
        }
    }
}
