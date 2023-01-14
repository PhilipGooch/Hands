using NBG.Core.GameSystems;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Recoil.Util
{
    public interface IOnPhysicsAfterSolve
    {
        void OnPhysicsAfterSolve();
    }

    // System that executes registered IOnFixedUpdate behaviors on main thread right after state is read
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(PhysicsAfterSolve))]
    public class OnPhysicsAfterSolveSystem : GameSystem
    {
        static OnPhysicsAfterSolveSystem instance;

        protected override void OnCreate()
        {
            base.OnCreate();
            instance = this;
        }

        List<IOnPhysicsAfterSolve> behaviors = new List<IOnPhysicsAfterSolve>();

        public static void Register(IOnPhysicsAfterSolve behavior)
        {
            instance.behaviors.Add(behavior);
        }
        public static void Unregister(IOnPhysicsAfterSolve behavior)
        {
            instance.behaviors.Remove(behavior);
        }
        protected override void OnUpdate()
        {
            Profiler.BeginSample(nameof(IOnPhysicsAfterSolve));
            for (int i = 0; i < behaviors.Count; i++)
                if (behaviors[i] != null)
                    behaviors[i].OnPhysicsAfterSolve();
            Profiler.EndSample();
        }
    }
}
