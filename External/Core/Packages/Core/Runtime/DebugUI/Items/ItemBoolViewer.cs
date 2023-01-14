using System;

namespace NBG.DebugUI
{
    internal class ItemBoolViewer : Item
    {
        public Func<bool> GetValue;
        public bool CurrentValue { get; private set; }

        public ItemBoolViewer(string label) : base(label)
        {
        }

        public override bool HasActivation => false;
        public override bool HasSwitching => false;
        public override string DisplayValue => CurrentValue.ToString().ToUpper();

        public override void OnUpdate()
        {
            if (GetValue != null)
                CurrentValue = GetValue();
        }
    }
}
