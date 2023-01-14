using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelReference : ILevel
{
    public Scene BaseScene => SceneManager.GetActiveScene();

    public IEnumerable<Scene> Sections
    {
        get
        {
            yield break;
        }
    }
}
