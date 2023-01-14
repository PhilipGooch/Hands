using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public abstract class NodeSocket : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public Node node;
        [NonSerialized]
        public string name;

        [NonSerialized]
        public object value;
        public object initialValue;

        [SerializeField, HideInInspector]
        public string valSerialized;

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public abstract void Reset();
        public abstract void Render(Rect localPos);

        public void OnBeforeSerialize()
        {
            if (Application.isPlaying) return;
            if (initialValue == null)
            {
                valSerialized = "n";
                return;
            }
            var type = Type.GetTypeCode(initialValue.GetType());
            switch (type)
            {
                case TypeCode.Int32:
                    valSerialized = "i" + ((int)initialValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case TypeCode.Single:
                    valSerialized = "f" + ((float)initialValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case TypeCode.Boolean:
                    valSerialized = "b" + ((bool)initialValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    Debug.LogError("Socket type serialization is not implemented!");
                    break;
            }
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(valSerialized))
                return;
            char type = valSerialized[0];
            if (type == 'n')
                initialValue = null;
            else if (type == 'i')
                initialValue = int.Parse(valSerialized.Substring(1), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
            else if (type == 'f')
                initialValue = float.Parse(valSerialized.Substring(1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
            else if (type == 'b')
                initialValue = bool.Parse(valSerialized.Substring(1));
        }
    }
}
