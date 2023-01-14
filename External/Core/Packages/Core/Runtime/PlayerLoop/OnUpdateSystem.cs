using NBG.Core.GameSystems;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NBG.Core
{
    public interface IOnUpdate
    {
        bool Enabled { get; }

        void OnFixedUpdate();
    }

    [AlwaysSynchronizeWorld]
    [UpdateInGroup(typeof(UpdateSystemGroup))]
    public class OnUpdateSystem : GameSystem
    {
        public static int ReserveCapacity = 2048;

        static OnUpdateSystem instance;
        private CustomSampler profilerSampler;

        protected override void OnCreate()
        {
            base.OnCreate();
            instance = this;
            profilerSampler = CustomSampler.Create(nameof(OnUpdateSystem));
        }
        
        List<IOnUpdate> behaviors = new List<IOnUpdate>(ReserveCapacity);
        List<IOnUpdate> copy = new List<IOnUpdate>(ReserveCapacity);
        public static void Register(IOnUpdate behavior)
        {
            instance.behaviors.Add(behavior);
        }
        public static void Unregister(IOnUpdate behavior)
        {
            instance.behaviors.Remove(behavior);
            var idx = instance.copy.IndexOf(behavior);
            if (idx >= 0)
                instance.copy[idx] = null;
        }

        protected override void OnUpdate()
        {
            profilerSampler.Begin();
            copy.Clear();
            copy.AddRange(behaviors);
            for (int i = 0; i < copy.Count; i++)
            {
                try
                {
                    if (copy[i] != null && copy[i].Enabled)
                        copy[i].OnFixedUpdate();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            profilerSampler.End();
        }
    }
}
