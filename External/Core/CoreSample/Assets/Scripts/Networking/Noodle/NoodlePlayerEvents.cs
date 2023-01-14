using NBG.Core;
using NBG.Core.Streams;
using NBG.Entities;
using NBG.Net.PlayerManagement;
using NBG.Unsafe;
using Noodles;
using Recoil;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoreSample.Network.Noodle
{
    /// <summary>
    /// Example implementation of IPlayerEvents.
    /// This is a game-specific collection to handle Player activity like spawning/removing/movement
    /// </summary>
    public class NoodlePlayerEvents : IPlayerEvents
    {
        //TODO: Eventually this needs to be properly acked toggles
        private InputFrame[] remoteInputFrames;
        private float[] remoteTimeStamps;
        private InputFrame empty;
        public GameObject prefab;
        public event Action OnLocalPlayerCountChanged;

        public NoodlePlayerEvents(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public void Init(int maxGlobalUsers)
        {
            remoteInputFrames = new InputFrame[maxGlobalUsers];
            remoteTimeStamps = new float[maxGlobalUsers];
            for (var i = 0; i < remoteTimeStamps.Length; i++)
            {
                remoteTimeStamps[0] = float.MinValue;
            }
        }

        void IPlayerEvents.DestroyInstance(GameObject playerObject, int globalID)
        {
            var script = playerObject.GetComponent<NoodlePlayerController>();
            var managedBehaviours = playerObject.GetComponentsInChildren<IManagedBehaviour>(true);
            foreach (var managedBehaviour in managedBehaviours)
            {
                managedBehaviour.OnLevelUnloaded();
            }
            script.Dispose();
            GameObject.Destroy(playerObject);
        }

        GameObject IPlayerEvents.CreateInstance(int globalID)
        {
            var instance = GameObject.Instantiate(prefab);
            instance.gameObject.name = $"{prefab.name} globalID: {globalID}";
            instance.gameObject.SetActive(true);


            if (instance.TryGetComponent<PlayerControllerBase>(out var playerController))
            {
                playerController.OnCreate();
            }

            RigidbodyRegistration.RegisterHierarchy(instance);
            var managedBehaviours = instance.GetComponentsInChildren<IManagedBehaviour>(true);
            foreach (var managedBehaviour in managedBehaviours)
                managedBehaviour.OnLevelLoaded();

            foreach (var managedBehaviour in managedBehaviours)
                managedBehaviour.OnAfterLevelLoaded();

            return instance;
        }

        void IPlayerEvents.OnLocalPlayerAdded(GameObject playerObject, int localID, int globalID)
        {
            var script = playerObject.GetComponent<NoodlePlayerController>();
            //if (localID == 0)
            {
                var controls = new UnityPlayerControls();
                controls.Player.Enable();
                controls.devices = InputSystem.devices;
                EntityStore.AddComponentObject(script.entity, controls);
            }
            CameraManagerBase.GetOrCreateInstance().AddCamera(script.entity);
            OnLocalPlayerCountChanged?.Invoke();
        }

        void IPlayerEvents.OnLocalPlayerRemoved(GameObject playerObject, int localID, int globalID)
        {
            var script = playerObject.GetComponent<NoodlePlayerController>();
            //if (localID == 0)
            {
                var controls = EntityStore.GetComponentObject<UnityPlayerControls>(script.entity);
                EntityStore.RemoveComponentObject(script.entity, controls);
            }
            CameraManagerBase.GetOrCreateInstance().RemoveCamera(script.entity);
            OnLocalPlayerCountChanged?.Invoke();
        }

        public void OnClientRemotePlayerAdded(GameObject playerObject, int globalID, IStreamReader payload)
        {
            //var frameCount = payload.ReadInt32(32);//TODO: why is this required?
        }

        public void OnServerRemotePlayerAdded(GameObject playerObject, int globalID)
        {
        }

        void IPlayerEvents.OnReceivePlayerInput(GameObject playerObject, int globalID, IStreamReader reader)
        {
            //var script = playerObject.GetComponent<NoodlePlayerController>();
            //ref var frame = ref EntityStore.GetComponentData<InputFrame>(script.entity);
            ref var frame = ref remoteInputFrames[globalID];
            frame.jump = reader.ReadBool();
            frame.grabL = reader.ReadBool();
            frame.grabR = reader.ReadBool();
            frame.playDead = reader.ReadBool();
            frame.lookPitch = reader.ReadInt32(6, 12).Dequantize(InputFrame.PITCH_RANGE, 12);
            frame.lookYaw = reader.ReadInt32(6, 12).Dequantize((float)Math.PI * 2f, 12) - (float)Math.PI;
            frame.moveMagnitude = reader.ReadInt32(6, 12).Dequantize(1, 12);
            frame.moveYaw = reader.ReadInt32(6, 12).Dequantize((float)Math.PI * 2f, 12) - (float)Math.PI;
            remoteTimeStamps[globalID] = Time.unscaledTime;
            //Debug.Log("Recieved input from globalID " + globalID + "grabL: " + frame.grabL + " jump ");
        }
        void IPlayerEvents.OnGatherPlayerInput(GameObject playerObject, int globalID, int localID, IStreamWriter writer)
        {
            var script = playerObject.GetComponent<NoodlePlayerController>();
            ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(script.entity);
            writer.Write(inputFrame.jump);
            writer.Write(inputFrame.grabL);
            writer.Write(inputFrame.grabR);
            writer.Write(inputFrame.playDead);
            writer.Write(inputFrame.lookPitch.Quantize(InputFrame.PITCH_RANGE, 12), 6, 12);
            writer.Write((inputFrame.lookYaw + (float)Math.PI).Quantize((float)Math.PI * 2f, 12), 6, 12);
            writer.Write(inputFrame.moveMagnitude.Quantize(1, 12), 6, 12);
            writer.Write((inputFrame.moveYaw + (float)Math.PI).Quantize((float)Math.PI * 2f, 12), 6, 12);
            //Debug.Log("Player input send: " + inputFrame.grabL);
        }

        public void OnPlayerCreatedBroadcast(GameObject playerObject, int scopeID, IStreamWriter writer)
        {
            //writer.Write(Time.frameCount, 32);//TODO: why is this required?
        }

        public unsafe void ApplyNetworkInputFrame(GameObject player, int globalID)
        {
            NoodlePlayerController controller = player.GetComponent<NoodlePlayerController>();
            var lastRemote = remoteTimeStamps[globalID];
            ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(controller.entity);
            if (lastRemote + 1 < Time.unscaledTime)
            {
                UnsafeUtility.MemCpy(inputFrame.AsPointer(), empty.AsPointer(), UnsafeUtility.SizeOf<InputFrame>());
            }
            else
            {
                ref var last = ref remoteInputFrames[globalID];
                UnsafeUtility.MemCpy(inputFrame.AsPointer(), last.AsPointer(), UnsafeUtility.SizeOf<InputFrame>());
            }
        }
    }
}
