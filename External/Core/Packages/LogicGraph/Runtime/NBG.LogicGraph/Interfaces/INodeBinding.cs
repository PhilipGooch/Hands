namespace NBG.LogicGraph
{
    interface INodeBinding : INodeObjectContext
    {
        /// <summary>
        /// Called immediately after a node is created or deserialized.
        /// </summary>
        void OnDeserializedBinding(UserlandBinding binding);
    }
}
