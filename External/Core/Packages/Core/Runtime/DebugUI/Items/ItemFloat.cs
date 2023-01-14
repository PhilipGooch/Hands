using System;

namespace NBG.DebugUI
{
    internal class ItemFloat : Item
    {
        public Func<double> GetValue;
        public Action<double> SetValue;
        public double Step
        {
            get => _step;
            set
            {
                _step = value;
                _format = DetermineFormatForPrecision(_step);
            }
        }
        public double MinValue = float.MinValue; // Clamp to float range by default
        public double MaxValue = float.MaxValue; // Clamp to float range by default

        public double CurrentValue { get; private set; }
        
        public ItemFloat(string label) : base(label)
        {
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => true;
        public override bool HoldChange => true;

        public override string DisplayValue => CurrentValue.ToString(_format);

        private double _step;
        private string _format;

        public override bool OnIncrement(uint speed)
        {
            var desiredStep = (double)(Step * speed);
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
            var desiredStep = (double)(Step * speed);
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

        private static string DetermineFormatForPrecision(double value)
        {
            const int min = 2;
            const int max = 12;

            int digits = 0;
            double frac = value % 1;
            while (frac > 0.0 && frac < 1.0 && digits < max)
            {
                frac *= 10;
                digits++;
            }

            if (digits < min)
                digits = min;
            
            return $"F{digits}";
        }
    }
}
