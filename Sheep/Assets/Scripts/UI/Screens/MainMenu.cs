using UnityEngine;
using System;
using VR.System;

public class MainMenu : MonoBehaviour
{
    ChapterSelectionScreen chapterScreen;
    LevelSelectionScreen levelScreen;
    StartingScreen startingScreen;
    HorizontalNavigationButtons horizontalNavigation;

    LevelManager levelManager;

    public void Start()
    {
        chapterScreen = GetComponentInChildren<ChapterSelectionScreen>(true);
        levelScreen = GetComponentInChildren<LevelSelectionScreen>(true);
        startingScreen = GetComponentInChildren<StartingScreen>(true);
        horizontalNavigation = GetComponentInChildren<HorizontalNavigationButtons>(true);

        levelManager = LevelManager.Instance;

        chapterScreen.Initialize(levelManager, horizontalNavigation, ShowLevelSelectScreen);
        startingScreen.Initialize(ShowChapterSelectScreen);

        SetupInitialScreen();
    }

    void SetupInitialScreen()
    {
        startingScreen.Toggle();

        if (VRSystem.Instance.NeedsHeightCalibration && GameSettings.Instance.playerHeight.Value == 0 && VRSystem.Instance.Initialized)
        {
            PlayerUIManager.Instance.DisplayNotification(
               "Height not calibrated",
               "Your height appears to not be calibrated",
               "Calibrate",
               () =>
               {
                   Player.Instance.CalibrateHeight();
                   Player.Instance.ReadjustPlayerInstancePosition();
               }
           );
        }
        else
        {
            if (levelManager.lastChapterId != -1)
                ShowLevelSelectScreen(levelManager.lastChapterId);
        }
    }

    void ShowChapterSelectScreen()
    {
        if (levelManager.chaptersCount == 1)
            ShowLevelSelectScreen(0);
        else
            chapterScreen.Toggle();
    }

    void ShowLevelSelectScreen(int chapterId)
    {
        levelScreen.Initialize(levelManager, horizontalNavigation, chapterId);
        levelScreen.Toggle();
    }

 
}
