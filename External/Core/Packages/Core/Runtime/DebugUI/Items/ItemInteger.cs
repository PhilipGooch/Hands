using System;

namespace NBG.DebugUI
{
    internal class ItemInteger : Item
    {
        public Func<int> GetValue;
        public Action<int> SetValue;
        public int Step = 1;
        public int MinValue = int.MinValue;
        public int MaxValue = int.MaxValue;

        public int CurrentValue { get; private set; }
        
        public ItemInteger(string label) : base(label)
        {
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => true;
        public override bool HoldChange => true;

        public override string DisplayValue => CurrentValue.ToString();

        public override bool OnIncrement(uint speed)
        {
            var desiredStep = (int)(Step * speed);
            var newValue = Math.Min(MaxValue, CurrentValue + desiredStep);
            if (CurrentValue != newValue)
            {
                CurrentValue = newValue;
                SetValue?.Invoke(CurrentValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool OnDecrement(uint speed)
        {
            var desiredStep = (int)(Step * speed);
            var newValue = Math.Max(MinValue, CurrentValue - desiredStep);
            if (CurrentValue != newValue)
            {
                CurrentValue = newValue;
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
