using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using NBG.Core.GameSystems;

namespace NBG.Core.DataMining
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup), orderLast: true)]
    [UpdateInGroup(typeof(LateUpdateSystemGroup), orderLast: true)]
    public class DataMinerRecorder : GameSystem
    {
        protected override void OnCreate()
        {
            Enabled = false;

            Debug.Assert(DataMiner.activeRecorder == null);
            DataMiner.activeRecorder = this;
        }

        protected override void OnDestroy()
        {
            Debug.Assert(DataMiner.activeRecorder == this);
            DataMiner.activeRecorder = null;
        }

        protected override void OnStartRunning()
        {
            StartRecording();
        }

        protected override void OnStopRunning()
        {
            StopRecording();
        }

        protected override void OnUpdate()
        {
            if (Time.inFixedTimeStep)
            {
                DataMiner.OnFixedUpdate();
            }
            else
            {
                DataMiner.OnLateUpdate();
            }
        }



        public enum State
        {
            //Inactive,  // No impact
            Active,    // Memory allocated, etc.
            Recording, // Live!
        }

        private new State _state = State.Active;
        private FileStream _file; // Current output. Valid in between Start/Stop calls.
        private DataWriter _stream;
        public DataWriter Writer => _stream;
        public string OutputPath { get; private set; }

        bool StartRecording()
        {
            try
            {
                Assert.IsTrue(_state == State.Active);

                // Prepare file
                if (!Directory.Exists(DataMiner.DefaultFolder))
                    Directory.CreateDirectory(DataMiner.DefaultFolder);
                OutputPath = Path.Combine(DataMiner.DefaultFolder, $"recording-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.{DataMiner.FileExtension}");
                _file = new FileStream(OutputPath, FileMode.Create);

                // Setup stream
                _stream = new DataWriter(new BinaryWriter(_file));
                _stream.WriteHeader();

                DataMiner.OnBeginRecording();

                _state = State.Recording;
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"[Data Miner] Failed to start recording: {e.Message}");
                return false;
            }
        }

        void StopRecording()
        {
            try
            {
                Assert.IsTrue(_state == State.Recording);
                _state = State.Active;

                DataMiner.OnEndRecording();

                // Cleanup stream
                _stream.WriteFooter();
                _stream = null;

                // Cleanup file
                _file.Close();
                _file.Dispose();
                _file = null;
            }
            catch (Exception e)
            {
                Debug.Log($"[Data Miner] Failed to stop recording: {e.Message}");
            }
        }
    }
}
