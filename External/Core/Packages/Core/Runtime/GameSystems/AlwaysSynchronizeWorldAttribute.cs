using System;

namespace NBG.Core.GameSystems
{
    /// <summary>
    /// AlwaysSynchronizeWorld can be applied to a GameSystemBase derivatives to force it to synchronize on all
    /// jobs in the current world before every update. This attribute should only be applied when a synchronization point is
    /// necessary every frame.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AlwaysSynchronizeWorldAttribute : Attribute
    {
    }
}
