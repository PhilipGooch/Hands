using UnityEngine;

namespace NBG.Joints
{
    [CreateAssetMenu(fileName = "RevoluteJointProfile", menuName = "[NBG] Joints/Revolute Joint Profile", order = 1)]
    /// <summary>
    /// Stores spring damp data presets for prismatic joints.
    /// </summary>
    public class RevoluteJointProfile : ScriptableObject
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
