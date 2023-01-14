using System;
using System.Collections.Generic;

namespace NBG.DebugUI
{
    internal abstract class Item : IDebugItem
    {
        public string Label { get; private set; }

        public const int DefaultPriority = 0;
        public int Priority { get; set; } = DefaultPriority;
        public bool Enabled { get; set; } = true;

        private static long _lastCreationId = 0;
        private readonly long _creationId;

        public Item(string label)
        {
            Label = label;

            _creationId = ++_lastCreationId;
        }

        public abstract bool HasActivation { get; }
        public abstract bool HasSwitching { get; }
        public virtual bool HoldChange => false;

        // Get a formatted value to display
        // Returns null if there is no value to display
        public abstract string DisplayValue { get; }

        // Called when this item is activated
        public virtual void OnActivate() { }

        // Called when this item is switched (incremented)
        // <speed> is an optional step multiplier for better UX
        public virtual bool OnIncrement(uint speed = 1) { throw new NotImplementedException(); }

        // Called when this item is switched (decremented)
        // <speed> is an optional step multiplier for better UX
        public virtual bool OnDecrement(uint speed = 1) { throw new NotImplementedException(); }

        public virtual void OnUpdate() { }

        internal class PriorityComparer : IComparer<Item>
        {
            int IComparer<Item>.Compare(Item x, Item y)
            {
                if (Object.ReferenceEquals(x, y))
                    return 0;

                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                // First sort by priority
                int result = x.Priority.CompareTo(y.Priority);
                if (result != 0)
                    return result;

                // Then by registration order
                return x._creationId.CompareTo(y._creationId);
            }
        }
    }
}
