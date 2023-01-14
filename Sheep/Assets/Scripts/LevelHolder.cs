using Malee.List;
using NBG.Core;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class LevelList : ReorderableArray<Level> { }

[Serializable]
public class Chapter
{
    public string name;
    [Reorderable]
    public LevelList levels;
    public Button3D selectionPrefab;
    [HideInInspector]
    public string id;
}

[Serializable]
public class Level
{
    public string name;
    public SceneField scene;
    public Button3D selectionPrefab;
    [HideInInspector]
    public string id;

}

[CreateAssetMenu(fileName = "LevelHolder", menuName = "Level Holder", order = 1)]
public class LevelHolder : ScriptableObject
{
    [SerializeField]
    Chapter[] chapters;
    [SerializeField]
    SceneField mainMenuScene;
    [SerializeField]
    SceneField bootstrapScene;

    public SceneField MainMenu => mainMenuScene;
    public SceneField BoostrapScene => bootstrapScene;

    //https://forum.unity.com/threads/associating-an-assetreference-with-its-asset-address.773057/

#if UNITY_EDITOR
    void OnValidate()
    {
        foreach (var chapter in chapters)
        {
            if (string.IsNullOrEmpty(chapter.id))
            {
                chapter.id = chapter.name;
                Debug.LogWarning("<color=red>Chapter id not set! Using chapter name as ID</color>", this);
            }

            foreach (var level in chapter.levels)
            {
                level.scene.Verify();

                if (level.scene != null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath(level.scene.ScenePath, typeof(SceneAsset));
                    if (asset != null)
                    {
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localID))
                        {
                            level.id = guid;
                        }
                    }
                    else
                    {
                        // Loading asset failed, but scene is not null. This happens when switching branches.
                    }
                }
                else
                {
                    Debug.LogError($"scene asset is null: {level.scene.SceneName}", this);
                }
            }
        }

        mainMenuScene.Verify();
    }
#endif

    public int GetChapterCount()
    {
        return chapters.Length;
    }

    public int GetLevelCount()
    {
        var levels = 0;
        foreach (var chapter in chapters)
        {
            levels += chapter.levels.Length;
        }
        return levels;
    }

    public IEnumerable<Chapter> GetAllChapters()
    {
        foreach (var chapter in chapters)
        {
            yield return chapter;
        }
    }

    public IEnumerable<Level> GetAllLevels()
    {
        foreach (var chapter in chapters)
        {
            foreach (var level in chapter.levels)
            {
                yield return level;
            }
        }
    }

    public SceneField GetSceneAtIndex(int index)
    {
        var level = GetLevelAtIndex(index);
        if (level != null)
        {
            return level.scene;
        }
        else
        {
            return null;
        }
    }

    public Level GetLevelAtIndex(int index)
    {
        if (index >= 0)
        {
            var levelCount = GetLevelCount();
            index = index % levelCount;
            foreach (var chapter in chapters)
            {
                if (chapter.levels.Length < index)
                {
                    index -= chapter.levels.Length;
                }
                else
                {
                    return chapter.levels[index];
                }
            }
        }
        return null;
    }

    public Chapter GetChapter(int chapterIndex)
    {
        return chapters[chapterIndex];
    }

    public Chapter GetChapterAtLevelIndex(int levelIndex)
    {
        return GetChapterDataAtLevelIndex(levelIndex).chapter;
    }

    public int GetChapterIndexAtLevelIndex(int levelIndex)
    {
        return GetChapterDataAtLevelIndex(levelIndex).chapterID;
    }

    (int chapterID, Chapter chapter) GetChapterDataAtLevelIndex(int levelIndex)
    {
        if (levelIndex >= 0)
        {
            levelIndex = levelIndex % GetLevelCount();
            for (int i = 0; i < chapters.Length; i++)
            {
                var chapter = chapters[i];
                if (chapter.levels.Count > levelIndex)
                {
                    return (i, chapter);
                  
                }
                levelIndex -= chapter.levels.Count;
            }
        }

        return (-1, null);
    }
}
