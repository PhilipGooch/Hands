using NBG.Core.Events;
using NBG.Core.Streams;
using NBG.Net;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Actor
{
    /// <summary>
    /// Module responsible for hooking up actor net events.
    /// </summary>
    public class ActorNetModule
    {
        internal struct ActorAndInitialState
        {
            public ActorSystem.IActor actor;
            public bool initialEnabled;

            public ActorAndInitialState(ActorSystem.IActor actor, bool initialEnabled)
            {
                this.actor = actor;
                this.initialEnabled = initialEnabled;
            }
        }

        private readonly Dictionary<int, bool> actorInitialState = new Dictionary<int, bool>();

        private readonly HashSet<int> modifiedActorEnabled = new HashSet<int>();

        private ActorSystem actorSystem;

        internal readonly Dictionary<int, int> actorGameObjectToActorID = new Dictionary<int, int>();
        private readonly Dictionary<int, int> actorIDToActorGameObject = new Dictionary<int, int>();

        internal ActorNetModule(ActorSystem actorManager)
        {
            actorSystem = actorManager;

            var eventBus = EventBus.Get();
            eventBus.Register<ActorSystem.ChangeActiveStateEvent>(OnSetActive);
            eventBus.Register<NewPeerIsReadyEvent>(OnNewPeerIsReady);

            actorSystem.OnActorRegister += OnActorRegister;
            actorSystem.OnAfterActorUnregister += OnActorUnregister;
            actorSystem.OnDispose += Dispose;
        }

        private void OnActorRegister (int actorID, ActorSystem.IActor actor)
        {
            int gameObjectID = actor.ActorGameObject.GetInstanceID();
            actorGameObjectToActorID[gameObjectID] = actorID;
            actorIDToActorGameObject[actorID] = gameObjectID;

            actorInitialState[actorID] = actor.ActorGameObject.activeSelf;
        }

        private void OnActorUnregister (int actorID)
        {
            int gameObjectID = actorIDToActorGameObject[actorID];
            actorGameObjectToActorID.Remove(gameObjectID);
            actorIDToActorGameObject.Remove(actorID);

            actorInitialState.Remove(actorID);
        }

        private void OnSetActive(ActorSystem.ChangeActiveStateEvent actorActive)
        {
            Debug.Assert(actorInitialState.ContainsKey(actorActive.actorID), $"{actorActive.actorID} is not a registered Actor. Currently registered initial state count: {actorInitialState.Count}");

            if (actorInitialState[actorActive.actorID] == actorActive.activeValue)
                modifiedActorEnabled.Remove(actorActive.actorID);
            else
                modifiedActorEnabled.Add(actorActive.actorID);
        }

        private void Dispose()
        {
            var eventBus = EventBus.Get();
            eventBus?.Unregister<ActorSystem.ChangeActiveStateEvent>(OnSetActive);
            eventBus?.Unregister<NewPeerIsReadyEvent>(OnNewPeerIsReady);

            actorInitialState.Clear();
            modifiedActorEnabled.Clear();

            actorSystem.OnActorRegister -= OnActorRegister;
            actorSystem.OnAfterActorUnregister -= OnActorUnregister;
            actorSystem.OnDispose -= Dispose;

            actorSystem = null;
        }

        private void OnNewPeerIsReady(NewPeerIsReadyEvent e)
        {
            foreach (int actorID in modifiedActorEnabled)
            {
                bool modifiedState = !actorInitialState[actorID];
                e.CallOnPeer(new ActorSystem.ChangeActiveStateEvent { actorID = actorID, activeValue = modifiedState });
            }
        }

        [NetEventBusSerializer]
        public class ChangeActiveStateEvent_NetSerializer : IEventSerializer<ActorSystem.ChangeActiveStateEvent>
        {
            public void Serialize(IStreamWriter writer, ActorSystem.ChangeActiveStateEvent data)
            {
                // TODO: WriteGameObject fails on dynamically created items
                //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, ActorSystem.Main.Actors[data.actorID].ActorGameObject);
                writer.Write(data.activeValue);
            }

            public ActorSystem.ChangeActiveStateEvent Deserialize(IStreamReader reader)
            {
                //int actorGameObjectID = ObjectIdDatabaseResolver.instance.ReadGameObject(reader).GetInstanceID();

                return new ActorSystem.ChangeActiveStateEvent()
                {
                    // TODO: WriteGameObject fails on dynamically created items
                    //actorID = ActorSystem.NetModule.actorGameObjectToActorID[actorGameObjectID], // The need to go through singleton is a bit annoying here.
                    activeValue = reader.ReadBool(),
                };
            }
        }

        [NetEventBusSerializer]
        public class RespawnEvent_NetSerializer : IEventSerializer<ActorSystem.RespawnEvent>
        {
            public void Serialize(IStreamWriter writer, ActorSystem.RespawnEvent data)
            {
                // TODO: WriteGameObject fails on dynamically created items
                //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, ActorSystem.Main.Actors[data.actorID].ActorGameObject);
                // NOTE: We don't teleport on clients and we do this by writing event data.
            }

            public ActorSystem.RespawnEvent Deserialize(IStreamReader reader)
            {
                //int actorGameObjectID = ObjectIdDatabaseResolver.instance.ReadGameObject(reader).GetInstanceID();
                return new ActorSystem.RespawnEvent()
                {
                    // TODO: WriteGameObject fails on dynamically created items
                    //actorID = ActorSystem.NetModule.actorGameObjectToActorID[actorGameObjectID],
                    applyTeleport = false,
                };
            }
        }
    }
}
