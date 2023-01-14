using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    public class BlockGrabVolume : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<NoodleHand>();
            if (hand != null)
                hand.ReleaseGrabOnFixedUpdate(.1f);
        }
        private void OnTriggerStay(Collider other)
        {
            OnTriggerEnter(other);
        }
    }
}
