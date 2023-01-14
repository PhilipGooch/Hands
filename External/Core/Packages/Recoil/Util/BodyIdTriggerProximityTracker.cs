using System.Collections.Generic;
using UnityEngine;

namespace Recoil
{
    public class BodyIdTriggerProximityTracker
    {
        public int Count { get { return bodyToCollider.Count; } }

        /// <summary>
        /// Key - bodyId
        /// Value - number of colliders in trigger (reference counter)
        /// </summary>
        private Dictionary<int, int> bodyToCollider = new Dictionary<int, int>();
        private List<int> deadEntriesCache = new List<int>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other">Returns bodyId if it's a newly added body. World.environmentId otherwise.</param>
        /// <returns></returns>
        public int OnTriggerEnter(Collider other)
        {
            int bodyId = ManagedWorld.main.FindBody(other.attachedRigidbody);
            if (World.IsEnvironment(bodyId)) return World.environmentId;
            
            if (bodyToCollider.ContainsKey(bodyId))
            {
                bodyToCollider[bodyId]++;
                return World.environmentId;
            }
            else
            {
                bodyToCollider.Add(bodyId, 1);
                return bodyId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other">Returns bodyId if the body left the collider. World.environmentId otherwise.</param>
        /// <returns></returns>
        public int OnTriggerLeave(Collider other)
        {
            int bodyId = ManagedWorld.main.FindBody(other.attachedRigidbody);
            if (World.IsEnvironment(bodyId)) return World.environmentId;

            if (bodyToCollider.ContainsKey(bodyId))
            {
                bodyToCollider[bodyId]--;
                if (bodyToCollider[bodyId] == 0)
                {
                    bodyToCollider.Remove(bodyId);
                    return bodyId;
                }
                else
                {
                    if (bodyToCollider[bodyId] < 0)
                    {
                        Error_RefCounterIsNegative(bodyId);
                        bodyToCollider.Remove(bodyId);
                    }
                    return World.environmentId;
                }
            }
            else
            {
                Error_RefCounterIsNegative(bodyId);
                return World.environmentId;
            }
        }

        public void Clear ()
        {
            bodyToCollider.Clear();
        }

        public void ForceRemoveEntry (int bodyId)
        {
            bodyToCollider.Remove(bodyId);
        }

        public void RemoveDeadEntries ()
        {
            foreach (int bodyId in bodyToCollider.Keys)
            {
                if (!World.main.GetBody(bodyId).alive)
                {
                    deadEntriesCache.Add(bodyId);
                }
            }

            for (int i=0; i<deadEntriesCache.Count; i++)
            {
                bodyToCollider.Remove(deadEntriesCache[i]);
            }

            deadEntriesCache.Clear();
        }

        private void Error_RefCounterIsNegative (int bodyId)
        {
            Rigidbody rigidbody = ManagedWorld.main.GetRigidbody(bodyId);
            if (rigidbody == null)
            {
                Debug.LogError($"Ref counter negative: For body {bodyId} more colliders left the trigger than entered. " +
                    $"Was force-remove entry used on an object that later triggered the trigger exit?");
            }
            else
            {
                Debug.LogError($"Ref counter negative: For body {bodyId}, {rigidbody.name} more colliders left the trigger than entered. " +
                    $"Was force-remove entry used on an object that later triggered the trigger exit", rigidbody);
            }
        }
    }
}
