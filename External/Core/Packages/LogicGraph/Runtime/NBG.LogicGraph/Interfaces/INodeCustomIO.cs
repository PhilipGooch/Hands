namespace NBG.LogicGraph
{
    interface INodeCustomIO
    {
        public enum Type
        {
            Inputs,
            Outputs
        }

        bool CanAddAndRemove { get; }
        Type CustomIOType { get; }
        void AddCustomIO(string name, VariableType type);
        void RemoveCustomIO(int index);
        void UpdateCustomIO(int index, string name, VariableType type);
        string GetCustomIOName(int index);
        VariableType GetCustomIOType(int index);
    }
}
