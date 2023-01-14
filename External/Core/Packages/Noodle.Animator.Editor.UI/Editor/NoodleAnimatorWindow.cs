using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Tool window - root element, updates and manages other tool components
    /// </summary>
    public class NoodleAnimatorWindow : EditorWindow, IOnPlayFrameChange
    {
        private const string k_UXMLGUID = "0ca62ee21915bad4a9793959a2dd1deb";

        private VisualElement root;
        private ToolbarView toolbarView;
        private TimelineView timelineView;
        private InspectorView inspectorView;

        private AnimatorWindowAnimationData data;

        private NoodleAnimationEditorShortcuts shortcuts;

        private static PhysicalAnimation loadAnimation = null;
        private static ToolbarView _toolbarView;

        [MenuItem("No Brakes Games/Noodle Animator...")]
        public static void OpenWindow()
        {
            NoodleAnimatorWindow wnd = GetWindow<NoodleAnimatorWindow>();
            wnd.titleContent = new GUIContent("Noodle Animator Window");
            wnd.minSize = new Vector2(970, 200);
        }

        public static void OpenWindowWithAsset(PhysicalAnimation file = null)
        {
            loadAnimation = file;
            OpenWindow();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            root.focusable = true;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(root);

            timelineView = root.Q<TimelineView>("timelineView");
            inspectorView = root.Q<InspectorView>("inspectorView");
            toolbarView = root.Q<ToolbarView>("toolbar");

            timelineView.onScroll += TimelineScrolled;
            inspectorView.onScroll += InspectorScrolled;
            timelineView.focusable = true;
            shortcuts = new NoodleAnimationEditorShortcuts();
            inspectorView.RegisterCallback<KeyDownEvent>(shortcuts.OnKeyDownInspector, TrickleDown.TrickleDown);
            timelineView.RegisterCallback<KeyDownEvent>(shortcuts.OnKeyDownTimeline, TrickleDown.TrickleDown);
            root.Focus();

            root.RegisterCallback<GeometryChangedEvent>((_) =>
            {
                var scrollValue = inspectorView.GetVerticalScrollValue();
                inspectorView.SetHorizontalScrollVisibility(timelineView.HorizontalScrollBarVisible);
                inspectorView.SetVerticalScrollValue(scrollValue);
                timelineView.SetVerticalScrollValue(scrollValue);
            });

            toolbarView.SetupFileSelection(FileSelected, loadAnimation == null ? GetPreviousAnimation() : loadAnimation);
            _toolbarView = toolbarView;
        }

        private void OnDestroy()
        {
            if (data != null)
            {
                data.playbackController.SetMode(PlayBackMode.Preview);
            }
        }

        private void Update()
        {
            if (data == null)
                return;

            if (data.stateChanged)
            {
                data.stateChanged = false;
                UpdateAll();
            }
        }

        private PhysicalAnimation GetPreviousAnimation()
        {
            var path = EditorPrefsManager.GetLastEditedAnimationPath();
            return (PhysicalAnimation)AssetDatabase.LoadAssetAtPath(path, typeof(PhysicalAnimation));
        }

        private void UpdateAll()
        {
            data.Update();

            timelineView.Update();
            inspectorView.Update();
            toolbarView.Update();

            data.ResetState();
        }

        private void UpdatePlayMode()
        {
            timelineView.UpdatePlayMode();
            inspectorView.Update();
        }

        private void FileSelected(PhysicalAnimation anim)
        {
            if (data != null)
                data.playbackController.SetMode(PlayBackMode.Preview);

            var noodleAnimationEditorData = new NoodleAnimationEditorData(anim)
            {
                onPlayFrameChange = this
            };

            
            data = new AnimatorWindowAnimationData(noodleAnimationEditorData, data);
            data.playbackController.SetFrame(SessionStateManager.GetCursorPosition());
            //something is strange with frameLength, does it not start from 0?
            toolbarView.SetFrameCountFieldValue(noodleAnimationEditorData.animation.frameLength + 1);

            timelineView.SetNewDataFile(data);
            inspectorView.SetNewDataFile(data);
            toolbarView.SetNewDataFile(data);

            shortcuts.Initialize(data, ArrowKeysVerticalScroll, ArrowKeysHorizontalScroll);
        }

        //to get when cursor moved in play mode
        public void OnChange()
        {
            UpdatePlayMode();
        }

        #region Scrolling 

        private void InspectorScrolled(float value)
        {
            timelineView.SetVerticalScrollValue(value);
        }

        private void TimelineScrolled(float value)
        {
            inspectorView.SetVerticalScrollValue(value);
        }

        private void ArrowKeysVerticalScroll(int sign)
        {
            var result = timelineView.IsTrackHidden(data.selectionController.LastSelectedTrack);
            if (result.direction == sign)
            {
                timelineView.AddToVerticalScrollValue(result.amountToUnhide);
                inspectorView.AddToVerticalScrollValue(result.amountToUnhide);
            }
        }

        private void ArrowKeysHorizontalScroll(int change)
        {
            timelineView.ArrowKeysHorizontalScroll(change);
        }

        #endregion

        public static void LoadAnim(PhysicalAnimation newAnim)
        {
            _toolbarView.fileField.value = newAnim;
        }
    }

    internal static class ElementsUtils
    {
        internal static void SetElementMinWidth<T>(VisualElement parent, float labelWidth) where T : VisualElement
        {
            parent.Q<T>().style.minWidth = labelWidth;
        }

        internal static void SetLabelMinWidth(VisualElement element, float labelWidth)
        {
            element.style.minWidth = labelWidth;
        }
    }
}