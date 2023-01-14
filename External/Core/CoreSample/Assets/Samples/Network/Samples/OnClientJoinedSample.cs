using NBG.Core;
using NBG.Core.Events;
using UnityEngine;

namespace NBG.Net.Sample
{
    public class OnClientJoinedSample : MonoBehaviour, IManagedBehaviour
    {
        public int someStateWeWantToSync;

        void IManagedBehaviour.OnLevelLoaded()
        {
            var events = EventBus.Get();

            //Just as an example, but probably most efficient way would be to use some gamesystem and then only that gamesystem would register for listening and then decide that to do with events
            events.Register<StructForSyncingSomeState>(SyncStateEvent);
            events.Register<NewPeerIsReadyEvent>(OnNewPeerIsReady);

            someStateWeWantToSync = Random.Range(0, 128);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            var events = EventBus.Get();
            events.Unregister<StructForSyncingSomeState>(SyncStateEvent);
            events.Unregister<NewPeerIsReadyEvent>(OnNewPeerIsReady);
        }

        void SyncStateEvent(StructForSyncingSomeState eventData)
        {
            //Checks if received message belongs to this instance.
            if (eventData.GameObjectToWrite == gameObject)
            {
                someStateWeWantToSync = eventData.StateToSync;
                NetSampleSceneHelper.Instance.ApplyLog($"[OnClientJoinedSample] {gameObject.name} received Info someStateWeWantToSync:{someStateWeWantToSync}");
            }
        }

        //This will be fired on server, then new client is joined up.
        //We can send current state with other events
        void OnNewPeerIsReady(NewPeerIsReadyEvent e)
        {
            var evt = new StructForSyncingSomeState()
            {
                GameObjectToWrite = gameObject,//this is required for client to which gameobject should receive this message, as it can be multiple instances of same listeners. We only support one net object per gameobject
                StateToSync = someStateWeWantToSync,
            };
            NetSampleSceneHelper.Instance.ApplyLog($"[OnClientJoinedSample] {gameObject.name} sent Info someStateWeWantToSync:{someStateWeWantToSync}");
            //send current data just for the connected client
            e.CallOnPeer(evt);
        }
    }
}
