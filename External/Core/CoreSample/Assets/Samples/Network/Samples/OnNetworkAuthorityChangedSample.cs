using UnityEngine;

namespace NBG.Net.Sample
{
    public class OnNetworkAuthorityChangedSample : MonoBehaviour, INetBehavior
    {
        public bool IAmServer = true;

        //This is called automatically everytime then level is loaded (client only) as by default we assume that we are server
        //And
        //In the middle of level if authority is changed (not currently supported as changing will trigger level load in Sample)
        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Client:
                    IAmServer = false;
                    //Do stuff here for disabling behaviour like unregister from fixed update, stop coroutines, stop game systems, etc.
                    //Rigidbodies are automatically handled and will become kinematic
                    NetSampleSceneHelper.Instance.ApplyLog($"[OnNetworkAuthorityChangedSample] {gameObject.name} is now Client");
                    break;
                case NetworkAuthority.Server:
                    IAmServer = true;
                    //This is not called on initial run. It will be called just after authority changed, but not on initial server authority.
                    //Do stuff here for reregistering for fixed update and stuff.
                    NetSampleSceneHelper.Instance.ApplyLog($"[OnNetworkAuthorityChangedSample] {gameObject.name} is now Server");
                    break;
            }
        }
    }
}
