namespace NBG.LogicGraph
{
    interface INodeOnAddToGraph
    {
        /// <summary>
        /// Called immediately after a new node is added to a graph.
        /// </summary>
        void OnAddToGraph();
    }
}
