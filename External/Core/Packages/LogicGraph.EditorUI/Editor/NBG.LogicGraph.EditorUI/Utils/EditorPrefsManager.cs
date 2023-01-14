using UnityEditor;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Saves UI preferences 
    /// </summary>
    internal static class EditorPrefsManager
    {
        private const string kKeyPrefix = "LogicGraphUIEditorPrefsKeyPrefix";

        private const string kMinimapVisibleKey = kKeyPrefix + "kMinimapVisibleKey";
        private const string kHierarchySearcherVisibleKey = kKeyPrefix + "kHierarchySearcherVisibleKey";
        private const string kBuiltinSearcherVisibleKey = kKeyPrefix + "kBuiltinSearcherVisibleKey";
        private const string kBlackboardVisibleKey = kKeyPrefix + "kBlackboardVisibleKey";
        private const string kInspectorVisibleKey = kKeyPrefix + "kInspectorVisibleKey";

        internal static bool MinimapVisible
        {
            get
            {
                return EditorPrefs.GetBool(kMinimapVisibleKey, true);
            }
            set
            {
                EditorPrefs.SetBool(kMinimapVisibleKey, value);
            }
        }

        internal static bool HierarchySearcherVisible
        {
            get
            {
                return EditorPrefs.GetBool(kHierarchySearcherVisibleKey, true);
            }
            set
            {
                EditorPrefs.SetBool(kHierarchySearcherVisibleKey, value);
            }
        }

        internal static bool BuiltinSearcherVisible
        {
            get
            {
                return EditorPrefs.GetBool(kBuiltinSearcherVisibleKey, true);
            }
            set
            {
                EditorPrefs.SetBool(kBuiltinSearcherVisibleKey, value);
            }
        }

        internal static bool BlackboardVisible
        {
            get
            {
                return EditorPrefs.GetBool(kBlackboardVisibleKey, true);
            }
            set
            {
                EditorPrefs.SetBool(kBlackboardVisibleKey, value);
            }
        }

        internal static bool InspectorVisible
        {
            get
            {
                return EditorPrefs.GetBool(kInspectorVisibleKey, true);
            }
            set
            {
                EditorPrefs.SetBool(kInspectorVisibleKey, value);
            }
        }
    }
}
