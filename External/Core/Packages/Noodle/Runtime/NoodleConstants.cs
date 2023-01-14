using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    public class NoodleConstants 
    {
        public const float UPPER_ARM_LENGTH_TEMP = .35f; // upper arm length - todo calculate from structure
        public const float LOWER_ARM_LENGTH_TEMP = .31f; // lower arm length - todo calculate from structure

        public const float UPPER_LEG_LENGTH_TEMP = .22f; // upper arm length - todo calculate from structure
        public const float LOWER_LEG_LENGTH_TEMP = .16f; // lower arm length - todo calculate from structure
        public const float FOOT_ANCHOR_TEMP = .16f-.08f; // actual foot location


        public const float HAND_IK_ANCHOR_SHIFT_TO_CENTER = .1f; // if 0 - IK feedback is applied to shoulder, if .2 - to chest center
        public const float SUSPENSION_TENSION = .5f; // spring rate is calculated to support ragdoll at this compression
    }
}
