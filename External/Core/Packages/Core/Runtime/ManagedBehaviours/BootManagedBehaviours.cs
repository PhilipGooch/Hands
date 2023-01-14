
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace NBG.Core
{
    public static class BootManagedBehaviours
    {
        private static readonly List<IManagedBehaviour> allBehaviors = new List<IManagedBehaviour>();
        private static readonly List<IManagedBehaviour> tmp = new List<IManagedBehaviour>();
        private static int resumePoint = 0;
        private static readonly Stopwatch sw = new Stopwatch();

        private static void Collect(Scene scene)
        {
            var rootGOs = scene.GetRootGameObjects();
            //NOTE: Root-Gameobject's order is not deterministic. This might initialize in a different order in builds.
            for (var index = 0; index < rootGOs.Length; index++)
            {
                var rootGO = rootGOs[index];
                rootGO.GetComponentsInChildren(true, tmp);
                allBehaviors.AddRange(tmp);
                tmp.Clear();
            }
        }
        public static bool RunOnLevelLoaded(Scene scene, int timeLimitMillis = 0)
        {
            sw.Restart();
            Collect(scene);
            if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
            {
                return false;
            }
            while (resumePoint < allBehaviors.Count)
            {
                var current = allBehaviors[resumePoint];
                try
                {
                    current.OnLevelLoaded();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                resumePoint++;

                if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
                {
                    return false;
                }
            }
            return true;
        }
        public static void NotifyOnLevelLoadedDone()
        {
            allBehaviors.TrimExcess();
            resumePoint = 0;
        }
        public static bool RunOnAfterLevelLoaded(Scene scene, int timeLimitMillis = 0)
        {
            sw.Restart();
            if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
            {
                return false;
            }
            while (resumePoint < allBehaviors.Count)
            {
                var current = allBehaviors[resumePoint];
                try
                {
                    current.OnAfterLevelLoaded();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                resumePoint++;

                if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
                {
                    return false;
                }
            }
            return true;
        }
        public static void NotifyOnAfterLevelLoadedDone()
        {
            resumePoint = Mathf.Max(allBehaviors.Count -1, 0);
        }
        public static bool RunUnloadAll(int timeLimitMillis = 0)
        {
            //Don't try to unload if there was nothing loaded
            if (allBehaviors.Count <= 0)
            {
                return true;
            }
            sw.Restart();
            Debug.Assert(resumePoint != allBehaviors.Count, "Resume point different than expected");
            if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
            {
                return false;
            }
            while (resumePoint >= 0)
            {
                var current = allBehaviors[resumePoint];
                try
                {
                    current.OnLevelUnloaded();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                resumePoint--;

                if (timeLimitMillis > 0 && sw.ElapsedMilliseconds >= timeLimitMillis)
                {
                    return false;
                }
            }
            allBehaviors.Clear();
            resumePoint = 0;
            return true;
        }
    }
}