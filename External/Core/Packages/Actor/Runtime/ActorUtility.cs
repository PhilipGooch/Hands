using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NBG.Actor
{
    internal static class Utility
    {
        internal struct ActorTreeEdges
        {
            public ActorSystem.IActor parent;
            public List<ActorSystem.IActor> children;

            public ActorTreeEdges(ActorSystem.IActor parent)
            {
                this.parent = parent;
                children = new List<ActorSystem.IActor>();
            }
        }

        private readonly static Dictionary<ActorSystem.IActor, ActorTreeEdges> actorTree = new Dictionary<ActorSystem.IActor, ActorTreeEdges>();

        private static HashSet<ActorSystem.IActor> unprocessedLeafActors = new HashSet<ActorSystem.IActor>(); // Not read-only because we swap pointers with this cache

        private static HashSet<ActorSystem.IActor> nextUnprocessedLeafActors = new HashSet<ActorSystem.IActor>(); // Not read-only because we swap pointers with this cache

        private readonly static List<Transform> transformTempCache = new List<Transform>();

        internal static int UnprocessedLeafActorsCount => unprocessedLeafActors.Count;

        /// <summary>
        /// Collections should be cleared before using and after using them.
        /// Clearing after using them is the clean way
        /// Clearing before using them is a safeguard in case a previous use attempt didn't finish correctly or threw an exception, etc.
        /// </summary>
        internal static void ClearTempCollectionsForActorMapping()
        {
            actorTree.Clear();
            unprocessedLeafActors.Clear();
            nextUnprocessedLeafActors.Clear();
            transformTempCache.Clear();
        }

        internal static void CreateActorEdges(ActorSystem.IActor actor, ICollection<ActorSystem.IActor> mapScopeLimit = null)
        {
            CreateActorEdges(actor, actorTree, unprocessedLeafActors, mapScopeLimit);
        }

        /// <summary>
        /// This creates actor trees which shows the hierachy order between actors.
        /// </summary>
        /// <param name="actor">Actor to map neighbors from</param>
        /// <param name="outActorMap">Collection of actors with potential parent and potential immediate children</param>
        /// <param name="outLeafActors">A helper collection for leaf to root traversal, that caches all the leaves as we generate the tree</param>
        /// <param name="mapScopeLimit">Optional parameter that creates a cap of actor maps. Used when new objects are instantiated and we only need a new subset of the tree.</param>
        internal static void CreateActorEdges(ActorSystem.IActor actor, Dictionary<ActorSystem.IActor, ActorTreeEdges> outActorMap, HashSet<ActorSystem.IActor> outLeafActors,
            ICollection<ActorSystem.IActor> mapScopeLimit = null)
        {
            ActorSystem.IActor parent = null;
            if (actor.ActorGameObject.transform.parent != null)
            {
                parent = actor.ActorGameObject.transform.parent.gameObject.GetComponentInParent<ActorSystem.IActor>(true); // NOTE: Get actual parent and not the component on self.
                if (mapScopeLimit != null && parent != null && !mapScopeLimit.Contains(parent))
                    parent = null;
            }

            if (outActorMap.ContainsKey(actor))
            {
                ActorTreeEdges oldTreeEdges = outActorMap[actor];
                oldTreeEdges.parent = parent;
                outActorMap[actor] = oldTreeEdges;
            }
            else
            {
                outActorMap.Add(actor, new ActorTreeEdges(parent));
                outLeafActors.Add(actor); // NOTE: Actor is leaf until proven otherwise
            }

            if (parent != null)
            {
                if (!outActorMap.TryGetValue(parent, out ActorTreeEdges entry))
                {
                    entry = new ActorTreeEdges(null);
                    outActorMap[parent] = entry;
                }

                entry.children.Add(actor);
                outLeafActors.Remove(parent); // NOTE: Proven not to be a leaf
            }
        }

        private static void ExcludeChildGameObjectsOfOtherActors_LeafToRoot(ActorSystem actorManager, ActorSystem.IActor actor, HashSet<int> target,
            Dictionary<ActorSystem.IActor, ActorTreeEdges> actorTree)
        {
            ActorTreeEdges actorEdge = actorTree[actor];

            for (int i = 0; i < actorEdge.children.Count; i++)
            {
                int childActorID = actorManager.actorMap[actorEdge.children[i]];
                target.ExceptWith(actorManager.actorIDToActorGameObjectsSorted[childActorID]);
                ExcludeChildGameObjectsOfOtherActors_LeafToRoot(actorManager, actorEdge.children[i], target, actorTree);
            }
        }

        /// <summary>
        /// During tests this method performed around 4 times faster than RootToLeaf if
        /// the Actor distribution was leaning towards breadth instead of depth. (Many actors, but hierarchies aren't very deep)
        /// And two times slower if the distribution was towards depth (Many actors, deep hierarchies but few siblings)
        /// This method has some overhead HashSet.ExceptWith, but keeps the amount of HashSets allocated at the same time low.
        /// </summary>
        public static void InitializeActors_LeafToRoot(ActorSystem actorManager, HashSet<ActorSystem.IActor> unprocessedLeafActors,
            Dictionary<ActorSystem.IActor, ActorTreeEdges> actorTree, List<Transform> transformTempCache, HashSet<ActorSystem.IActor> nextLeafCandidateCollection)
        {
            foreach (ActorSystem.IActor leafActor in unprocessedLeafActors)
            {
                transformTempCache.Clear();
                leafActor.ActorGameObject.GetComponentsInChildren(true, transformTempCache);

                HashSet<int> allActorChildren = new HashSet<int>(transformTempCache.Select(transform => transform.gameObject.GetInstanceID()));
                ExcludeChildGameObjectsOfOtherActors_LeafToRoot(actorManager, leafActor, allActorChildren, actorTree);

                actorManager.RegisterActor(leafActor, allActorChildren);

                ActorSystem.IActor nextLeafCandidate = actorTree[leafActor].parent;
                if (nextLeafCandidate != null && !nextLeafCandidateCollection.Contains(nextLeafCandidate))
                {
                    bool candidateWillBeDeepestUninitializedItem = true;
                    for (int i = 0; i < actorTree[nextLeafCandidate].children.Count; i++)
                    {
                        ActorSystem.IActor child = actorTree[nextLeafCandidate].children[i];

                        // Childern of the candidate are being processed in this loop or were processed some time ago.
                        if (!unprocessedLeafActors.Contains(child) && !actorManager.actorMap.ContainsKey(child))
                        {
                            candidateWillBeDeepestUninitializedItem = false;
                            break;
                        }
                    }

                    if (candidateWillBeDeepestUninitializedItem)
                    {
                        nextLeafCandidateCollection.Add(nextLeafCandidate);
                    }
                }
            }
        }

        public static void InitializeActors_LeafToRoot(ActorSystem actorSystem)
        {
            InitializeActors_LeafToRoot(actorSystem, unprocessedLeafActors, actorTree, transformTempCache, nextUnprocessedLeafActors);

            unprocessedLeafActors.Clear(); // unprocessedLeafActors have been processed, nextUnprocessedLeafActors are moved to unprocessedLeafActors
            (unprocessedLeafActors, nextUnprocessedLeafActors) = (nextUnprocessedLeafActors, unprocessedLeafActors); // Swap caches using: Tuple-Swap syntax sugar
        }
    }
}
