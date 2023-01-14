using UnityEditor;

namespace NBG.SuperCombiner
{
    [InitializeOnLoadAttribute]
    internal static class SuperCombinerSceneChecker
    {
        static SuperCombinerSceneChecker()
        {
            EditorApplication.playModeStateChanged += CheckPlayModeState;
        }

        private static void CheckPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                UpdateSuperCombinerLightmapIndexes();
        }

        private static void UpdateSuperCombinerLightmapIndexes()
        {
            SuperCombiner.ApplyAllLighmapIndices();
        }
    }
}
