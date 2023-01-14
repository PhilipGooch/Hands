namespace NBG.LogicGraph
{
    interface INodeValidation
    {
        /// <summary>
        /// Runs node diagnostics.
        /// </summary>
        /// <returns>Error message or null</returns>
        string CheckForErrors();
    }
}
