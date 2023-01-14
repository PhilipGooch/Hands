using NBG.Core.Events;
using UnityEngine;
using UnityEngine.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Core.LowLevelPlayerLoop
{
    struct LowLevelFixedUpdateEvent
    {
    }

    struct LowLevelEarlyUpdateEvent
    {
    }

    struct LowLevelUpdateEvent
    {
    }

    struct LowLevelLateUpdateEvent
    {
    }

    public static class LowLevelPlayerLoopEventEmitter
    {
        static void Call<T>() where T : struct
        {
            var eventBus = EventBus.Get();
            if (eventBus != null)
            {
                eventBus.Send<T>(default(T));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInit()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;
#endif
            Setup();
        }

#if UNITY_EDITOR
        private static void OnPlaymodeChanged(UnityEditor.PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    {
                        TearDown();
                    }
                    break;
            }
        }
#endif

        static void Setup()
        {
            //Debug.LogWarning("LowLevelPlayerLoopEventEmitter setup.");
            
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            LowLevelPlayerLoopUtilities.AppendToPlayerLoopList(
                typeof(LowLevelFixedUpdateEvent), Call<LowLevelFixedUpdateEvent>,
                ref loop, typeof(UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate));

            LowLevelPlayerLoopUtilities.AppendToPlayerLoopList(
                typeof(LowLevelEarlyUpdateEvent), Call<LowLevelEarlyUpdateEvent>,
                ref loop, typeof(UnityEngine.PlayerLoop.EarlyUpdate));

            LowLevelPlayerLoopUtilities.AppendToPlayerLoopList(
                typeof(LowLevelUpdateEvent), Call<LowLevelUpdateEvent>,
                ref loop, typeof(UnityEngine.PlayerLoop.Update));

            LowLevelPlayerLoopUtilities.AppendToPlayerLoopList(
                typeof(LowLevelLateUpdateEvent), Call<LowLevelLateUpdateEvent>,
                ref loop, typeof(UnityEngine.PlayerLoop.PreLateUpdate));

            PlayerLoop.SetPlayerLoop(loop);
        }

        static void TearDown()
        {
            //Debug.LogWarning("LowLevelPlayerLoopEventEmitter teardown.");

            var loop = PlayerLoop.GetCurrentPlayerLoop();

            LowLevelPlayerLoopUtilities.RemoveFromPlayerLoopList(
                typeof(LowLevelFixedUpdateEvent),
                ref loop, typeof(UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate));

            LowLevelPlayerLoopUtilities.RemoveFromPlayerLoopList(
                typeof(LowLevelEarlyUpdateEvent),
                ref loop, typeof(UnityEngine.PlayerLoop.EarlyUpdate));

            LowLevelPlayerLoopUtilities.RemoveFromPlayerLoopList(
                typeof(LowLevelUpdateEvent),
                ref loop, typeof(UnityEngine.PlayerLoop.Update));

            LowLevelPlayerLoopUtilities.RemoveFromPlayerLoopList(
                typeof(LowLevelLateUpdateEvent),
                ref loop, typeof(UnityEngine.PlayerLoop.PreLateUpdate));

            PlayerLoop.SetPlayerLoop(loop);
        }
    }
}
