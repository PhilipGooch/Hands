using System;
using UnityEngine;

namespace NBG.DebugUI
{
    internal class ItemEnum : Item
    {
        public Type EnumType { get; private set; }

        public Func<Enum> GetValue;
        public Action<Enum> SetValue;

        public Enum CurrentValue { get; private set; }

        string[] _names;
        Array _values;

        public ItemEnum(string label, Type enumType) : base(label)
        {
            Debug.Assert(enumType.IsEnum);
            EnumType = enumType;

            _names = Enum.GetNames(enumType);
            if (_names.Length == 0)
                throw new InvalidOperationException($"DebugUI can't use enum {enumType.Name} with no elements.");
            
            _values = Enum.GetValues(enumType);
            CurrentValue = (Enum)_values.GetValue(0);
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => true;

        public override string DisplayValue => CurrentValue.ToString();

        public override bool OnIncrement(uint speed_unused)
        {
            var currentIndex = Array.IndexOf(_values, CurrentValue);
            Debug.Assert(currentIndex != -1);

            var newIndex = Math.Min(_names.Length - 1, currentIndex + 1);
            if (currentIndex != newIndex)
            {
                CurrentValue = (Enum)_values.GetValue(newIndex);
                SetValue?.Invoke(CurrentValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool OnDecrement(uint speed_unused)
        {
            var currentIndex = Array.IndexOf(_values, CurrentValue);
            Debug.Assert(currentIndex != -1);

            var newIndex = Math.Max(0, currentIndex - 1);
            if (currentIndex != newIndex)
            {
                CurrentValue = (Enum)_values.GetValue(newIndex);
                SetValue?.Invoke(CurrentValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void OnUpdate()
        {
            if (GetValue != null)
                CurrentValue = GetValue();
        }
    }
}
