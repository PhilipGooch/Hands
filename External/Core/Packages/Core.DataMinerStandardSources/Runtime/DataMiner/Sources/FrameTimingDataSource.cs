using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace NBG.Core.DataMining.StandardSources
{
    public class FrameTimingDataSource : IDataSource, IDataSourceLateUpdater, ITimingProvider
    {
        public byte Id => (byte)DataSourceId.FrameTiming;
        public byte Version => 1;
        public bool FrameUnique => true;

        Recorder _recPlayerLoop;
        Recorder _recGCCollect;
        Recorder _recGfxWaitForPresent;

        public void OnBeginRecording()
        {
            _recPlayerLoop = Recorder.Get("PlayerLoop");
            _recPlayerLoop.enabled = true;

            _recGCCollect = Recorder.Get("GC.Collect");
            _recGCCollect.enabled = true;

            _recGfxWaitForPresent = Recorder.Get("Gfx.WaitForPresent");
            _recGfxWaitForPresent.enabled = true;
        }

        public void OnEndRecording()
        {
            _recPlayerLoop.enabled = false;
            _recGCCollect.enabled = false;
            _recGfxWaitForPresent.enabled = false;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((float)Time.unscaledDeltaTime * 1000.0f);

            writer.Write((float)(_recPlayerLoop.elapsedNanoseconds / 1000000.0));
            writer.Write((float)(_recGCCollect.elapsedNanoseconds / 1000000.0));
            writer.Write((float)(_recGfxWaitForPresent.elapsedNanoseconds / 1000000.0));
        }

        public void OnLateUpdate()
        {
            DataMiner.RequestWrite(this, 16);
        }

        #region ITimingProvider
        IEnumerable<string> ITimingProvider.Names
        {
            get
            {
                yield return "unscaledDeltaTime";
                yield return "(dev) PlayerLoop";
                yield return "(dev) GC.Collect";
                yield return "(dev) Gfx.WaitForPresent";
            }
        }

        public class Data : IDataBlob, ITimingProvider.IData
        {
            public float unscaledDeltaTime;
            public float playerLoop;
            public float gcCollect;
            public float gfxWaitForPresent;

            float ITimingProvider.IData.GetValue(uint index)
            {
                switch (index)
                {
                    case 0:
                        return unscaledDeltaTime;
                    case 1:
                        return playerLoop;
                    case 2:
                        return gcCollect;
                    case 3:
                        return gfxWaitForPresent;
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
                    data.unscaledDeltaTime = reader.ReadSingle();
                    data.playerLoop = reader.ReadSingle();
                    data.gcCollect = reader.ReadSingle();
                    data.gfxWaitForPresent = reader.ReadSingle();
                    break;

                default:
                    throw new System.NotSupportedException($"Can't read version {version}.");
            }

            return data;
        }
    }
}
