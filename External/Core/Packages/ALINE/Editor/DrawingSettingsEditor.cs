using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Drawing {
	/// <summary>Helper for adding project settings</summary>
	static class ALINESettingsRegister {
		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider () {
			// First parameter is the path in the Settings window.
			// Second parameter is the scope of this setting: it only appears in the Project Settings window.
			var provider = new SettingsProvider("Project/ALINE", SettingsScope.Project) {
				// By default the last token of the path is used as display name if no label is provided.
				label = "ALINE",
				guiHandler = (searchContext) =>
				{
					var settings = new SerializedObject(DrawingSettings.GetSettingsAsset());
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of lines when in front of objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of lines when behind or inside objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Solids", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of solid objects when in front of other objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of solid objects when behind or inside other objects"));
					EditorGUILayout.HelpBox("Opacity of lines and solid objects drawn using ALINE. When drawing behind other objects, an additional opacity multiplier is applied.", MessageType.None);
					settings.ApplyModifiedProperties();
					if (GUILayout.Button("Reset to default")) {
						var def = DrawingSettings.DefaultSettings;
						var current = DrawingSettings.GetSettingsAsset();
						current.settings.lineOpacity = def.lineOpacity;
						current.settings.lineOpacityBehindObjects = def.lineOpacityBehindObjects;
						current.settings.solidOpacity = def.solidOpacity;
						current.settings.solidOpacityBehindObjects = def.solidOpacityBehindObjects;
						EditorUtility.SetDirty(current);
					}
				},

				// Populate the search keywords to enable smart search filtering and label highlighting:
				keywords = new HashSet<string>(new[] { "Drawing", "Wire", "aline", "opacity" })
			};

			return provider;
		}
	}
}
