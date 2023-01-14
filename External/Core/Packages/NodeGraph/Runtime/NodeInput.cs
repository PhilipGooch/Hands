using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeInput : NodeSocket
    {
        public Node connectedNode;
        public string connectedSocket;

        public virtual bool CanConnect(NodeOutput output) { return node.CanConnect(output, this); }

        public Action<NodeOutput> onConnect = delegate { };

        public override void OnEnable()
        {
            var connectedOutput = GetConnectedOutput();
            Connect(connectedOutput);
        }

        public override void OnDisable()
        {
            var connectedOutput = GetConnectedOutput();
            if (connectedOutput != null)
                connectedOutput.onValueChanged -= ConnectedOutput_onValueChanged;
        }

        protected virtual void ConnectedOutput_onValueChanged(object value)
        {
            if (value != null && value.Equals(this.value))
                return;

            this.value = value;
            node.SetDirty();
        }

        public void Connect(NodeOutput output)
        {
            OnDisable();
            if (output == null)
            {
                connectedNode = null;
                connectedSocket = null;
                value = initialValue;
            }
            else
            {
                connectedNode = output.node;
                connectedSocket = output.name;
                ConnectedOutput_onValueChanged(output.GetValue());
                output.onValueChanged += ConnectedOutput_onValueChanged;
            }
            onConnect(output);
        }

        public NodeOutput GetConnectedOutput()
        {
            if (connectedNode == null) return null;
            return connectedNode.GetOutput(connectedSocket);
        }

        //Default render
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width = localPos.width;

            GUI.Label(labelRect, name + ":" + value);
        }

        public override void Reset()
        {
            var connectedOutput = GetConnectedOutput();
            value = connectedOutput != null ? connectedOutput.value : initialValue;
        }

    }

    [Serializable]
    public abstract class NodeInput<T> : NodeInput where T : IEquatable<T>
    {
        public new T value
        {
            get
            {
                if (base.value is T) //in case null or type was changed
                    return (T)base.value;
                return default;
            }
            set
            {
                base.value = value;
            }
        }

        public new T initialValue
        {
            get
            {
                if (base.initialValue is T)
                    return (T)base.initialValue;
                return default;
            }
            set
            {
                base.initialValue = value;
            }
        }

        public new NodeOutput<T> GetConnectedOutput()
        {
            if (connectedNode == null) return null;
            return connectedNode.GetOutput<T>(connectedSocket);
        }
        public override void Reset()
        {
            var connectedOutput = GetConnectedOutput();
            value = connectedOutput != null ? connectedOutput.value : initialValue;
        }
    }
}
