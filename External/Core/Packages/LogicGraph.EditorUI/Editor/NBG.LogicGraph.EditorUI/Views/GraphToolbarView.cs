using NBG.Core;
using NBG.Core.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Utility bar view
    /// </summary>
    internal class GraphToolbarView : VisualElement
    {
        private Toolbar toolbar;

        private BlackboardView blackboardView;
        private InspectorView inspectorView;
        private SettingsView settingsView;

        private ToolbarToggle toggleBlackboard;
        private ToolbarToggle toggleInspector;
        private ToolbarToggle toggleSettings;
        
        private const string k_UXMLGUID = "4d5b3b2722243f54194009a89b6dae4c";
        public new class UxmlFactory : UxmlFactory<GraphToolbarView, UxmlTraits> { }

        public GraphToolbarView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            toolbar = this.Q<Toolbar>();

            toggleBlackboard = toolbar.Q<ToolbarToggle>("toggleBlackboard");
            toggleInspector = toolbar.Q<ToolbarToggle>("toggleInspector");
            toggleSettings = toolbar.Q<ToolbarToggle>("toggleSettings");

            toggleBlackboard.RegisterCallback<ChangeEvent<bool>>(OnBlackboardToggle);
            toggleBlackboard.SetValueWithoutNotify(EditorPrefsManager.BlackboardVisible);

            toggleInspector.RegisterCallback<ChangeEvent<bool>>(OnInspectorToggle);
            toggleInspector.SetValueWithoutNotify(EditorPrefsManager.InspectorVisible);

            toggleSettings.RegisterCallback<ChangeEvent<bool>>(OnSettingsToggle);
            toggleSettings.SetValueWithoutNotify(EditorStateManager.SettingsViewVisibility);
            toggleSettings.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("d_Settings@2x");
            toggleSettings.style.unityBackgroundImageTintColor = Color.cyan;

            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
            {
                this.Q<Label>("header").FixLightSkinLabel();
                toggleBlackboard.Q<Label>().FixLightSkinLabel();
                toggleInspector.Q<Label>().FixLightSkinLabel();
            }

            this.FixTabBackgroundColor();
        }

        public void Initialize(SettingsView settingsView, BlackboardView blackboardView, InspectorView inspectorView)
        {
            this.settingsView = settingsView;
            this.blackboardView = blackboardView;
            this.inspectorView = inspectorView;
        }

        private void OnInspectorToggle(ChangeEvent<bool> evt)
        {
            EditorPrefsManager.InspectorVisible = evt.newValue;
            inspectorView.SetVisibility(evt.newValue);
        }

        private void OnBlackboardToggle(ChangeEvent<bool> evt)
        {
            EditorPrefsManager.BlackboardVisible = evt.newValue;
            blackboardView.SetVisibility(evt.newValue);
        }

        private void OnSettingsToggle(ChangeEvent<bool> evt)
        {
            EditorStateManager.SettingsViewVisibility = evt.newValue;
            settingsView.SetVisibility(evt.newValue);
        }
    }
}
