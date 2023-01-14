using NBG.Core;
using UnityEngine;

namespace Recoil
{
    public class Friction : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        public float angularFrictionAcceleration;
        public float linearFrictionAcceleration;
        int bodyId;

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IManagedBehaviour.OnLevelLoaded()
        {
            bodyId = ManagedWorld.main.FindBody(GetComponent<Rigidbody>());
            OnFixedUpdateSystem.Register(this);
        }
        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (angularFrictionAcceleration <= 0 && linearFrictionAcceleration <= 0)
                return;

            ref var v = ref World.main.GetVelocity4(bodyId);
            v.angular = re.MoveTowards(v.angular, 0, angularFrictionAcceleration * World.main.dt);
            v.linear= re.MoveTowards(v.linear, 0, linearFrictionAcceleration * World.main.dt);
        }


        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

    }
}
