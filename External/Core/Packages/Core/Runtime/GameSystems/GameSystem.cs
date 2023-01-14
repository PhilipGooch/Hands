namespace NBG.Core.GameSystems
{
    /// <summary>
    /// System that will only run on the main thread.
    /// 
    /// Always synchronizes dependencies.
    /// </summary>
    public abstract class GameSystem : GameSystemBase
    {
        void BeforeOnUpdate()
        {
            if (AlwaysSynchronizeWorld)
            {
                World.DependencyManager.CompleteAll();
            }
            else if (AlwaysSynchronizeSystem)
            {
                CompleteDependencies();
            }
        }

        void AfterOnUpdate()
        {
            if (AlwaysCompleteWorldAfterUpdate)
            {
                World.DependencyManager.CompleteAll();
            }
        }

        public sealed override void Update()
        {
            var state = CheckedState();

            if (Enabled && ShouldRunSystem())
            {
                if (!state.previouslyEnabled)
                {
                    state.previouslyEnabled = true;
                    OnStartRunning();
                }

                BeforeOnUpdate();

                try
                {
                    OnUpdate();
                }
                finally
                {
                    AfterOnUpdate();
                }
            }
            else if (state.previouslyEnabled)
            {
                state.previouslyEnabled = false;
                OnStopRunningInternal();
            }
        }

        internal override void OnBeforeCreateInternal(GameSystemWorld world)
        {
            AlwaysSynchronizeSystem = true;
            AlwaysSynchronizeWorld = GetType().GetCustomAttributes(typeof(AlwaysSynchronizeWorldAttribute), true).Length != 0;
            AlwaysCompleteWorldAfterUpdate = false;
        }

        /// <summary>
        /// Executes the system immediately.
        /// </summary>
        protected abstract void OnUpdate();
    }
}
