using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NBG.Core
{
    // Describes a single level
    public interface ILevel
    {
        // Level base scene
        Scene BaseScene { get; }
        // Level sections: other scenes loaded additively
        IEnumerable<Scene> Sections { get; }
    }
}
