using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    [CreateAssetMenu(fileName = "RopeProfile", menuName = "[NBG] Ropes/Rope Profile", order = 1)]
    public class RopeProfile : ScriptableObject
    {
        [SerializeField]
        RopeProfileData profileData = RopeProfileData.CreateDefault();
        public RopeProfileData ProfileData => profileData;
    }
}
