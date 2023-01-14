using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    enum SearcherVisualType
    {
        Hierarchy,
        List
    }

    /// <summary>
    /// Searcher Utility class (helps with searcher data creation)
    /// </summary>
    internal static class SearcherUtils
    {
        internal static SearcherData GetHierarchyNodesSearcherData(IEnumerable<NodeEntry> nodeTypes, LogicGraphPlayerEditor activeGraph, SearcherVisualType visualType)
        {
            var searcherData = new SearcherData();
            HashSet<string> checkedTypes = new HashSet<string>();

            foreach (NodeEntry nodeType in nodeTypes)
            {
                if (nodeType.reference != null)
                {
                    var isTypeVisible = true;
                    if (nodeType.bindingType != null)
                    {
                        isTypeVisible = EditorStateManager.GetSearcherTypeVisibility(nodeType.bindingType.FullName);
                        if (!isTypeVisible && nodeType.bindingType != null)
                            checkedTypes.Add(nodeType.bindingType.FullName);
                    }

                    if (nodeType.bindingType == null || isTypeVisible)
                    {
                        switch (visualType)
                        {
                            case SearcherVisualType.Hierarchy:
                                var hierarchyPath = nodeType.reference.GetPathHierarchyType(activeGraph.logicGraphPlayer.gameObject);
                                searcherData.AddItem(nodeType, hierarchyPath, nodeType.description);
                                break;
                            case SearcherVisualType.List:
                                var listPath = nodeType.reference.GetPathListType(nodeType);
                                searcherData.AddItem(nodeType, listPath, nodeType.reference.Target.name);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            searcherData.disabledTypesCount = checkedTypes.Count;

            return searcherData;
        }

        internal static SearcherData GetBuiltInNodesSearcherData(IEnumerable<NodeEntry> nodeTypes)
        {
            var searcherData = new SearcherData();

            foreach (var nodeType in nodeTypes)
            {
                if (nodeType.reference == null && !string.IsNullOrEmpty(nodeType.categoryPath))
                {
                    var splitPath = nodeType.categoryPath.Split('/').ToList();

                    List<(string segment, UnityEngine.Object relativeObj)> path = new List<(string segment, UnityEngine.Object relativeObj)>();
                    foreach (var split in splitPath)
                    {
                        path.Add((split, null));
                    }

                    searcherData.AddItem(nodeType, path, nodeType.description);
                }
            }

            return searcherData;
        }

        internal static string PathToID(List<(string segment, UnityEngine.Object relativeObj)> path, int depth)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i <= depth; i++)
            {
                stringBuilder.Append(path[i].segment);
                stringBuilder.Append("/");
            }

            var pathEndObj = path[depth].relativeObj;
            if (pathEndObj != null)
                stringBuilder.Append(pathEndObj.GetInstanceID());

            return stringBuilder.ToString();
        }

        private static readonly Regex camelCase = new Regex(@"(?<!^)(?=[A-Z])");
        public static string[] SplitCamelCase(this string source)
        {
            return camelCase.Split(source);
        }

        private static readonly Regex whitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(this string input, string replacement)
        {
            return whitespace.Replace(input, replacement);
        }
    }

    /// <summary>
    /// Interface to implement searcher menu
    /// </summary>
    internal interface Searcher
    {
        public void ShowSearcher(Vector2 position);

        public List<SearcherData> GetSearcherData();
    }
}
