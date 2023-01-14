using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace NBG.Core.DataMining
{
    /*  File format (version 1)

    [Header][First Frame Tag]...[Last FrameTag]

    = HEADER =
    [6 bytes] header "NBGREC"
    [1 byte] version
    [1 byte] data source header count

    Data source header:
    [1 byte] id
    [1 byte] version

    = Frame tag =
    [1 byte] 0xFF
    [2 bytes] frame index, valid for upcoming tags (short: ~18 minutes @ 60 FPS)
    or
    [1 byte] 0xFE
    [4 bytes] frame index, valid for upcoming tags

    = Data source tag =
    [1 byte] data source id (< 0xFE)
    [1 byte] length
    (if length above is 0xFF, long format) [4 bytes] length
    [length bytes] of binary content

    */
    public class DataWriter
    {
        public const byte kFrameShortTagId = 0xFF;
        public const byte kFrameLongTagId = 0xFE;

        readonly BinaryWriter _writer; // No ownership
        uint _lastFrameTag = uint.MaxValue; // Only write frame tags when there is at least a single data tag
        uint _tagLengthRequested; // Size of incoming data tag
        long _tagStartOffset; // Offset in writer of incoming data tag for error handling

        public DataWriter(BinaryWriter writer)
        {
            _writer = writer;
        }

        public void WriteHeader()
        {
            _writer.Write('N');
            _writer.Write('B');
            _writer.Write('G');
            _writer.Write('R');
            _writer.Write('E');
            _writer.Write('C');
            _writer.Write((byte)DataMiner.FileVersion);
            _writer.Write((byte)DataMiner.Sources.Count());
            foreach (var source in DataMiner.Sources)
            {
                _writer.Write((byte)source.Id);
                _writer.Write((byte)source.Version);
            }
            EnsureCurrentFrameTag();
        }

        public void WriteFooter()
        {
            EnsureCurrentFrameTag();
        }

        void WriteFrameTag(uint frameIndex)
        {
            if (frameIndex <= ushort.MaxValue)
            {
                _writer.Write((byte)kFrameShortTagId);
                _writer.Write((ushort)frameIndex);
            }
            else
            {
                _writer.Write((byte)kFrameLongTagId);
                _writer.Write((uint)frameIndex);
            }
        }

        void EnsureCurrentFrameTag()
        {
            var curFrame = (uint)Time.frameCount;
            if (curFrame != _lastFrameTag)
            {
                WriteFrameTag((uint)curFrame);
                _lastFrameTag = curFrame;
            }
        }

        public BinaryWriter BeginTag(byte id, uint lengthBytes)
        {
            Assert.IsTrue(lengthBytes >= 0);

            EnsureCurrentFrameTag();

            // Tag header
            _writer.Write((byte)id);
            if (lengthBytes < 0xFF)
            {
                _writer.Write((byte)lengthBytes);
            }
            else
            {
                _writer.Write((byte)0xFF);
                _writer.Write((uint)lengthBytes);
            }

            _tagLengthRequested = lengthBytes;
            _tagStartOffset = _writer.BaseStream.Position;

            return _writer;
        }

        public void EndTag()
        {
            var len = _writer.BaseStream.Position - _tagStartOffset;
            Assert.IsTrue(len == _tagLengthRequested, "DataSource wrote an incorrect amount of data.");
        }
    }
}
