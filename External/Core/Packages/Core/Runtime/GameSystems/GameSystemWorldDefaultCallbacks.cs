using UnityEngine;

namespace NBG.Core.GameSystems
{
    [ScriptExecutionOrder(32767)]
    internal class GameSystemWorldDefaultCallbacks : MonoBehaviour
    {
        private GameSystemWorld _world;

        public static GameSystemWorldDefaultCallbacks Create(string name, GameSystemWorld world)
        {
            var go = new GameObject($"NBG_GAMESYSTEM_CALLBACKS - {name}");
            DontDestroyOnLoad(go);
            go.hideFlags |= HideFlags.NotEditable;

            var callbacks = go.AddComponent<GameSystemWorldDefaultCallbacks>();
            callbacks._world = world;
            return callbacks;
        }

        public static void Destroy(GameSystemWorldDefaultCallbacks callbacks)
        {
            if (callbacks == null)
                return;

            DestroyImmediate(callbacks.gameObject);
        }

        void FixedUpdate()
        {
            var fixedUpdate = _world.GetExistingSystem<FixedUpdateSystemGroup>();
            fixedUpdate.Update();
        }

        void Update()
        {
            var earlyUpdate = _world.GetExistingSystem<EarlyUpdateSystemGroup>();
            earlyUpdate.Update();

            var update = _world.GetExistingSystem<UpdateSystemGroup>();
            update.Update();
        }

        void LateUpdate()
        {
            var lateUpdate = _world.GetExistingSystem<LateUpdateSystemGroup>();
            lateUpdate.Update();
        }
    }
}
