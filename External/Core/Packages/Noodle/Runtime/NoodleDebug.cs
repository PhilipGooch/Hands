#if ENABLE_NOODLE_DEBUG
using Drawing;
using NBG.Core.GameSystems;
using Recoil;

namespace Noodles
{
    public class NoodleDebug
    {
        public static CommandBuilder builder;
        static RedrawScope scope;

        [UpdateInGroup(typeof(LateUpdateSystemGroup))]
        public class NoodleDebugUpdate : GameSystem
        {
            protected override void OnUpdate()
            {
                NoodleDebug.scope.Draw();
            }
        }

        [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
        [UpdateBefore(typeof(PhysicsSystemGroup))]
        public class NoodleDebugInit : GameSystem
        {
            protected override void OnUpdate()
            {
                NoodleDebug.scope = DrawingManager.GetRedrawScope();
                NoodleDebug.builder = DrawingManager.instance.gizmos.GetBuilder(scope, true);
            }
        }

        [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
        [UpdateAfter(typeof(PhysicsSystemGroup))]
        public class NoodleDebugDispose : GameSystem
        {
            protected override void OnUpdate()
            {
                NoodleDebug.builder.Dispose();
            }
        }
    }
}
#endif
