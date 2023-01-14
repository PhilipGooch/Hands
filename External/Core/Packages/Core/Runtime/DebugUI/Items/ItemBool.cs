using System;

namespace NBG.DebugUI
{
    internal class ItemBool : Item
    {
        public Func<bool> GetValue;
        public Action<bool> SetValue;

        public bool CurrentValue { get; private set; }

        public ItemBool(string label) : base(label)
        {
        }

        public override bool HasActivation => true;
        public override bool HasSwitching => true;
        public override string DisplayValue => CurrentValue.ToString().ToUpper();

        public override void OnActivate()
        {
            CurrentValue = !CurrentValue;
            SetValue?.Invoke(CurrentValue);
        }

        public override bool OnIncrement(uint speed_unused) => OnChange();
        public override bool OnDecrement(uint speed_unused) => OnChange();
        private bool OnChange()
        {
            OnActivate();
            return true;
        }

        public override void OnUpdate()
        {
            if (GetValue != null)
                CurrentValue = GetValue();
        }
    }
}
