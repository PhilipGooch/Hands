using UnityEditor;

namespace NBG.Core.DataMining
{
    public static class DataMinerEditorPrefs
    {
        const string k_keyPrefix = "DataMinerEditorPrefsKeyPrefix";

        const string k_graphsHeightKey = "graphsHeightKey";
        const int k_defaultGraphsHeight = 200;

        const string k_msLineHeightKey = "msLineHeightKey";
        const int k_defaultMsLineHeight = 16;

        const string k_columnWidthKey = "columnWidthKey";
        const int k_defaultColumnWidth = 10;

        const string k_msCapKey = "msCapKey";
        const int k_defaultMsCap = 40;

        public static void SaveGraphsHeight(int newHeight)
        {
            EditorPrefs.SetInt(k_keyPrefix + k_graphsHeightKey, newHeight);
        }

        public static int GetGraphsHeight()
        {
            string key = k_keyPrefix + k_graphsHeightKey;
            if (EditorPrefs.HasKey(key))
                return (EditorPrefs.GetInt(key));
            else
            {
                SaveGraphsHeight(k_defaultGraphsHeight);
                return k_defaultGraphsHeight;
            }
        }

        public static void SaveMsLineHeight(int newHeight)
        {
            EditorPrefs.SetInt(k_keyPrefix + k_msLineHeightKey, newHeight);
        }
        public static int GetMsLineHeight()
        {
            string key = k_keyPrefix + k_msLineHeightKey;
            if (EditorPrefs.HasKey(key))
                return (EditorPrefs.GetInt(key));
            else
            {
                SaveMsLineHeight(k_defaultMsLineHeight);
                return k_defaultMsLineHeight;
            }
        }
        public static void SaveMsCap(int newCap)
        {
            EditorPrefs.SetInt(k_keyPrefix + k_msCapKey, newCap);
        }
        public static int GetMsCap()
        {
            string key = k_keyPrefix + k_msCapKey;
            if (EditorPrefs.HasKey(key))
                return (EditorPrefs.GetInt(key));
            else
            {
                SaveColumnWidth(k_defaultMsCap);
                return k_defaultMsCap;
            }
        }
        public static void SaveColumnWidth(int newWidth)
        {
            EditorPrefs.SetInt(k_keyPrefix + k_columnWidthKey, newWidth);
        }
        public static int GetColumnWidth()
        {
            string key = k_keyPrefix + k_columnWidthKey;
            if (EditorPrefs.HasKey(key))
                return (EditorPrefs.GetInt(key));
            else
            {
                SaveColumnWidth(k_defaultColumnWidth);
                return k_defaultColumnWidth;
            }
        }
    }
}
