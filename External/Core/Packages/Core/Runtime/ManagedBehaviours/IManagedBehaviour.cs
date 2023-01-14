using System;

namespace NBG.Core
{
    /// <summary>
    /// Level scope events
    /// 
    /// Depending on the level definition in a specific game, these events can mean different things:
    ///     - single scene loaded,
    ///     - multiple scenes loaded and activated,
    ///     - certain physics or networking systems are fully initialized,
    ///     - etc.
    /// </summary>
    public interface IManagedBehaviour
    {
        /// <summary>
        /// Called when a level is completely loaded.
        /// </summary>
        public void OnLevelLoaded();

        /// <summary>
        /// Called after OnLevelLoaded for all scripts in level is called.
        /// Essentially phase 2 of initialization.
        /// </summary>
        public void OnAfterLevelLoaded();

        /// <summary>
        /// Called before a level is unloaded.
        /// </summary>
        public void OnLevelUnloaded();
    }
}