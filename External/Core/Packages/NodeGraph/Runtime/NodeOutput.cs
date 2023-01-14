using System;

namespace NBG.NodeGraph
{
    [Serializable]
    public abstract class NodeOutput : NodeSocket
    {
        public Action<object> onValueChanged = delegate { };

        public abstract object GetValue();

    }

    [Serializable]
    public abstract class NodeOutput<T> : NodeOutput where T : IEquatable<T>
    {
        public new T value
        {
            get
            {
                if (base.value == null)
                    return default;
                return (T)base.value;
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
                if (base.initialValue == null)
                    return default;
                return (T)base.initialValue;
            }
            set
            {
                base.initialValue = value;
            }
        }

        public void SetValue(T value)
        {
            if (value.Equals(this.value))
                return;
            this.value = value;
            onValueChanged(value);
        }

        public override void Reset()
        {
            value = initialValue;
        }
        public override object GetValue()
        {
            return value;
        }
    }
}
