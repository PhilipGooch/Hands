using System;

namespace NBG.DebugUI
{
    internal class ItemAction : Item
    {
        public Action OnPress;

        public ItemAction(string label) : base(label)
        {
        }

        public override bool HasActivation => true;
        public override bool HasSwitching => false;

        public override string DisplayValue => null;

        public override void OnActivate()
        {
            OnPress?.Invoke();
        }
    }
}
