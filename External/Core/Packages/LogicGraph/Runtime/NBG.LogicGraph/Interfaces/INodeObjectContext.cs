namespace NBG.LogicGraph
{
    internal interface INodeObjectContext
    {
        /// <summary>
        /// Set immediately after a node is created or deserialized.
        /// </summary>
        UnityEngine.Object ObjectContext { get; set; }
    }
}
