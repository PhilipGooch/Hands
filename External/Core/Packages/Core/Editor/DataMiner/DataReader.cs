using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NBG.Core.DataMining
{
    public class DataReader
    {
        public uint UniqueFrameCount { get; private set; }
        public uint FirstFrameNo { get; private set; } // Unity's Time.frameCount
        public uint LastFrameNo { get; private set; } // Unity's Time.frameCount
        public uint FrameCount => (LastFrameNo - FirstFrameNo + 1);

        readonly BinaryReader _reader; // No ownership

        class DataSource
        {
            public IDataSource source;
            public byte version;
            public Dictionary<uint, IDataBlob> values = new Dictionary<uint, IDataBlob>(); //FrameUnique
            public Dictionary<uint, List<IDataBlob>> valueLists = new Dictionary<uint, List<IDataBlob>>();
        }
        Dictionary<byte, DataSource> _sources = new Dictionary<byte, DataSource>();
        public IEnumerable<IDataSource> Sources => _sources.Values.Select(ds => ds.source);

        public Dictionary<uint, IDataBlob> GetFrameUniqueValuesForDataSource(byte id)
        {
            var ds = _sources[id];
            if (!ds.source.FrameUnique)
                throw new System.Exception($"Can't use GetFrameUniqueValuesForDataSource for source id:{ds.source.Id} because that source is not frame unique.");
            return _sources[id].values;
        }

        public DataReader(BinaryReader reader)
        {
            _reader = reader;
        }

        public void Read()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Data Miner] Reader report:");

            _sources.Clear();
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);

            // Header
            DataAssert(_reader.ReadChar() == 'N');
            DataAssert(_reader.ReadChar() == 'B');
            DataAssert(_reader.ReadChar() == 'G');
            DataAssert(_reader.ReadChar() == 'R');
            DataAssert(_reader.ReadChar() == 'E');
            DataAssert(_reader.ReadChar() == 'C');

            var fileVersion = _reader.ReadByte();
            DataAssert(fileVersion == DataMiner.FileVersion); //TODO: backwards compatibility

            var sourcesCount = _reader.ReadByte();
            for (int i = 0; i < sourcesCount; ++i)
            {
                var id = _reader.ReadByte();
                var version = _reader.ReadByte();
                
                var supported = DataMiner.Sources.Any(s => s.Id == id && s.Version >= version);
                if (supported)
                {
                    var ds = new DataSource();
                    ds.version = version;
                    ds.source = DataMiner.Sources.Single(s => s.Id == id && s.Version >= version);
                    _sources.Add(id, ds);
                    sb.AppendLine($" * Supported data source found: {id} version {version}. Reader version {ds.source.Version}.");
                }
            }

            sb.AppendLine($"Parsing Data Miner recording version {fileVersion} with {sourcesCount} sources.");

            // Read the first frame tag (guaranteed)
            FirstFrameNo = ReadFrameIndex(_reader);
            DataAssert(FirstFrameNo != uint.MaxValue);
            LastFrameNo = FirstFrameNo;
            UniqueFrameCount = 1;

            // Find other frame tags
            while (_reader.BaseStream.Position + 1 < _reader.BaseStream.Length) //TODO: gracefully handle corrupted files
            {
                uint frameIndex = ReadFrameIndex(_reader);
                if (frameIndex != uint.MaxValue)
                {
                    DataAssert(frameIndex > LastFrameNo);
                    LastFrameNo = frameIndex;
                    UniqueFrameCount++;
                }
                else
                {
                    ReadTag(LastFrameNo);
                }
            }

            sb.AppendLine($"Frames {FirstFrameNo} to {LastFrameNo}. Unique: {UniqueFrameCount}.");
            Debug.Log(sb.ToString());
        }

        void ReadTag(uint currentFrameId)
        {
            var id = _reader.ReadByte();
            DataAssert(id != 0xFF && id != 0xFE);

            uint len = _reader.ReadByte();
            if (len == 0xFF)
                len = _reader.ReadUInt32();

            DataSource ds = null;
            _sources.TryGetValue(id, out ds);
            if (ds != null)
            {
                // Read
                var beginPos = _reader.BaseStream.Position;
                var blob = ds.source.Read(_reader, ds.version);
                if (ds.source.FrameUnique)
                {
                    ds.values.Add(currentFrameId, blob);
                }
                else
                {
                    List<IDataBlob> valueList;
                    if (!ds.valueLists.TryGetValue(currentFrameId, out valueList))
                    {
                        valueList = new List<IDataBlob>();
                        valueList.Add(blob);
                        ds.valueLists.Add(currentFrameId, valueList);
                    }
                }
                var endPos = _reader.BaseStream.Position;
                DataAssert(endPos - beginPos == len);
            }
            else
            {
                // Skip
                _reader.BaseStream.Seek(len, SeekOrigin.Current);
            }
        }

        /*static void SkipOverTag(BinaryReader reader)
        {
            var id = reader.ReadChar();
            ThrowIfFalse(id != 0xFF && id != 0xFE);

            uint len = reader.ReadByte();
            if (len == 0xFF)
                len = reader.ReadUInt32();

            reader.BaseStream.Seek(len, SeekOrigin.Current);
        }*/

        static uint ReadFrameIndex(BinaryReader reader)
        {
            var id = reader.ReadByte();
            if (id == DataWriter.kFrameShortTagId)
            {
                return reader.ReadUInt16();
            }
            else if (id == DataWriter.kFrameLongTagId)
            {
                return reader.ReadUInt32();
            }
            else
            {
                reader.BaseStream.Seek(-1, SeekOrigin.Current); //TODO@UGH
                return uint.MaxValue;
            }
        }

        static void DataAssert(bool condition)
        {
            if (!condition)
                throw new InvalidDataException();
        }
    }
}
