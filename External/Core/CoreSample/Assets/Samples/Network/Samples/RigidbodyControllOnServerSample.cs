using NBG.Core;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Net.Sample
{
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyControllOnServerSample : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate, INetBehavior
    {
        public float amplitude = 2;
        public float frequency = 0.3f;

        ReBody reBody;
        Vector3 startingPosition;

        void IManagedBehaviour.OnLevelLoaded()
        {
            reBody = new ReBody(GetComponent<Rigidbody>());
            startingPosition = reBody.position;

            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IOnFixedUpdate.OnFixedUpdate()
        {
            var tmpPosition = startingPosition;
            tmpPosition.y = Mathf.Sin(Time.fixedTime * frequency) * amplitude;
            reBody.position = tmpPosition;
        }

        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Server:
                    OnFixedUpdateSystem.Register(this);
                    break;
                case NetworkAuthority.Client:
                    OnFixedUpdateSystem.Unregister(this);
                    break;
            }
        }
    }
}
