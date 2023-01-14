using NBG.Core.GameSystems;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.SceneManagement;

namespace NBG.Actor
{
    /// <summary>
    /// A class built in a similar structure as BootManagedBehaviours
    /// A multi-step initialization with timeout tracking not to lock up main thread.
    /// </summary>
    public static class BootActors
    {
        private enum ActorBootStep
        {
            InitAndCollectLooseComponents = 0,
            CreateActorTree = 1,
            InitializeActors = 2,
        }

        private readonly static List<ActorSystem.IActor> allActors = new List<ActorSystem.IActor>();
        private readonly static List<ActorSystem.IActor> actorTempCache = new List<ActorSystem.IActor>();

        private readonly static Stopwatch stopwatch = new Stopwatch();

        private static ActorBootStep actorBootStep = ActorBootStep.InitAndCollectLooseComponents;
        private static int iteratorCache = 0;

        /// <summary>
        /// Only external clear booter state reasonably covers exceptions. Because booter operates by returning true/false of
        /// whether it has completed and just awaits next call, booter itself can't identify all the cases where it was interuppted.
        /// For example: exceptions happen one layer above and booter just thinks that it's supposed to get a next boot iteration.
        /// </summary>
        public static void ClearBooterState()
        {
            Utility.ClearTempCollectionsForActorMapping();
            allActors.Clear();
            actorTempCache.Clear();

            iteratorCache = 0;
            actorBootStep = 0;
        }

        private static void Collect(Scene scene)
        {
            var rootGOs = scene.GetRootGameObjects();
            //NOTE: Root-Gameobject's order is not deterministic. This might initialize in a different order in builds.
            for (var index = 0; index < rootGOs.Length; index++)
            {
                var rootGO = rootGOs[index];
                rootGO.GetComponentsInChildren(true, actorTempCache);
                allActors.AddRange(actorTempCache);
                actorTempCache.Clear();
            }
        }

        [Conditional("UNITY_EDITOR")]
        private static void SlowSingleStep(long elapsed, long limit)
        {
            const long TOO_LONG_SCALE = 2;
            if (elapsed > limit * TOO_LONG_SCALE)
                UnityEngine.Debug.LogWarning($"A single minimum step of {nameof(BootActors)} is longer than timeout * {TOO_LONG_SCALE}. Elapsed: {elapsed}, timeout: {limit}");
        }

        private static bool Timeout(int timeLimitMillis)
        {
            if (timeLimitMillis > 0)
            {
                SlowSingleStep(stopwatch.ElapsedMilliseconds, timeLimitMillis);
                return stopwatch.ElapsedMilliseconds >= timeLimitMillis;
            }
            return false;
        }

        public static bool RunInits(Scene scene, int timeLimitMillis = 0)
        {
            stopwatch.Restart();

            if (actorBootStep == ActorBootStep.InitAndCollectLooseComponents)
            {
                Utility.ClearTempCollectionsForActorMapping();
                Collect(scene);
                actorBootStep++;
                if (Timeout(timeLimitMillis))
                    return false;
            }

            if (actorBootStep == ActorBootStep.CreateActorTree)
            {
                while (iteratorCache < allActors.Count)
                {
                    Utility.CreateActorEdges(allActors[iteratorCache]);
                    iteratorCache++;
                    if (Timeout(timeLimitMillis))
                        return false;
                }

                iteratorCache = 0;
                actorBootStep++;
            }

            if (actorBootStep == ActorBootStep.InitializeActors)
            {
                while (Utility.UnprocessedLeafActorsCount > 0)
                {
                    // NOTE: If necessary with some extra code another one layer could be unwrapped.
                    Utility.InitializeActors_LeafToRoot(ActorSystem.Main);

                    if (Timeout(timeLimitMillis))
                        return false;
                }

                actorBootStep++;
            }

            // Even though we trust that booter will be cleared externally, we clear here too
            // to not hold references to unnecessary data after a successful boot.
            ClearBooterState(); 
            return true;
        }
    }
}
