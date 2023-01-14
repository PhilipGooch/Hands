//#define ENABLE_DEBUG_STANDARD_COUNTERS_FRAMESELECTHANDLER
using System.Collections.Generic;
using System.IO;

namespace NBG.Core.DataMining.StandardSources
{
    public class StandardCountersDataSource : IDataSource, IDataSourceLateUpdater, ICountersProvider
#if ENABLE_DEBUG_STANDARD_COUNTERS_FRAMESELECTHANDLER
        , IFrameSelectHandler
#endif
    {
        public byte Id => (byte)DataSourceId.StandardCounters;
        public byte Version => 1;
        public bool FrameUnique => true;

        const int kIntervalFrames = 100;
        int _currentInterval = kIntervalFrames;

        public void OnBeginRecording()
        {
        }

        public void OnEndRecording()
        {
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((System.Int64)System.GC.GetTotalMemory(false));
        }

        public void OnLateUpdate()
        {
            if (_currentInterval < kIntervalFrames)
            {
                _currentInterval++;
                return;
            }
            _currentInterval = 0;

            DataMiner.RequestWrite(this, 8);
        }

        #region ICountersProvider
        IEnumerable<string> ICountersProvider.Names
        {
            get
            {
                yield return "Total GC Memory";
            }
        }

        public class Data : IDataBlob, ICountersProvider.IData
        {
            public long totalMemoryGC;

            long ICountersProvider.IData.GetValue(uint index)
            {
                switch (index)
                {
                    case 0:
                        return totalMemoryGC;
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
                    data.totalMemoryGC = reader.ReadInt64();
                    break;
                default:
                    throw new System.NotSupportedException($"Can't read version {version}.");
            }

            return data;
        }

        #region IFrameSelectHandler
#if ENABLE_DEBUG_STANDARD_COUNTERS_FRAMESELECTHANDLER
        string IFrameSelectHandler.HandlerName => "Standard Counters (debug)";

        bool IFrameSelectHandler.UsePreviousState => true;

        void IFrameSelectHandler.OnFrameSelect(uint frameNo, uint dataFrameNo, IDataBlob blob)
        {
            var data = (Data)blob;

            UnityEngine.Debug.Log($"[{frameNo}][{dataFrameNo}] Total GC Memory: {data.totalMemoryGC}");
        }
#endif
        #endregion
    }
}
