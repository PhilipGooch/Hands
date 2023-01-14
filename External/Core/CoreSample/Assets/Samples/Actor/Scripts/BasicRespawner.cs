using NBG.Actor;
using NBG.Core;
using Recoil;
using UnityEngine;

public class BasicRespawner : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
{
    bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
        OnFixedUpdateSystem.Register(this);
    }

    void IManagedBehaviour.OnLevelLoaded() { }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        OnFixedUpdateSystem.Unregister(this);
    }

    void IOnFixedUpdate.OnFixedUpdate()
    {
        for (int i = 0; i < ActorSystem.Main.Actors.Count; i++)
        {
            ActorSystem.IActor actor = ActorSystem.Main.Actors[i];
            // Actor list is with gaps because index doubles as actor id.
            if (actor == null || !ActorSystem.Main.IsActorActive(actor))
                continue;

            if (World.main.GetBodyPosition(actor.PivotBodyID).pos.y < -10f)
                ActorSystem.Main.RequestRespawn(actor);
        }
    }
}
