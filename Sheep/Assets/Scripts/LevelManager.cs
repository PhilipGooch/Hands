using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using NBG.Core;

public class LevelManager : MonoBehaviour
{
    public static event System.Action<Scene> onSceneLoaded;
    public static event System.Action<Scene> onSceneUnloaded;

    LevelHolder levelHolder;

    string[] levelKeys;
    string[] LevelKeys
    {
        get
        {
            if (levelKeys == null || levelKeys.Length == 0)
            {
                SetLevelKeys();
            }

            return levelKeys;
        }
    }


    [HideInInspector]
    public Level lastLevel = null;
    [HideInInspector]
    public int lastChapterId = -1;

    public int chaptersCount => levelHolder.GetChapterCount();

    public static LevelManager Instance { get; private set; }
    public LevelManager Initialize()
    {
        if (Instance == null)
        {
            levelHolder = LoadLevelHolder();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        return Instance;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public static LevelHolder LoadLevelHolder()
    {

#if DEMO_BUILD
        const string fileName = "Levels Demo";
#else
        const string fileName = "Levels";
#endif
        return Resources.Load(fileName) as LevelHolder;
    }

    void SetLevelKeys()
    {
        levelKeys = new string[levelHolder.GetLevelCount()];
        int index = 0;

        foreach (var level in levelHolder.GetAllLevels())
        {
            levelKeys[index] = level.scene;
            index++;
        }
    }

    public async Task LoadSceneAsync(SceneField scene, bool saveLastChapter = true)
    {
        var currentLevelIndex = GetCurrentLevelIndex();
        if (saveLastChapter)
        {
            lastChapterId = levelHolder.GetChapterIndexAtLevelIndex(currentLevelIndex);
        }
        else
        {
            lastChapterId = -1;
        }
        lastLevel = levelHolder.GetLevelAtIndex(currentLevelIndex);


        var activeScene = SceneManager.GetActiveScene();
        if (activeScene != null)
        {
            onSceneUnloaded?.Invoke(activeScene);
        }

        var op = SceneManager.LoadSceneAsync(scene);
        await WaitForConditionAsync.Create(() => op.isDone);
    }

    public async Task LoadMainMenuAsync(bool saveLastChapter = true)
    {
        await LoadSceneAsync(levelHolder.MainMenu, saveLastChapter);
    }

    public async Task LoadLevelAsync(int index)
    {
        await LoadSceneAsync(levelHolder.GetSceneAtIndex(index));
    }

    public async Task LoadLevelAsync(string name)
    {
        var index = FindLevel(name);
        if (index < 0)
        {
            return;
        }
        await LoadSceneAsync(levelHolder.GetSceneAtIndex(index));
    }

    public async Task LoadLevelAsync(SceneField scene)
    {
        await LoadSceneAsync(scene);
    }

    public async Task LoadNextLevelAsync()
    {
        RoomCollection.beginAtStart = true;
        if (RoomCollection.instance != null && RoomCollection.instance.NextPuzzle())
        {
            return;
        }

        var currentLevel = GetCurrentLevelIndex();
        if (currentLevel < 0)
        {
            return;
        }
        var nextLevel = (currentLevel + 1) % levelHolder.GetLevelCount();
        await LoadSceneAsync(levelHolder.GetSceneAtIndex(nextLevel));
    }

    public async Task LoadPreviousLevelAsync()
    {
        RoomCollection.beginAtStart = false;
        if (RoomCollection.instance != null && RoomCollection.instance.PreviousPuzzle())
        {
            return;
        }

        var currentLevel = GetCurrentLevelIndex();
        if (currentLevel < 0)
        {
            return;
        }
        var levelCount = levelHolder.GetLevelCount();
        var previousLevel = (currentLevel + levelCount - 1) % levelCount;
        await LoadSceneAsync(levelHolder.GetSceneAtIndex(previousLevel));
    }

    public async Task RestartLevelAsync()
    {
        await LoadSceneAsync(levelHolder.GetSceneAtIndex(GetCurrentLevelIndex()));
    }

    public Chapter GetChapterFromIndex(int index)
    {
        return levelHolder.GetChapter(index);
    }

    public Chapter GetCurrentChapter()
    {
        return levelHolder.GetChapterAtLevelIndex(GetCurrentLevelIndex());
    }

    public Level GetCurrentLevel()
    {
        return levelHolder.GetLevelAtIndex(GetCurrentLevelIndex());

    }
 
    public IEnumerable<Chapter> GetAllChapters()
    {
        foreach (var chapter in levelHolder.GetAllChapters())
        {
            yield return chapter;
        }
    }
    public int GetCurrentLevelIndex()
    {
        return FindLevel(SceneManager.GetActiveScene().name);
    }

    public int FindLevel(string levelName)
    {
        for (int i = 0; i < LevelKeys.Length; i++)
        {
            if (LevelKeys[i].EndsWith(levelName)) return i;
        }
        return -1;
    }

    public bool InMainMenu()
    {
        var currentLevelName = SceneManager.GetActiveScene().name;
        return currentLevelName == levelHolder.MainMenu;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        onSceneLoaded?.Invoke(scene);
    }
}
