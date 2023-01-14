using NBG.Core;
using NBG.Core.Events;
using NBG.Core.ObjectIdDatabase;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Sample
{
    //this struct is used for passing data betweeen server and client. We can only pass structs
    public struct StructForSyncingSomeState
    {
        /// <summary>
        /// this game object is used for checking if message received is owned by that gameobject.
        /// It doesn't need to be gameObject, it can be any identifier if it is possible to have deterministic counter or smthg
        /// Currently ObjectIDDatabase doesn't work with dynamically created objects, so you can use some int ID for example instead
        /// </summary>
        public GameObject GameObjectToWrite;
        public int StateToSync;
    }

    //This is used to tell how StructForSyncingSomeState needs to be serialized over net.
    [NetEventBusSerializer]
    public class StructForSyncingSomeState_NetSerializer : IEventSerializer<StructForSyncingSomeState>
    {
        const int BITS = 8;
        public StructForSyncingSomeState Deserialize(IStreamReader reader)
        {
            var data = new StructForSyncingSomeState();
            data.GameObjectToWrite = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);//we are receiving gameobject from database
            data.StateToSync = reader.ReadInt32(BITS);

            return data;
        }
        public void Serialize(IStreamWriter writer, StructForSyncingSomeState data)
        {
            ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectToWrite);//take gameobject id from database and write it to stream
            writer.Write(data.StateToSync, BITS);
        }
    }

    public class CustomEventsSample : MonoBehaviour, IManagedBehaviour
    {
        public int someStateWeWantToSync;

        void IManagedBehaviour.OnLevelLoaded()
        {
            //Just as an example, but probably most efficient way would be to use some gamesystem and then only that gamesystem would register for listening and then decide that to do with events
            EventBus.Get().Register<StructForSyncingSomeState>(SyncStateEvent);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            EventBus.Get().Unregister<StructForSyncingSomeState>(SyncStateEvent);
        }

        public void SendInfo()
        {
            someStateWeWantToSync = Random.Range(0, 128);

            var evt = new StructForSyncingSomeState()
            {
                GameObjectToWrite = gameObject,
                StateToSync = someStateWeWantToSync,
            };
            NetSampleSceneHelper.Instance.ApplyLog($"[CustomEventsSample] {gameObject.name} sent Info someStateWeWantToSync:{someStateWeWantToSync}");
            EventBus.Get().Send(evt);
        }

        void SyncStateEvent(StructForSyncingSomeState eventData)
        {
            //Checks if received message belongs to this instance.
            if (eventData.GameObjectToWrite == gameObject)
            {
                someStateWeWantToSync = eventData.StateToSync;
                NetSampleSceneHelper.Instance.ApplyLog($"[CustomEventsSample] {gameObject.name} received Info someStateWeWantToSync:{someStateWeWantToSync}");
            }
        }
    }
}
