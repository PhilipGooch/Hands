using System;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Actor.Tests
{
    public class ActorTestComponent : MonoBehaviour, ActorSystem.IActor
    {
        GameObject ActorSystem.IActor.ActorGameObject => gameObject;
        int ActorSystem.IActor.DefaultSpawnRelativeToBodyID { get; } = Recoil.World.environmentId;

        internal event Action<ActorSystem.IActor> OnAfterDespawn;
        internal event Action<ActorSystem.IActor> OnAfterSpawn;

        private int pivotBodyID = Recoil.World.environmentId;
        int ActorSystem.IActor.PivotBodyID
        {
            get
            {
                if (pivotBodyID == Recoil.World.environmentId)
                    pivotBodyID = Recoil.ManagedWorld.main.FindBody(GetComponent<Rigidbody>());
                return pivotBodyID;
            }
        }

        RigidTransform ActorSystem.IActor.DefaultSpawnPoint => RigidTransform.identity;

        private void OnDestroy()
        {
            ActorSystem.Main?.UnregisterActor(this);
        }

        void ActorSystem.IActorCallbacks.OnAfterDespawn() => OnAfterDespawn?.Invoke(this);
        void ActorSystem.IActorCallbacks.OnAfterSpawn() => OnAfterSpawn?.Invoke(this);
    }
}
