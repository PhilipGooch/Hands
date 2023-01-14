using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NBG.Joints
{
    class JointsEditorSettings : ScriptableObject
    {
        const string SettingsPath = "Assets/Settings/Editor/NBG.Joints.Settings.asset";

        private readonly static string[] DefaultPrismaticJointProfileGUIDs = new string[] {
            "55ac01f82ff9ec84794644a14ce7fe08", // Data/PrismaticJointProfile.Stable.asset
            "3cd97008936e04a41b092a51cd70b996", // Data/PrismaticJointProfile.Springy.asset
            "f64302f6e08fb094f9b2670a589f5101", // Data/PrismaticJointProfile.Strict.asset
        };
        private readonly static string[] DefaultRevoluteJointProfileGUIDs = new string[] {
            "5b6b0154f1aa6db448f66c0ad4203d8f", //Springy
            "c7b309c2d31be634ca2516a8107089be", //Stable
            "39f54709dac2675499f889b0a8330ad1" //Strict
        };

        [SerializeField]
        List<PrismaticJointProfile> m_PrismaticJointProfiles = new List<PrismaticJointProfile>();

        [SerializeField]
        List<RevoluteJointProfile> m_RevoluteJointProfiles = new List<RevoluteJointProfile>();

        public List<PrismaticJointProfile> PrismaticJointProfiles => m_PrismaticJointProfiles;
        public List<RevoluteJointProfile> RevoluteJointProfiles => m_RevoluteJointProfiles;

        internal static JointsEditorSettings GetOrCreateSettings()
        {
            JointsEditorSettings settings = AssetDatabase.LoadAssetAtPath<JointsEditorSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<JointsEditorSettings>();

                foreach (var guid in DefaultPrismaticJointProfileGUIDs)
                {
                    settings.m_PrismaticJointProfiles.Add(AssetDatabase.LoadAssetAtPath<PrismaticJointProfile>(AssetDatabase.GUIDToAssetPath(guid)));
                }

                foreach (var guid in DefaultRevoluteJointProfileGUIDs)
                {
                    settings.m_RevoluteJointProfiles.Add(AssetDatabase.LoadAssetAtPath<RevoluteJointProfile>(AssetDatabase.GUIDToAssetPath(guid)));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }
}
