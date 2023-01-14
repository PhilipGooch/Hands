using UnityEditor;
using UnityEngine;

namespace CoreSample.Base
{
    public class BootloaderEditor
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInit()
        {
            EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;
        }

        private static void OnPlaymodeChanged(UnityEditor.PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    {
                        Bootloader.DestroyIfAutoCreated();
                    }
                    break;
            }
        }
    }
}
