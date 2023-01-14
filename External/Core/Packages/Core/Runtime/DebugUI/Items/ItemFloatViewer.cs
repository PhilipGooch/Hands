using System;

namespace NBG.DebugUI
{
    internal class ItemFloatViewer : Item
    {
        public Func<double> GetValue;
        public double CurrentValue { get; private set; }

        public ItemFloatViewer(string label) : base(label)
        {
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => false;
        public override string DisplayValue => CurrentValue.ToString();

        public override void OnUpdate()
        {
            if (GetValue != null)
                CurrentValue = GetValue();
        }
    }
}
