using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CoreSample
{
    public class LevelSample : ILevel
    {
        public Scene _scene;

        public Scene BaseScene
        {
            get
            {
                return _scene;
            }
        }

        public IEnumerable<Scene> Sections
        {
            get
            {
                yield break;
            }
        }
    }

    public class LevelIndexerReference : ILevelIndexer
    {
        LevelSample _currentLevel = null;

        string[] _levelNames;
        string[] _levelBaseScenes;
        int[] _runLevelValidationTestsAtImportance;

        public LevelIndexerReference()
        {
            Refresh();
        }

        public ILevel CurrentLevel
        {
            get
            {
                if (_currentLevel == null || _currentLevel.BaseScene == null || !_currentLevel.BaseScene.IsValid())
                {
                    var scene = EditorSceneManager.GetActiveScene();
                    var idx = ArrayUtility.IndexOf(_levelBaseScenes, scene.path);
                    if (idx == -1)
                    {
                        throw new System.InvalidOperationException("Could not determine current level.");
                    }
                    else
                    {
                        _currentLevel = new LevelSample();
                        _currentLevel._scene = scene;
                    }
                }
                return _currentLevel;
            }
        }

        public string[] LevelBaseScenes => _levelBaseScenes;

        public string[] LevelNames => _levelNames;

        public int[] RunLevelValidationTestsAtImportance => _runLevelValidationTestsAtImportance;

        public string GetLevelLoadStatus(out bool error)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var idx = ArrayUtility.IndexOf(_levelBaseScenes, scene.path);
            if (idx == -1)
            {
                error = true;
                return $"Scene {scene.path} is not in the known level list.";
            }
            else
            {
                error = false;
                return $"Loaded '{_levelNames[idx]}' from '{scene.path}'.";
            }
        }

        public void OpenLevel(string baseScenePath)
        {
            var scene = new Scene();

            if (EditorSceneManager.loadedSceneCount == 1)
            {
                scene = EditorSceneManager.GetSceneAt(0);
            }

            if (EditorSceneManager.loadedSceneCount != 1 || scene.path != baseScenePath)
            {
                scene = EditorSceneManager.OpenScene(baseScenePath, OpenSceneMode.Single);
            }

            _currentLevel ??= new LevelSample();
            _currentLevel._scene = scene;
        }

        public void Refresh()
        {
            var names = new List<string>();
            var baseScenes = new List<string>();

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var index = i;
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                if (path.Contains("bootloader.unity")) // What else can we do?
                    continue;

                names.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                baseScenes.Add(path);
            }

            _levelBaseScenes = baseScenes.ToArray();
            _levelNames = names.ToArray();
            // All scenes should be in working condition and without errors
            _runLevelValidationTestsAtImportance = names.Select(x => int.MaxValue).ToArray();
        }

        public void OnValidationTestInstantiated(ValidationTest test)
        {
            // Example of overriding validation test caps:
            //if (test.GetType() == typeof(NBG.Core.ObjectIdDatabase.Editor.ValidateObjectIdDatabase))
            //{
            //    test.Caps &= ~ValidationTestCaps.Strict;
            //}

            // Don't force some strict validation tests in CoreSample
            if (test.GetType() == typeof(NBG.Core.ObjectIdDatabase.Editor.ValidateObjectIdDatabase))
            {
                test.Caps &= ~ValidationTestCaps.Strict;
            }

            if (test.GetType() == typeof(Recoil.Editor.ValidateUniformScale))
            {
                test.Caps &= ~ValidationTestCaps.Strict;
            }
        }
    }
}
