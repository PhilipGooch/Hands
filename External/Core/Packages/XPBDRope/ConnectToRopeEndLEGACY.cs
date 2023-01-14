using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;

namespace NBG.XPBDRope
{
    public class ConnectToRopeEndLEGACY : MonoBehaviour
    {
        [SerializeField]
        public Rope target;
        [SerializeField]
        public ConfigurableJoint joint;
        /*[SerializeField]
        [Tooltip("Fix the rope end position. Use this only if the rope end does not move and is attached to a kinematic rigidbody or similar.")]
        bool fixPosition = false;
        [SerializeField]
        [Tooltip("Should we move the whole rope to snap to the connection point? Only enable this if the rope end is being attached to a kinematic rigidbody or similar. " +
            "This will provide some extra stability in that specific setup. Do not enable otherwise.")]
        bool moveRope = false;*/

        private void OnEnable()
        {
            Debug.LogError("LEGACY SCRIPT DETECTED! Run the validation tests to remove this.", gameObject);
        }
    }
}

