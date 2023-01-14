namespace NBG.LogicGraph
{
    interface INodeOnInitialize
    {
        /// <summary>
        /// Called immediately after a node is created or deserialized.
        /// </summary>
        void OnInitialize();
    }
}
