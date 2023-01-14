using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    internal class SearcherEndNode
    {
        internal NodeEntry entry;
        internal INodeObjectReference Reference => entry.reference;
        internal string name;
        internal List<(string segment, UnityEngine.Object relativeObj)> path;

        internal string ID;

        public SearcherEndNode(NodeEntry entry, string name, List<(string segment, UnityEngine.Object relativeObj)> path)
        {
            this.entry = entry;
            this.name = name;
            this.path = path;
        }
    }

    /// <summary>
    /// Node hierarchy tree based on node paths
    /// </summary>
    internal class SearcherData
    {
        public List<SearcherEndNode> searcherEndNodes = new List<SearcherEndNode>();

        public int disabledTypesCount = 0;

        public void AddItem(NodeEntry entry, List<(string segment, UnityEngine.Object relativeObj)> path, string name)
        {
            searcherEndNodes.Add(new SearcherEndNode(entry, name, path));
        }
    }
}
