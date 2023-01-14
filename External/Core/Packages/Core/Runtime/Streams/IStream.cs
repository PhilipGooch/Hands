namespace NBG.Core.Streams
{
    public interface IStream : IStreamReader, IStreamWriter
    {
        /// <summary>
        /// The underlying byte array.
        /// Useful when sharing data with code 
        /// </summary>
        byte[] Buffer { get; }

        /// <summary>
        /// This streams position in Bytes.
        /// This is increased, whenever the first bit is written to this position.
        /// </summary>
        int PositionBytes { get; }

        /// <summary>
        /// This streams position in bits.
        /// </summary>
        int PositionBits { get; }

        /// <summary>
        /// Modify the current Read/Write pointer.
        /// This is in Bits!
        /// </summary>
        /// <param name="positionBits">absolute position, in bits</param>
        void Seek(int positionBits);

        /// <summary>
        /// This sets the read limit in bits to PositionBits and position to 0
        /// Use this to read from a stream after you've written to it.
        /// </summary>
        void Flip(); //TODO: better API name

        /// <summary>
        /// This resets the Streams position and disables Limits
        /// Useful when re-using an existing stream, that you want to put new data in.
        /// i.e. Pooling, temporary buffers, etc. 
        /// </summary>
        void Reset();
    }
}
