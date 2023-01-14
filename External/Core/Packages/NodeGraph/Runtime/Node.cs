using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public abstract class Node : MonoBehaviour
    {
        public Color nodeColour = Color.white;

        [HideInInspector]
        public Vector2 pos = new Vector2(10, 10);

        public string renamed;

        public virtual string Title
        {
            get
            {
                return GetType().Name;
            }
        }


        protected virtual void OnConnect(NodeOutput output)
        {
            Process();
        }

        List<NodeSocket> allSockets;
        public virtual List<NodeSocket> ListAllSockets()
        {
            if (allSockets == null)
                RebuildSockets();
            return allSockets;
        }

        public void RebuildSockets()
        {
            allSockets = new List<NodeSocket>();
            CollectAllSockets(allSockets);
        }

        protected virtual void CollectAllSockets(List<NodeSocket> sockets)
        {
            CollectReflectionSockets<NodeInput>(sockets);
            CollectReflectionSockets<NodeOutput>(sockets);
        }

        public virtual bool CanConnect(NodeOutput from, NodeInput to)
        {
            return true;
        }

        public virtual NodeOutput<T> GetOutput<T>(string name) where T : IEquatable<T>
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
                if (name.Equals(sockets[i].name))
                    return sockets[i] as NodeOutput<T>;
            return null;
        }

        public NodeOutput GetOutput(string name)
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
                if (name.Equals(sockets[i].name))
                    if (sockets[i] is NodeOutput)
                        return sockets[i] as NodeOutput;
            return null;
        }

        private void CollectReflectionSockets<T>(List<NodeSocket> sockets) where T : NodeSocket
        {
            Type type = this.GetType();
            FieldInfo[] fieldInfos = type.GetFields();
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                Type fieldType = fieldInfos[i].FieldType;
                if (fieldType == typeof(T) || fieldType.IsSubclassOf(typeof(T)))
                {
                    NodeSocket socket = fieldInfos[i].GetValue(this) as NodeSocket;
                    if (socket == null)
                    {
                        socket = Activator.CreateInstance(fieldType) as NodeSocket;
                        fieldInfos[i].SetValue(this, socket);
                    }
                    socket.name = fieldInfos[i].Name;
                    socket.node = this;
                    sockets.Add(socket);

                }
            }
        }

        public static List<Node> all = new List<Node>();
        protected virtual void OnEnable()
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
            {
                sockets[i].OnEnable();
                var socket = sockets[i] as NodeInput;
                if (socket != null)
                    socket.onConnect += OnConnect;
            }
            all.Add(this);
            SetDirty();
        }

        protected virtual void OnDisable()
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
            {
                sockets[i].OnDisable();
                var socket = sockets[i] as NodeInput;
                if (socket != null)
                    socket.onConnect -= OnConnect;
            }
            all.Remove(this);
        }

        public void ResetOutputs()
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i] as NodeOutput;
                if (socket != null)
                    socket.Reset();
            }
        }

        public void ResetInputs()
        {
            var sockets = ListAllSockets();
            for (int i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i] as NodeInput;
                if (socket != null)
                    socket.Reset();
            }
        }

        public virtual void Process()
        {

        }

        [NonSerialized]
        public bool isDirty;

        public void SetDirty()
        {
            if (isDirty) return;
            isDirty = true;
            SignalManager.AddDirtyNode(this);
        }

        //TODO: only triggered when component was added not in play mode, need better solution to make it work in playmode as well.
        // if list is not empty adds node to node window.
        private void Reset()
        {
            DirtyNodes.nodes.Push(this);
        }

    }
}
