using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.NodeGraph
{
    //[Serializable]
    //public class NodeGraphInput
    //{
    //    public string name;
    //    [HideInInspector]
    //    public NodeInput input = new NodeInput();
    //    [HideInInspector]
    //    public NodeOutput inputSocket;
    //}

    //[Serializable]
    //public class NodeGraphOutput
    //{
    //    public string name;
    //    [HideInInspector]
    //    public NodeInput outputSocket;
    //    [HideInInspector]
    //    public NodeOutput output;
    //}

    //Input and output type connected
    public enum SocketType
    {
        Float,
        Int,
        Bool
    }
    [Serializable]
    public class NodeGraphSocket
    {
        public string name;
        public SocketType type = SocketType.Float;
        [HideInInspector]
        [SerializeField]
        public NodeInput input;
        public NodeOutput output;
    }

    public class NodeGraph : Node
    {
        [HideInInspector]
        public Vector2 inputsPos = new Vector2(10, 10);

        [HideInInspector]
        public Vector2 outputsPos = new Vector2(10, 10);

        public List<NodeGraphSocket> inputs = new List<NodeGraphSocket>();
        public List<NodeGraphSocket> outputs = new List<NodeGraphSocket>();

        Type[] socketInTypes = new Type[] { typeof(NodeInputFloat), typeof(NodeInputInt), typeof(NodeInputBool) };
        Type[] socketOutTypes = new Type[] { typeof(NodeOutputFloat), typeof(NodeOutputInt), typeof(NodeOutputBool) };

        protected override void CollectAllSockets(List<NodeSocket> sockets)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (!string.IsNullOrEmpty(inputs[i].name))
                {
                    //in case type changes in the inspector or not initialized yet
                    //if (inputs[i].input == null || socketInTypes[(int)inputs[i].type] != inputs[i].input.GetType())
                    //    InitInput(ref inputs[i].input, inputs[i].type);
                    if (inputs[i].output == null || socketOutTypes[(int)inputs[i].type] != inputs[i].output.GetType())
                        InitOutput(out inputs[i].output, inputs[i].type);
                    if (inputs[i].input == null)
                        inputs[i].input = new NodeInput();
                    //if (inputs[i].output == null)
                    //    InitOutput(out inputs[i].output, inputs[i].type);

                    inputs[i].input.name = inputs[i].output.name = inputs[i].name;
                    inputs[i].input.node = inputs[i].output.node = this;
                    sockets.Add(inputs[i].input);
                    sockets.Add(inputs[i].output);
                }
            }
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!string.IsNullOrEmpty(outputs[i].name))
                {
                    //if (outputs[i].input == null || socketInTypes[(int)outputs[i].type] != outputs[i].GetType())
                    //    InitInput(ref outputs[i].input, outputs[i].type);
                    if (outputs[i].output == null)
                        InitOutput(out outputs[i].output, outputs[i].type);
                    if (outputs[i].input == null)
                        outputs[i].input = new NodeInput();
                    //if (inputs[i].output == null)
                    //    InitOutput(out inputs[i].output, inputs[i].type);
                    outputs[i].output.name = outputs[i].input.name = outputs[i].name;
                    outputs[i].output.node = outputs[i].input.node = this;
                    sockets.Add(outputs[i].input);
                    sockets.Add(outputs[i].output);
                }
            }
        }

        public override bool CanConnect(NodeOutput from, NodeInput to)
        {
            NodeGraphSocket socket = inputs.FirstOrDefault(s => s.input == to);
            if (socket != null)
                return socketOutTypes[(int)socket.type] == from.GetType();
            socket = outputs.FirstOrDefault(s => s.input == to);
            if (socket != null)
                return socketOutTypes[(int)socket.type] == from.GetType();
            return false;
        }

        void InitInput(ref NodeInput input, SocketType type)
        {
            NodeInput t;
            switch (type)
            {
                case SocketType.Float:
                    var temp = new NodeInputFloat();
                    if (input != null)
                    {
                        if (input.initialValue != null)
                            temp.initialValue = (float)input.initialValue;
                    }
                    t = temp;
                    break;
                case SocketType.Bool:
                    t = new NodeInputBool();
                    break;
                case SocketType.Int:
                    t = new NodeInputInt();
                    break;
                default:
                    t = null;
                    break;
            }

            if (input != null)
            {
                t.initialValue = input.initialValue;
                t.valSerialized = input.valSerialized;
                t.connectedNode = input.connectedNode;
                t.connectedSocket = input.connectedSocket;
            }
            input = t;
        }

        void InitOutput(out NodeOutput output, SocketType type)
        {
            switch (type)
            {
                case SocketType.Float:
                    output = new NodeOutputFloat();
                    break;
                case SocketType.Bool:
                    output = new NodeOutputBool();
                    break;
                case SocketType.Int:
                    output = new NodeOutputInt();
                    break;
                default:
                    output = null;
                    break;
            }
        }
        public override NodeOutput<T> GetOutput<T>(string name)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (name.Equals(inputs[i].name))
                    return inputs[i].output as NodeOutput<T>;
            }
            return null;
        }

        void SetValue(NodeGraphSocket socket)
        {
            NodeOutput connectedOutput = socket.input.GetConnectedOutput();
            if (connectedOutput == null) return;
            switch (socket.type)
            {
                case SocketType.Float:
                    (socket.output as NodeOutputFloat).SetValue((float)connectedOutput.GetValue());
                    break;
                case SocketType.Bool:
                    (socket.output as NodeOutputBool).SetValue((bool)socket.input.GetConnectedOutput().GetValue());
                    break;
                case SocketType.Int:
                    (socket.output as NodeOutputInt).SetValue((int)socket.input.GetConnectedOutput().GetValue());
                    break;
            }
        }

        public override void Process()
        {
            base.Process();
            for (int i = 0; i < inputs.Count; i++)
                SetValue(inputs[i]);
            for (int i = 0; i < outputs.Count; i++)
                SetValue(outputs[i]);
        }
    }
}
