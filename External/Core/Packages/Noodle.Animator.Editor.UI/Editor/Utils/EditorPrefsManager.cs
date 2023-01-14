using UnityEditor;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Saves UI preferences 
    /// </summary>
    internal static class EditorPrefsManager
    {
        private const string k_keyPrefix = "DataMinerEditorPrefsKeyPrefix";

        private const string k_lastEditedAniamtionKey = k_keyPrefix + "graphsHeightKey";
        private const string k_defaultLastEditedAniamtionPath = "";

        internal static void SaveLastEditedAniamtion(string path)
        {
            EditorPrefs.SetString(k_lastEditedAniamtionKey, path);
        }

        internal static string GetLastEditedAnimationPath()
        {
            return EditorPrefs.GetString(k_lastEditedAniamtionKey, k_defaultLastEditedAniamtionPath);
        }
    }
}
