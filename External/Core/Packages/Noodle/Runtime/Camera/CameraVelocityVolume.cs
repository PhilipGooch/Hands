using NBG.Core;
using Recoil;
using Unity.Mathematics;
using UnityEngine;
namespace Noodles
{

    public class CameraVelocityVolume : MonoBehaviour, IManagedBehaviour, ICameraVelocityOverride
    {
        int bodyId;

        [SerializeField]
        private int _priority;
        public int priority => (int)_priority;
        public float3 cameraVelocity => World.main.GetVelocity(bodyId).linear;


        TriggerProximityList<Noodle> nearbyNoodles = new TriggerProximityList<Noodle>(ProximityListComponentSearch.InParents);

        void IManagedBehaviour.OnLevelLoaded()
        {
            bodyId = ManagedWorld.main.FindBody(GetComponentInParent<Rigidbody>());
        }
        void IManagedBehaviour.OnLevelUnloaded()
        {
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            var noodle = nearbyNoodles.OnTriggerEnter(other);
            if (noodle != null)
                CameraOverrideList<ICameraVelocityOverride>.AddOverride(noodle.entity, this);
        }
        private void OnTriggerExit(Collider other)
        {
            var noodle = nearbyNoodles.OnTriggerLeave(other);
            if (noodle != null)
                CameraOverrideList<ICameraVelocityOverride>.RemoveOverride(noodle.entity, this);
        }

    }
}