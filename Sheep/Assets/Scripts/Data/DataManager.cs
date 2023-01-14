using UnityEngine;

public class DataManager
{
    public static DataManager Instance { get; private set; }
    public static DataManager EnsureInitialized()
    {
        if (Instance == null)
        {
            Instance = new DataManager();
        }
        return Instance;
    }

    private SaveData _saveData;
    public SaveData saveData
    {
        get
        {
            if (_saveData == null)
            {
                DataSerializationUtility.Load<SaveData>(SaveData.fileName, SaveData.fileExtension, DataLoadCallback);
                _saveData.onDataSaveCallback += DataSaveCallback;
            }

            return _saveData;
        }
        set
        {
            _saveData = value;

            _saveData.Save();
        }
    }

    public void SaveLevelComplete(float time, string chapterId, string levelId)
    {
        saveData.SaveLevelComplete(time, chapterId, levelId);
    }

    void DataLoadCallback(SaveData data, DataSerializationStatus status)
    {
        switch (status.serializationStatus)
        {
            case SerializationStatus.Ok:
                _saveData = data;
                break;
            case SerializationStatus.MinorError:
                Debug.LogWarning($"Error recieved while loading save file. Error message: {status.msg}");
                break;
            case SerializationStatus.CriticalError:
                PlayerUIManager.Instance.DisplayNotification("Data load error", "<color=#f00000>status.msg</color>\nThe game will exit.", "Ok", Application.Quit);
                Debug.LogWarning($"CRITICAL Error recieved while loading save file. Error message: {status.msg}");
                break;

        }
    }

    void DataSaveCallback(DataSerializationStatus status)
    {
        switch (status.serializationStatus)
        {
            case SerializationStatus.Ok:
                break;
            case SerializationStatus.MinorError:
                Debug.LogWarning($"Error recieved while loading save file. Error message: {status.msg}");
                break;
            case SerializationStatus.CriticalError:
                PlayerUIManager.Instance.DisplayNotification("Data save error", "<color=#f00000>status.msg</color>\nThe game will exit.", "Ok", Application.Quit);
                Debug.LogWarning($"CRITICAL Error recieved while loading save file. Error message: {status.msg}");
                break;

        }
    }

    public float GetCampaignCompletion()
    {
        float completion = 0;
        int count = 0;

        foreach (var chapter in LevelManager.Instance.GetAllChapters())
        {
            completion += GetChapterCompletion(chapter);
            count++;
        }
        return completion / count;
    }
    //since only completed levels are saved its impossible to get completion just from the save data
    public float GetChapterCompletion(Chapter chapter)
    {
        int completed = 0;
        int notComepleted = 0;

        var chapterData = saveData.GetChapterDataById(chapter.id);
        if (chapterData != null)
        {
            foreach (var level in chapter.levels)
            {
                var lvlData = chapterData.GetLevelDataById(level.id);
                if (lvlData != null && lvlData.completed)
                    completed++;
                else
                    notComepleted++;
            }
        }
        else
            notComepleted += chapter.levels.Count;

        return (float)completed / (float)(completed + notComepleted);
    }
}
