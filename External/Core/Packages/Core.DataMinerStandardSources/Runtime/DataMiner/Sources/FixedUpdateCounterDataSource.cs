using System.Collections.Generic;
using System.IO;

namespace NBG.Core.DataMining.StandardSources
{
    public class FixedUpdateCounterDataSource : IDataSource, IDataSourceFixedUpdater, IDataSourceLateUpdater, ICountersProvider
    {
        public byte Id => (byte)DataSourceId.FixedUpdate;
        public byte Version => 1;
        public bool FrameUnique => true;

        uint _fixedUpdateCount = 0; // Number of fixed updates performed during a frame

        public void OnBeginRecording()
        {
        }

        public void OnEndRecording()
        {
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((uint)_fixedUpdateCount);
        }

        public void OnFixedUpdate()
        {
            ++_fixedUpdateCount;
        }

        public void OnLateUpdate()
        {
            DataMiner.RequestWrite(this, 4);

            _fixedUpdateCount = 0;
        }

        #region ICountersProvider
        IEnumerable<string> ICountersProvider.Names
        {
            get
            {
                yield return "Fixed Updates";
            }
        }

        public class Data : IDataBlob, ICountersProvider.IData
        {
            public uint fixedUpdates;

            long ICountersProvider.IData.GetValue(uint index)
            {
                switch (index)
                {
                    case 0:
                        return fixedUpdates;
                    default:
                        throw new System.NotSupportedException();
                }
            }
        }
        #endregion

        public IDataBlob Read(BinaryReader reader, byte version)
        {
            var data = new Data();

            switch (version)
            {
                case 1:
                    data.fixedUpdates = reader.ReadUInt32();
                    break;

                default:
                    throw new System.NotSupportedException($"Can't read version {version}.");
            }

            return data;
        }
    }
}
