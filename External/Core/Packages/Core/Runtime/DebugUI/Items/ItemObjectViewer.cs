using System;

namespace NBG.DebugUI
{
    internal class ItemObjectViewer : Item
    {
        public Func<object> GetValue;
        public object CurrentValue { get; private set; }

        public ItemObjectViewer(string label) : base(label)
        {
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => false;
        public override string DisplayValue => (CurrentValue != null ? CurrentValue.ToString() : "null");

        public override void OnUpdate()
        {
            if (GetValue != null)
                CurrentValue = GetValue();
        }
    }
}
