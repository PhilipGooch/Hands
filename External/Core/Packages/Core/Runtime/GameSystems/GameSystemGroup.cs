using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.GameSystems
{
    public abstract class GameSystemGroup : GameSystem
    {
        private bool m_systemSortDirty = false;

        // If true (the default), calling SortSystems() will sort the system update list, respecting the constraints
        // imposed by [UpdateBefore] and [UpdateAfter] attributes. SortSystems() is called automatically during
        // GameSystemGroup.OnUpdate(), but may also be called manually.
        //
        // If false, calls to SortSystems() on this system group will have no effect on update order of systems in this
        // group (though SortSystems() will still be called recursively on any child system groups). The group's systems
        // will update in the order of the most recent sort operation, with any newly-added systems updating in
        // insertion order at the end of the list. In this mode, removing systems from the group is an error.
        //
        // Setting this value to false is not recommended unless you know exactly what you're doing, and you have full
        // control over the systems which will be updated in this group.
        public bool EnableSystemSorting { get; protected set; } = true;

        internal List<GameSystemBase> m_systemsToUpdate = new List<GameSystemBase>();
        internal List<GameSystemBase> m_systemsToRemove = new List<GameSystemBase>();

        public virtual IReadOnlyList<GameSystemBase> Systems => m_systemsToUpdate;

        public void AddSystemToUpdateList(GameSystemBase sys)
        {
            if (sys != null)
            {
                if (this == sys)
                    throw new ArgumentException($"Can't add {GetType().Name} to its own update list");

                // Check for duplicate Systems
                if (m_systemsToUpdate.IndexOf(sys) >= 0)
                {
                    if (m_systemsToRemove.Contains(sys))
                    {
                        m_systemsToRemove.Remove(sys);
                    }
                    return;
                }

                m_systemsToUpdate.Add(sys);
                m_systemSortDirty = true;
            }
        }

        public void RemoveSystemFromUpdateList(GameSystemBase sys)
        {
            if (!EnableSystemSorting)
                throw new InvalidOperationException("Removing systems from a group is not supported if group.EnableSystemSorting is false.");

            if (m_systemsToUpdate.Contains(sys) && !m_systemsToRemove.Contains(sys))
            {
                m_systemSortDirty = true;
                m_systemsToRemove.Add(sys);
            }
        }

        protected override void OnUpdate()
        {
            UpdateAllSystems();
        }

        void UpdateAllSystems()
        {
            if (m_systemSortDirty)
                SortSystems();

            int updateListLength = m_systemsToUpdate.Count;
            for (int i = 0; i < updateListLength; ++i)
            {
                try
                {
                    var sys = m_systemsToUpdate[i];
                    sys.Update();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void SortSystems()
        {
            SortSystemsRecurse();
        }

        void SortSystemsRecurse()
        {
            if (!EnableSystemSorting)
            {
                m_systemSortDirty = true;
            }
            else if (m_systemSortDirty)
            {
                GenerateMasterUpdateList();
            }

            foreach (var sys in m_systemsToUpdate)
            {
                if (TypeManager.IsSystemAGroup(sys.GetType()))
                {
                    var childGroup = sys as GameSystemGroup;
                    childGroup.SortSystemsRecurse();
                }
            }
        }

        private void GenerateMasterUpdateList()
        {
            RemovePending();

            var groupType = GetType();
            var allElems = new GameSystemSorter.SystemElement[m_systemsToUpdate.Count];
            var systemsPerBucket = new int[3];
            for (int i = 0; i < m_systemsToUpdate.Count; ++i)
            {
                var system = m_systemsToUpdate[i];
                var sysType = system.GetType();
                int orderingBucket = ComputeSystemOrdering(sysType, groupType);
                allElems[i] = new GameSystemSorter.SystemElement
                {
                    Type = sysType,
                    Index = i,
                    OrderingBucket = orderingBucket,
                    updateBefore = new List<Type>(),
                    nAfter = 0,
                };
                systemsPerBucket[orderingBucket]++;
            }
            
            // Find & validate constraints between systems in the group
            GameSystemSorter.FindConstraints(groupType, allElems);

            // Build three lists of systems
            var elemBuckets = new[]
            {
                new GameSystemSorter.SystemElement[systemsPerBucket[0]],
                new GameSystemSorter.SystemElement[systemsPerBucket[1]],
                new GameSystemSorter.SystemElement[systemsPerBucket[2]],
            };
            var nextBucketIndex = new int[3];

            for (int i = 0; i < allElems.Length; ++i)
            {
                int bucket = allElems[i].OrderingBucket;
                int index = nextBucketIndex[bucket]++;
                elemBuckets[bucket][index] = allElems[i];
            }
            // Perform the sort for each bucket.
            for (int i = 0; i < 3; ++i)
            {
                if (elemBuckets[i].Length > 0)
                {
                    GameSystemSorter.Sort(elemBuckets[i]);
                }
            }

            // Append buckets in order
            var oldSystems = m_systemsToUpdate;
            m_systemsToUpdate = new List<GameSystemBase>(oldSystems.Count);
            for (int i = 0; i < 3; ++i)
            {
                foreach (var e in elemBuckets[i])
                {
                    var index = e.Index;
                    m_systemsToUpdate.Add(oldSystems[index]);
                }
            }

            m_systemSortDirty = false;
        }

        private void RemovePending()
        {
            if (m_systemsToRemove.Count > 0)
            {
                foreach (var sys in m_systemsToRemove)
                {
                    m_systemsToUpdate.Remove(sys);
                }

                m_systemsToRemove.Clear();
            }
        }

        protected override void OnStopRunning()
        {
        }

        internal override void OnStopRunningInternal()
        {
            OnStopRunning();

            foreach (var sys in m_systemsToUpdate)
            {
                if (sys == null)
                    continue;

                if (sys._state == null)
                    continue;

                if (!sys._state.previouslyEnabled)
                    continue;

                sys._state.previouslyEnabled = false;
                sys.OnStopRunningInternal();
            }
        }

        internal static int ComputeSystemOrdering(Type sysType, Type ourType)
        {
            foreach (var uga in TypeManager.GetSystemAttributes(sysType, typeof(UpdateInGroupAttribute)))
            {
                var updateInGroupAttribute = (UpdateInGroupAttribute)uga;

                if (updateInGroupAttribute.GroupType.IsAssignableFrom(ourType))
                {
                    if (updateInGroupAttribute.OrderFirst)
                    {
                        return 0;
                    }

                    if (updateInGroupAttribute.OrderLast)
                    {
                        return 2;
                    }
                }
            }

            return 1;
        }

        public override bool DebugContainsRecursive(GameSystemBase system)
        {
            foreach (var item in m_systemsToUpdate)
            {
                if (item == system)
                    return true;
                if (item.DebugContainsRecursive(system))
                    return true;
            }

            return false;
        }

        public override void DebugPrint(System.Text.StringBuilder sb, int indent)
        {
            var desc = new string(' ', indent * 4);
            desc += $"[{GetDebugSystemStatusPrefix(this)}G] {GetType().Name}";
            DebugAppendDeps(ref desc);
            sb.AppendLine(desc);
            
            if (!EnableSystemSorting)
            {
                sb.Append(' ', indent * 4);
                sb.AppendLine("* Automatic system sorting is OFF.");
            }

            if (m_systemSortDirty)
            {
                sb.Append(' ', indent * 4);
                sb.AppendLine("* System sort is DIRTY.");
            }

            ++indent;

            foreach (var system in m_systemsToUpdate)
            {
                system.DebugPrint(sb, indent);
            }
        }
    }
}
