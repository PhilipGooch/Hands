using NBG.Core;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Utility bar to display current selection state and change animation state (Play/Edit/Preview)
    /// Also contains settings and other utility fields
    /// </summary>
    public class ToolbarView : VisualElement
    {
        private const string k_UXMLGUID = "444ae90a0356de447bb00a20bdfadcc2";
        public new class UxmlFactory : UxmlFactory<ToolbarView, UxmlTraits> { }

        private readonly IntegerField framesCount;
        private readonly IntegerField selectedFrame;
        private readonly FloatField playbackSpeed;
        private readonly Toggle hideNoDataTracks;
        private readonly Toggle showDebugGizmos;

        public ObjectField fileField;

        private readonly VisualElement framesCountFieldInputField;
        private readonly VisualElement playbackSpeedFieldInputField;

        private readonly Button playButton;
        private readonly Button editButton;
        private readonly Button previewButton;
        private readonly Button openSettingsButton;

        private Button debugToggleButton;
        private Vector2Field moveOverride;
        private FloatField turnSpeed;

        private readonly Toggle looping;

        private AnimatorWindowAnimationData data;

        private Color active = new Color32(103, 235, 52, 255);
        private Color inactive = new Color32(255, 81, 0, 255);

        private VisualElement additionalSettingsParent;

        public ToolbarView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            framesCount = this.Q<IntegerField>("timelineLength");
            selectedFrame = this.Q<IntegerField>("selectedFrame");
            fileField = this.Q<ObjectField>("animationFile");
            playbackSpeed = this.Q<FloatField>("playbackSpeed");
            hideNoDataTracks = this.Q<Toggle>("hideNoDataTracks");
            showDebugGizmos = this.Q<Toggle>("showDebugGizmos");

            playButton = this.Q<Button>("play");
            editButton = this.Q<Button>("edit");
            previewButton = this.Q<Button>("preview");
            openSettingsButton = this.Q<Button>("openSettings");

            looping = this.Q<Toggle>("looping");
            looping.RegisterValueChangedCallback((evt) =>
            {
                data.noodleAnimatorData.animation.looped = evt.newValue;
                data.StateChanged();
            });

            showDebugGizmos.RegisterValueChangedCallback((evt) =>
            {
                NoodleAnimator.showDebugGizmos = evt.newValue;
            });

            playButton.style.color = Color.black;
            editButton.style.color = Color.black;
            previewButton.style.color = Color.black;

            framesCountFieldInputField = framesCount.QInputField<int>();
            playbackSpeedFieldInputField = playbackSpeed.QInputField<float>();

            SetupAdditionalSettings();

            playButton.clickable.clicked += () =>
            {
                if (data == null)
                    return;

                data.playbackController.SetMode(PlayBackMode.Play);
                data.StateChanged();
            };

            editButton.clickable.clicked += () =>
            {
                if (data == null)
                    return;

                data.playbackController.SetMode(PlayBackMode.Edit);
                data.StateChanged();
            };

            previewButton.clickable.clicked += () =>
            {
                if (data == null)
                    return;

                data.playbackController.SetMode(PlayBackMode.Preview);
                data.StateChanged();
            };

            ElementsUtils.SetElementMinWidth<Label>(framesCount, 100);
            ElementsUtils.SetElementMinWidth<Label>(selectedFrame, 100);
            ElementsUtils.SetElementMinWidth<Label>(playbackSpeed, 100);
            ElementsUtils.SetElementMinWidth<Label>(fileField, 40);
            ElementsUtils.SetElementMinWidth<Label>(looping, 55);

            //i hate this, just expose input field types!
            framesCountFieldInputField.style.width = 50;
            selectedFrame.QInputField<int>().style.width = 50;
            playbackSpeedFieldInputField.style.width = 50;

            fileField.objectType = typeof(PhysicalAnimation);

            SetupFramesCountSelection(0);
            SetupFrameSelection(0);
            SetupPlaybackSpeedSelection(1);
            SetupHideNodeDataTracksToggle();
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;

            looping.SetValueWithoutNotify(data.noodleAnimatorData.animation.looped);
            playbackSpeed.SetValueWithoutNotify(data.playbackController.GetPlaybackSpeed());

            hideNoDataTracks.SetValueWithoutNotify(SessionStateManager.GetHideEmptyTracks());

            Update();
        }

        private void SetupAdditionalSettings()
        {
            openSettingsButton.clickable.clicked += () =>
            {
                additionalSettingsParent.SetVisibility(!additionalSettingsParent.visible);
            };

            additionalSettingsParent = this.Q<VisualElement>("additionalSettingsParent");

            moveOverride = additionalSettingsParent.Q<Vector2Field>("moveOverride");
            turnSpeed = additionalSettingsParent.Q<FloatField>("turnSpeed");
            debugToggleButton = additionalSettingsParent.Q<Button>("toggleDebugMode");

            var xFloatField = moveOverride.Q<FloatField>("unity-x-input");
            var yFloatField = moveOverride.Q<FloatField>("unity-y-input");
            xFloatField.QInputField<float>().style.width = 80;
            yFloatField.QInputField<float>().style.width = 80;
            turnSpeed.QInputField<float>().style.width = 50;

            ElementsUtils.SetElementMinWidth<Label>(moveOverride, 100);
            ElementsUtils.SetElementMinWidth<Label>(turnSpeed, 100);

            debugToggleButton.clickable.clicked += ToggleDebugSkin;

            moveOverride.SetValueWithoutNotify(RootMovementOverrides.moveOverride);
            moveOverride.RegisterValueChangedCallback((evt) => RootMovementOverrides.moveOverride = evt.newValue);

            turnSpeed.SetValueWithoutNotify(RootMovementOverrides.turnSpeed);
            turnSpeed.RegisterValueChangedCallback((evt) => RootMovementOverrides.turnSpeed = evt.newValue);

            additionalSettingsParent.SetVisibility(false);
        }

        private void ToggleDebugSkin()
        {
            if (Application.isPlaying)
            {
                NoodleDebugSkinToggleAdapter.adapter.Toggle();

                string showSkin = NoodleDebugSkinToggleAdapter.adapter.showSkin ? "T" : "F";
                string showDebug = NoodleDebugSkinToggleAdapter.adapter.showDebug ? "T" : "F";

                debugToggleButton.text = $"Debug: Skin({showSkin}) : Debug ({showDebug})";
            }
        }

        internal void Update()
        {
            if (data == null)
                return;

            //these values can only change here, so no point updating them from backend constantly.
            //this also makes editing them way more smooth, since they are not constantly overwritten.
            if (data.refreshSource != RefreshSource.InputFieldValueChanged)
            {
                framesCount.SetValueWithoutNotify(data.noodleAnimatorData.animation.frameLength);
                playbackSpeed.SetValueWithoutNotify(data.playbackController.GetPlaybackSpeed());
            }

            selectedFrame.SetValueWithoutNotify(data.noodleAnimatorData.currentFrame);

            UpdateControlButtons();
        }

        private void UpdateControlButtons()
        {
            switch (data.noodleAnimatorData.playbackMode)
            {
                case PlayBackMode.Edit:
                    playButton.style.backgroundColor = inactive;
                    editButton.style.backgroundColor = active;
                    previewButton.style.backgroundColor = inactive;
                    break;
                case PlayBackMode.Play:
                    playButton.style.backgroundColor = active;
                    editButton.style.backgroundColor = inactive;
                    previewButton.style.backgroundColor = inactive;
                    break;
                case PlayBackMode.Preview:
                    playButton.style.backgroundColor = inactive;
                    editButton.style.backgroundColor = inactive;
                    previewButton.style.backgroundColor = active;
                    break;
                default:
                    break;
            }
        }

        public void SetFrameCountFieldValue(int value)
        {
            framesCount.SetValueWithoutNotify(value);
        }

        public void SetupFramesCountSelection(int initialValue)
        {
            framesCount.SetValueWithoutNotify(initialValue);
            framesCount.RegisterValueChangedCallback((evt) =>
            {
                if (data == null)
                    return;

                data.refreshSource = RefreshSource.InputFieldValueChanged;
                data.playbackController.SetFrameLength(evt.newValue);
                data.undoController.OverwriteUndo();
                data.StateChanged();
            });

            framesCountFieldInputField.RegisterCallback<FocusInEvent>((_) =>
            {
                if (data == null)
                    return;

                if (data.refreshSource != RefreshSource.KeyboardOperation)
                {
                    data.currentFocus.focusedElement = framesCountFieldInputField;
                    data.undoController.RecordUndo();
                }
            });
        }

        public void SetupFrameSelection(int initialValue)
        {
            selectedFrame.SetValueWithoutNotify(initialValue);
            selectedFrame.RegisterValueChangedCallback((evt) =>
            {
                if (data == null)
                    return;

                data.SetCursorPosition(evt.newValue);
            });
        }

        public void SetupPlaybackSpeedSelection(int initialValue)
        {
            playbackSpeed.SetValueWithoutNotify(initialValue);
            playbackSpeed.RegisterValueChangedCallback((evt) =>
            {
                if (data == null)
                    return;

                data.refreshSource = RefreshSource.InputFieldValueChanged;
                data.playbackController.SetPlaybackSpeed(evt.newValue);
                data.undoController.OverwriteUndo();
                data.StateChanged();
            });

            playbackSpeedFieldInputField.RegisterCallback<FocusInEvent>((_) =>
            {
                if (data == null)
                    return;

                if (data.refreshSource != RefreshSource.KeyboardOperation)
                {
                    data.currentFocus.focusedElement = playbackSpeedFieldInputField;
                    data.undoController.RecordUndo();
                }
            });
        }

        internal void SetupHideNodeDataTracksToggle()
        {
            hideNoDataTracks.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                SessionStateManager.SetHideEmptyTracks(evt.newValue);
                data.StateChanged();
            });
        }

        public void SetupFileSelection(Action<PhysicalAnimation> newFileSet, PhysicalAnimation initialValue = null)
        {
            fileField.SetValueWithoutNotify(initialValue);

            fileField.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) =>
            {
                var anim = (PhysicalAnimation)evt.newValue;
                EditorPrefsManager.SaveLastEditedAniamtion(AssetDatabase.GetAssetPath(evt.newValue));
                newFileSet(anim);
            });

            if (initialValue != null)
                newFileSet(initialValue);
        }
    }
}
