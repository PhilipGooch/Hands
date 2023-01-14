using System;
using NBG.Core;
using NBG.Core.Streams;
using NBG.Net.Systems;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace NBG.Net
{
    [Protocol]
    public static class NetBehaviourListProtocol
    {
        public static ushort MasterFrame; // Server->client
        public static ushort DeltaFrame; // Server->client
        public static ushort FrameAck; // Client->server
    }

    /// <summary>
    /// INetBehaviour manager.
    /// </summary>
    public class NetBehaviourList : INetFrameCollector, INetFrameReader
    {
        [ClearOnReload] private static NetBehaviourList _instance = null;

        public static NetBehaviourList instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetBehaviourList();
                }

                return _instance;
            }
        }

        private readonly Dictionary<int, List<Entry>> behaviours = new Dictionary<int, List<Entry>>();

        public int GetNextFreeId()
        {
            return behaviours.Keys.Max() + 1;
        }

        public void Register(int registerID, Scene scene)
        {
            var rootGOs = scene.GetRootGameObjects();
            var list = new List<Entry>();
            for (var i = 0; i < rootGOs.Length; i++)
            {
                FindRecursive(rootGOs[i].transform, list);
            }
            ThrowOnDoubleRegistration(registerID);
            behaviours[registerID] = list;
        }

        public void Register(int registerID, Transform transform)
        {
            var entries = new List<Entry>();
            FindRecursive(transform, entries);
            ThrowOnDoubleRegistration(registerID);
            behaviours[registerID] = entries;
        }

        public void Register(int registerID, INetBehavior behavior)
        {
            ThrowOnDoubleRegistration(registerID);
            behaviours[registerID] = new List<Entry>() { new Entry() { netBehavior = behavior } };
        }

        public void Unregister(int registerID)
        {
            var wasRemoved = behaviours.Remove(registerID);
            if (!wasRemoved)
            {
                Debug.LogWarning("Tried to Unregister NetBehaviourList Entry with Id {registerID} but was not found");
            }
        }

        [Conditional("UNITY_EDITOR")]
        private void ThrowOnDoubleRegistration(int registerID)
        {
            if (behaviours.TryGetValue(registerID, out var existing))
            {
                throw new Exception($"Behaviour with ID {registerID} is already registered");
            }
        }
        

        public int UnregisterAll(int startID)
        {
            var toRemove = behaviours.Keys.Where(x => x >= startID).ToArray();
            foreach (var key in toRemove)
            {
                behaviours.Remove(key);
            }

            return toRemove.Length;
        }

        public void Clear()
        {
            behaviours.Clear();
        }

        private void FindRecursive(Transform current, List<Entry> bodies)
        {
            if (current.TryGetComponent<INetBehavior>(out var netBehavior))
            {
                bodies.Add(new Entry()
                {
                    netBehavior = netBehavior
                });
            }

            foreach (Transform childTransform in current)
            {
                FindRecursive(childTransform, bodies);
            }
        }

        struct Entry
        {
            public INetBehavior netBehavior;
        }

        public void OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            foreach (var behaviourList in behaviours)
            {
                var bodyList = behaviourList.Value;
                for (int i = 0; i < bodyList.Count; i++)
                {
                    var item = bodyList[i];
                    item.netBehavior.OnNetworkAuthorityChanged(authority);
                }
            }
        }

        #region INetFrameCollector
        private IStream tmpNetBehaviours = BasicStream.Allocate(NetWriteAndSendFrame.FULL_STATE_INITIAL_SIZE);
        
        IStream INetFrameCollector.Collect()
        {
            var ret = BasicStream.Allocate(NetWriteAndSendFrame.FULL_STATE_INITIAL_SIZE);
            foreach (var key in behaviours)
            {
                tmpNetBehaviours.Reset();
                tmpNetBehaviours.WriteID((uint)key.Key);
                var bodyList = behaviours[key.Key];
                for (int i = 0; i < bodyList.Count; i++)
                {
                    var item = bodyList[i];
                    var streamer = item.netBehavior as INetStreamer;
                    streamer?.CollectState(tmpNetBehaviours);
                }
                tmpNetBehaviours.Flip();
                ret.WriteStream(tmpNetBehaviours);
            }
            ret.Flip();
            return ret;
        }

        void INetFrameCollector.Validate(IStreamReader stream)
        {
        }

        public IStream CalculateDelta(IStreamReader fullStream, IStreamReader baseStream) 
        {
            var baseScopes = baseStream.ReadScopes();
            var fullScopes = fullStream.ReadScopes();

            var ret = BasicStream.Allocate(1024);

            foreach (var nextScopeID in fullScopes.Keys)
            {
                tmpNetBehaviours.Reset();
                tmpNetBehaviours.WriteID(nextScopeID);
                var nextScope = fullScopes[nextScopeID];
                if (baseScopes.TryGetValue(nextScopeID, out var baseScope))
                {
                    //Scope exists in base and in full -> Send Delta
                    var bodies = behaviours[(int)nextScopeID];
                    tmpNetBehaviours.Write(true);
                    foreach (var entry in bodies)
                    {
                        var streamer = entry.netBehavior as INetStreamer;
                        streamer?.CalculateDelta(baseScope, nextScope, tmpNetBehaviours);
                    }
                } 
                else
                {
                    //Scope is new -> Send full scope. 
                    tmpNetBehaviours.Write(false);
                    tmpNetBehaviours.CopyStreamData(nextScope);
                }
                tmpNetBehaviours.Flip();
                ret.WriteStream(tmpNetBehaviours);
            }
            ret.Flip();

            return ret;
        }

        #endregion

        #region INetFrameReader

        void INetFrameReader.OnEnable()
        {
        }
        
        void INetFrameReader.OnDisable()
        {
        }

        void INetFrameReader.Read(IStreamReader frame0, IStreamReader frame1, float mix, float timeBetweenFrames)
        {
            var frame0Scopes = frame0?.ReadScopes();
            var frame1Scopes = frame1.ReadScopes();

            foreach (var registerID in behaviours.Keys)
            {
                IStream scope0Stream = null;
                if (frame0Scopes == null || !frame0Scopes.TryGetValue((uint)registerID, out scope0Stream))
                {
                    //Note: No baseframe because scope is newly added. Render with SingleScope
                    if (frame1Scopes.TryGetValue((uint)registerID, out var scope1Single))
                    {
                        ApplySingleScope(registerID, scope1Single);
                        continue;
                    }
                }

                if (!frame1Scopes.TryGetValue((uint)registerID, out var scope1Stream))
                {
                    Debug.LogWarning($"BehaviourList {registerID} exists in scene, but does not exist in frame1 {frame1}");
                    continue;
                }

                ApplyScopeInterpolated(registerID, scope0Stream, scope1Stream, mix, timeBetweenFrames);
            }
        }
        void INetFrameReader.AddDelta(IStreamReader baseStream, IStreamReader deltaStream, IStreamWriter targetStream)
        {
            var baseScopes = baseStream.ReadScopes();
            var deltaScopes = deltaStream.ReadScopes();
            foreach (var registerID in behaviours.Keys)
            {
                if (deltaScopes.TryGetValue((uint)registerID, out var behaviourDelta))
                {
                    var isDelta = behaviourDelta.ReadBool();
                    if (isDelta) 
                    {
                        tmpNetBehaviours.Reset();
                        tmpNetBehaviours.WriteID((uint)registerID);
                        var behaviourBase = baseScopes[(uint)registerID];
                        var bodies = behaviours[(int)registerID];
                        foreach (var entry in bodies)
                        {
                            var streamer = entry.netBehavior as INetStreamer;
                            streamer?.AddDelta(behaviourBase, behaviourDelta, tmpNetBehaviours);
                        }
                    }
                    else
                    {
                        tmpNetBehaviours.Reset();
                        tmpNetBehaviours.WriteID((uint)registerID);
                        tmpNetBehaviours.CopyStreamData(behaviourDelta);
                    }
                    tmpNetBehaviours.Flip();
                    targetStream.WriteStream(tmpNetBehaviours);
                }
            }
        }

        private void ApplyScopeInterpolated(int scopeID, IStreamReader frame0Stream, IStreamReader frame1Stream, float mix, float timeBetweenFrames)
        {
            if (!behaviours.TryGetValue(scopeID, out var allBodies))
            {
                Debug.LogError($"Trying to apply BehaviourList {scopeID} but scope was not found!");
                return;
            }

            for (var i = 0; i < allBodies.Count; i++)
            {
                var item = allBodies[i];
                var streamer = item.netBehavior as INetStreamer;
                streamer?.ApplyLerpedState(frame0Stream, frame1Stream, mix, timeBetweenFrames);
            }
        }

        private void ApplySingleScope(int scopeID, IStreamReader frameStream)
        {
            if (!behaviours.TryGetValue(scopeID, out var allBodies))
            {
                Debug.LogError($"Trying to apply BehaviourList {scopeID} but scope was not found!");
                return;
            }

            for (var i = 0; i < allBodies.Count; i++)
            {
                var item = allBodies[i];
                var streamer = item.netBehavior as INetStreamer;
                streamer?.ApplyState(frameStream);
            }
        }

        #endregion
    }
}