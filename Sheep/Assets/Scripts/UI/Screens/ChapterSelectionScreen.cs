using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;

public class ChapterSelectionScreen : PaginatedSelectionScreen
{
    Chapter[] chapters;
    Action<int> onChapterClick;
    public void Initialize(LevelManager levelManager, HorizontalNavigationButtons horizontalNavigation, Action<int> onChapterClick)
    {
        chapters = levelManager.GetAllChapters().ToArray();
        this.onChapterClick = onChapterClick;
        Initialize(horizontalNavigation, GetButtonPrefabs(chapters), chapters.Length);
    }

    List<Button3D> GetButtonPrefabs(Chapter[] chapters)
    {
        List<Button3D> buttons = new List<Button3D>();
        for (int i = 0; i < chapters.Length; i++)
        {
            buttons.Add(chapters[i].selectionPrefab ?? defaultSelectionButtonPrefab);
        }
        return buttons;
    }

    protected override void SetupButton(Button3D target, int id)
    {
        var chapter = chapters[id];

        target.SetPrimaryText("chapter_" + id);
        target.SetSecondaryText(ScriptTerms.progress);
        target.SetParameterInSecondaryText("COMPLETION_PERCENT", ((int)(DataManager.Instance.GetChapterCompletion(chapter) * 100)).ToString());
        target.SetInteractable(GetIsInteractable(id));
        target.onClick += () => { onChapterClick(id); };
    }

    bool GetIsInteractable(int id)
    {
        return true;
    }
}
