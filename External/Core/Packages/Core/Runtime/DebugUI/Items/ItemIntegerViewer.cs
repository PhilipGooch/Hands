using System;

namespace NBG.DebugUI
{
    internal class ItemIntegerViewer : Item
    {
        public Func<int> GetValue;
        public int CurrentValue { get; private set; }

        public ItemIntegerViewer(string label) : base(label)
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
