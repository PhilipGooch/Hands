using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NBG.Core.Editor
{
    // Level indexing interface (game specific)
    public interface ILevelIndexer
    {
        // Currently loaded level
        // Only the base scene is expected to be loaded, throw otherwise
        ILevel CurrentLevel { get; }

        // Base scene paths for all levels
        string[] LevelBaseScenes { get; }
        // Level names, in the same order as <LevelBaseScenes>
        string[] LevelNames { get; }
        // Importance specifying which level validation tests should be run, in the same order as <LevelBaseScenes>.
        // Test will run if Test.Importance < RunLevelValidationTestsAtImportance.
        // Specify -1 to disable tests.
        int[] RunLevelValidationTestsAtImportance { get; }

        // Returns an user-friendly description of the current level load status
        string GetLevelLoadStatus(out bool error);
        // Opens a level provided a path from <LevelBaseScenes>
        void OpenLevel(string baseScenePath);

        // Refresh the level list
        // This does not guarantee to retain the item order
        void Refresh();

        // Allows game specific type overrides
        // For when your game does not use all of the features of the library
        void OnValidationTestInstantiated(ValidationTest test);
    }

    internal class LooseScenesProxyLevel : ILevel
    {
        public Scene BaseScene => EditorSceneManager.GetActiveScene();

        public IEnumerable<Scene> Sections
        {
            get
            {
                var count = EditorSceneManager.loadedSceneCount;
                for (int i = 0; i < count; ++i)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene == EditorSceneManager.GetActiveScene())
                        continue; // Skip active scene as it is already provided as BaseScene
                    if (!scene.isLoaded)
                        continue; // Skip not loaded scenes

                    yield return scene;
                }
                yield break;
            }
        }
    }
}
