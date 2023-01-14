namespace NBG.DebugUI
{
    public struct Input
    {
        public bool up; // Moves selection up the item list
        public bool down; // Moves selection down the item list
        public bool left; // Switches (decrements) items
        public bool right; // Switches (increments) items

        public bool categoryLeft; // Switches (decrements) category
        public bool categoryRight; // Switches (decrements) category

        public bool ok; // Activates items
        //public bool back;
    }
}
