using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public const string fileName = "SaveData";
    public const string fileExtension = ".json";

    [SerializeField]
    List<ChapterData> _chapterData;
    public List<ChapterData> chapterData
    {
        get
        {
            if (_chapterData == null)
                _chapterData = new List<ChapterData>();
            return _chapterData;
        }
        set
        {
            _chapterData = value;
            Save();
        }
    }

    public Action<string> onDataLoadFailedCallback;
    public Action<DataSerializationStatus> onDataSaveCallback;

    public void Save()
    {
        DataSerializationUtility.Save(this, fileName, fileExtension, onDataSaveCallback);
    }


    public void SaveLevelComplete(float time, string chapterId, string levelId)
    {

        ChapterData chapterData = GetChapterDataById(chapterId);
        if (chapterData == null)
        {
            chapterData = SaveNewChapter(chapterId);
        }

        LevelData levelData = chapterData.GetLevelDataById(levelId);
        if (levelData == null)
        {
            levelData = chapterData.SaveNewLevel(levelId);
        }

        if (time < levelData.bestTime || levelData.bestTime < 0)
            levelData.bestTime = time;

        levelData.completed = true;

        Save();
    }


    public ChapterData GetChapterDataById(string id)
    {
        ChapterData data = chapterData.Find(x => x.id.Equals(id));
        return data;
    }

    ChapterData SaveNewChapter(string id)
    {
        var data = new ChapterData(id);
        chapterData.Add(data);
        return data;
    }
}

[System.Serializable]
public class ChapterData
{
    [SerializeField]
    public string id;
    [SerializeField]
    public List<LevelData> levelData;

    public ChapterData(string id)
    {
        this.id = id;
        levelData = new List<LevelData>();
    }
    public LevelData GetLevelDataById(string id)
    {
        LevelData data = levelData.Find(x => x.id.Equals(id));
        return data;
    }
    public LevelData SaveNewLevel(string id)
    {
        var data = new LevelData(id);
        levelData.Add(data);
        return data;
    }
}
[System.Serializable]
public class LevelData
{
    [SerializeField]
    public string id;
    [SerializeField]
    public bool completed;
    [SerializeField]
    public float bestTime;

    public LevelData(string id)
    {
        this.id = id;
        completed = false;
        bestTime = -1;
    }
}
