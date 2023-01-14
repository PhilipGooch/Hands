#if ENABLE_DATA_MINER_EVENT_SOURCE // Not implemented yet
using NBG.Core.DataMining;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

namespace NBG.Core.DataMining.StandardSources
{
    public class SceneEventDataSource : IDataSource, IEventsProvider
    {
        public byte Id => (byte)DataSourceId.SceneEvent;
        public byte Version => 1;
        public bool FrameUnique => false;

        // Format:
        // [1 byte] Length in bytes
        // [N bytes] UTF-8 encoded string
        const int kMaxBytes = 255;
        byte[] _buffer = new byte[kMaxBytes];
        byte _numBytes;

        public void OnBeginRecording()
        {
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        public void OnEndRecording()
        {
            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        private void SceneManager_sceneUnloaded(Scene scene)
        {
            Notify($"SceneUnloaded({scene.name})");
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Notify($"SceneLoaded({scene.name}, {mode})");
        }

        private void SceneManager_activeSceneChanged(Scene scene0, Scene scene1)
        {
            Notify($"ActiveSceneChanged({scene0.name}, {scene1.name})");
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_numBytes);
            writer.Write(_buffer, 0, _numBytes);
        }

        public void Notify(string eventText)
        {
            _numBytes = (byte)Encoding.UTF8.GetBytes(eventText, 0, eventText.Length, _buffer, 0);
            DataMiner.RequestWrite(this, (uint)_numBytes + 1); // TODO: meh?
        }

        IEnumerable<string> IEventsProvider.Names
        {
            get
            {
                yield return "Scene Event";
            }
        }

        IEnumerable<IEventsProvider.Type> IEventsProvider.Types
        {
            get
            {
                yield return IEventsProvider.Type.Scenes;
            }
        }

#if UNITY_EDITOR
        public class Data : IDataBlob, IEventsProvider.IData
        {
            public string eventText;

            public string GetValue(uint index)
            {
                switch (index)
                {
                    case 0:
                        return eventText;
                    default:
                        throw new System.NotSupportedException();
                }
            }
        }

        public IDataBlob Read(BinaryReader reader, byte version)
        {
            var data = new Data();

            switch (version)
            {
                case 1:
                    {
                        var numBytes = reader.ReadByte();
                        var buffer = reader.ReadBytes(numBytes);
                        data.eventText = Encoding.UTF8.GetString(buffer);
                    }
                    break;

                default:
                    throw new System.NotSupportedException($"Can't read version {version}.");
            }

            return data;
        }
#endif
    }
}
#endif //ENABLE_DATA_MINER_EVENT_SOURCE
