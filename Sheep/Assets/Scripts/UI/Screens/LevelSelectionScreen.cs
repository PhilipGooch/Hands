using System.Collections.Generic;
using System;
using UnityEngine;

public class LevelSelectionScreen : PaginatedSelectionScreen
{
    LevelList scenes;
    Action<Level> onSceneButtonClick;

    public void Initialize(LevelManager levelManager, HorizontalNavigationButtons horizontalNavigation, int chapterId)
    {
        scenes = levelManager.GetChapterFromIndex(chapterId).levels;

        this.onSceneButtonClick = (level) => PlayerUIManager.Instance.PlayLevel(level);

        Initialize(horizontalNavigation, GetButtonPrefabs(scenes), scenes.Length);
    }

    List<Button3D> GetButtonPrefabs(LevelList scenes)
    {
        List<Button3D> buttons = new List<Button3D>();
        for (int i = 0; i < scenes.Count; i++)
        {
            buttons.Add(scenes[i].selectionPrefab ?? defaultSelectionButtonPrefab);
        }
        return buttons;
    }

    protected override void SetupButton(Button3D target, int id)
    {
        var scene = scenes[id];

        target.SetPrimaryText(scene.scene.SceneName);
        target.SetInteractable(GetIsInteractable(id));
        target.onClick += () => { onSceneButtonClick(scene); };
    }

    bool GetIsInteractable(int id)
    {
        return true;
    }
}
