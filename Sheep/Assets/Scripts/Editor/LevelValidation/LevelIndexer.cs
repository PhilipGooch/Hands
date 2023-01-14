using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// TODO: improve level handling
public class LevelSheep : ILevel
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

public class LevelIndexerSheep : ILevelIndexer
{
    const string kLevelManagerPath = "Assets/Resources/Levels.asset";

    string[] _levelNames;
    string[] _levelBaseScenes;
    int[] _runLevelValidationTestsAtImportance;

    LevelSheep _currentLevel;

    public LevelIndexerSheep()
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
                    _currentLevel = new LevelSheep();
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

    public void OnValidationTestInstantiated(ValidationTest test)
    {
        if (test.GetType() == typeof(Recoil.Editor.ValidateUniformScale))
            test.Caps &= ~ValidationTestCaps.Strict; // This project does not fully use Recoil
    }

    public void OpenLevel(string baseScenePath)
    {
        var scene = EditorSceneManager.OpenScene(baseScenePath, OpenSceneMode.Single);
        
        _currentLevel ??= new LevelSheep();
        _currentLevel._scene = scene;
    }

    public void Refresh()
    {
        var names = new List<string>();
        var baseScenes = new List<string>();

        var lm = AssetDatabase.LoadAssetAtPath<LevelHolder>(kLevelManagerPath);
        foreach (var chapter in lm.GetAllChapters())
        {
            foreach (var level in chapter.levels)
            {
                var name = $"{chapter.name}: {level.name}";
                names.Add(name);

                baseScenes.Add(level.scene.ScenePath);
            }
        }

        _levelBaseScenes = baseScenes.ToArray();
        _levelNames = names.ToArray();
        // All scenes inside Levels asset should be in working condition and without errors
        _runLevelValidationTestsAtImportance = names.Select(x => int.MaxValue).ToArray();
    }
}
