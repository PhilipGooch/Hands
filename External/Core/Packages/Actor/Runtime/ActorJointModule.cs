using System.Collections.Generic;
using UnityEngine;

namespace NBG.Actor
{
    /// <summary>
    /// Actor module responsible for handling dynamic joints between actors on:
    /// Respawns, disables, teleports, etc.
    /// </summary>
    public class ActorJointModule
    {
        private ActorSystem actorSystem;

        private Dictionary<int, List<Joint>> actorIDToJoints = new Dictionary<int, List<Joint>>();

        internal ActorJointModule(ActorSystem actorSystem)
        {
            this.actorSystem = actorSystem;

            actorSystem.OnBeforeActorDespawn += DetachDynamicJoints;
            actorSystem.OnDispose += OnDispose;
        }

        private void DetachDynamicJoints(int actorID, ActorSystem.IActor affectedActor)
        {
            if (actorIDToJoints.TryGetValue(actorID, out List<Joint> jointsConnectedToActor))
            {
                for (int i = 0; i < jointsConnectedToActor.Count; i++)
                {
                    if (jointsConnectedToActor[i] != null)
                        Object.Destroy(jointsConnectedToActor[i]);
                }
            }
        }

        private void LinkJointToActor(Joint joint, ActorSystem.IActor actor)
        {
            int actorID = actorSystem.actorMap[actor];

            if (!actorIDToJoints.TryGetValue(actorID, out List<Joint> authorJoints))
            {
                authorJoints = new List<Joint>();
                actorIDToJoints[actorID] = authorJoints;
            }

            if (authorJoints.Contains(joint))
                return;

            int firstGap = authorJoints.IndexOf(null);
            if (firstGap >= 0)
                authorJoints[firstGap] = joint;
            else
                authorJoints.Add(joint);
        }

        public void RegisterDynamicJoint(Joint joint, ActorSystem.IActor authorActor)
        {
            LinkJointToActor(joint, authorActor);

            if (joint.connectedBody == null)
                return;

            ActorSystem.IActor connectedActor = joint.connectedBody.GetComponentInParent<ActorSystem.IActor>();
            if (connectedActor == null)
            {
                Debug.LogWarning($"Dynamic Joint connected to body {joint.connectedBody.name}. That isn't a part of an actor! " +
                    $"If {joint.connectedBody.name} respawns, dynamic joints won't be cleared.");
                return;
            }

            if (connectedActor != authorActor)
                LinkJointToActor(joint, connectedActor);
        }

        private void OnDispose()
        {
            actorSystem.OnBeforeActorDespawn -= DetachDynamicJoints;
            actorSystem.OnDispose -= OnDispose;

            actorSystem = null;
        }
    }
}
