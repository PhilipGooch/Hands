using UnityEngine;

namespace NBG.Joints
{
    [CreateAssetMenu(fileName = "PrismaticJointProfile", menuName = "[NBG] Joints/Prismatic Joint Profile", order = 1)]
    /// <summary>
    /// Stores spring damp data presets for prismatic joints.
    /// </summary>
    public class PrismaticJointProfile : ScriptableObject
    {
        public string profileNameOverride;
        public float spring;
        public float damp;

        public string ProfileName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(profileNameOverride))
                    return profileNameOverride;
                else
                    return name;
            }
        }
    }
}
