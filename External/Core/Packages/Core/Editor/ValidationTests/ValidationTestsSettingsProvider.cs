using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor
{
    class ValidationTestsSettingsProvider : SettingsProvider
    {
        const string kClearBeforeRun = "NBG.Core.ValidationTests.ClearBeforeRun";
        const string kClearBeforeAssist = "NBG.Core.ValidationTests.ClearBeforeAssist";
        const string kClearBeforeFix = "NBG.Core.ValidationTests.ClearBeforeFix";

        class Styles
        {
            public static GUIContent validationTests = new GUIContent("Validation Tests");
        }

        public ValidationTestsSettingsProvider()
            : base("No Brakes Games/Validation Tests", SettingsScope.User)
        {
        }

        public override void OnGUI(string searchContext)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;

            {
                var clearBeforeRun = GetClearBeforeRun();
                EditorGUI.BeginChangeCheck();
                clearBeforeRun = EditorGUILayout.Toggle("Clear Console before Run command", clearBeforeRun);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool(kClearBeforeRun, clearBeforeRun);

                var clearBeforeAssist = GetClearBeforeAssist();
                EditorGUI.BeginChangeCheck();
                clearBeforeAssist = EditorGUILayout.Toggle("Clear Console before Assist command", clearBeforeAssist);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool(kClearBeforeAssist, clearBeforeAssist);

                var clearBeforeFix = GetClearBeforeFix();
                EditorGUI.BeginChangeCheck();
                clearBeforeFix = EditorGUILayout.Toggle("Clear Console before Fix command", clearBeforeFix);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool(kClearBeforeFix, clearBeforeFix);
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new ValidationTestsSettingsProvider();
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }

        public static bool GetClearBeforeRun() => EditorPrefs.GetBool(kClearBeforeRun, false);
        public static bool GetClearBeforeAssist() => EditorPrefs.GetBool(kClearBeforeAssist, false);
        public static bool GetClearBeforeFix() => EditorPrefs.GetBool(kClearBeforeFix, false);
    }
}
