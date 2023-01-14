using NBG.Core;
using NBG.Core.GameSystems;

namespace Recoil
{
    public static class RecoilSystems
    {
        [ClearOnReload]
        static RecoilWorldReader reader;
        public static RecoilWorldReader WorldReader => reader;

        public static void Initialize(GameSystemWorld world)
        {
            // Sort PhysicsVelocityIterationSystemGroup which is handled manually
            var pvisg = world.GetExistingSystem<PhysicsVelocityIterationSystemGroup>();
            pvisg.SortSystems();

            // Setup a reader to handle ReadState system
            if (reader != null)
                throw new System.InvalidOperationException("Only one RecoilWorldReader is allowed");
            reader = RecoilWorldReader.Create("default");
        }

        public static void Shutdown()
        {
            RecoilWorldReader.Destroy(reader);
            reader = null;
        }
    }
}