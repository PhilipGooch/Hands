using NBG.Core.GameSystems;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NBG.Core
{
    public interface IOnFixedUpdate
    {
        bool Enabled { get; }

        void OnFixedUpdate();
    }

    [AlwaysSynchronizeWorld]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class OnFixedUpdateSystem : GameSystem
    {
        public static int ReserveCapacity = 2048;

        static OnFixedUpdateSystem instance;
        private CustomSampler profilerSampler;

        protected override void OnCreate()
        {
            base.OnCreate();
            instance = this;
            profilerSampler = CustomSampler.Create(nameof(OnUpdateSystem));
        }

        List<IOnFixedUpdate> behaviors = new List<IOnFixedUpdate>(ReserveCapacity);
        List<IOnFixedUpdate> copy = new List<IOnFixedUpdate>(ReserveCapacity);
        public static void Register(IOnFixedUpdate behavior)
        {
            instance.behaviors.Add(behavior);
        }
        public static void Unregister(IOnFixedUpdate behavior)
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
