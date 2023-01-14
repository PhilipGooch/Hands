using NBG.Core;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Displays animated part name and selected frame values
    /// </summary>
    public class GroupFieldView : VisualElement
    {
        private const string k_UXMLGUID = "b0dd6224191a557408143e6a38c060a5";
        public new class UxmlFactory : UxmlFactory<GroupFieldView, UxmlTraits> { }

        private readonly Label label;
        private readonly FloatField floatField;
        private readonly EnumField enumField;

        private readonly VisualElement floatFieldInputField;

        private int previousSelectedTrackID = -1;

        private AnimatorWindowAnimationData data;
        private Track track;

        public GroupFieldView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            style.height = NoodleAnimatorParameters.rowHeight;

            enumField = this.Q<EnumField>();
            floatField = this.Q<FloatField>();
            floatField.AddToClassList("inspector-group-field-view-root");

            floatFieldInputField = floatField.QInputField<float>();

            enumField.Init(EasingType.Default);
            enumField.value = EasingType.Default;

            label = floatField.Q<Label>();
            ElementsUtils.SetLabelMinWidth(label, 110);

            enumField.RegisterValueChangedCallback((evt) =>
            {
                data.selectionController.SelectTrack(track.trackId);
                data.operationsController.SetKeyEasing((EasingType)evt.newValue);
                data.StateChanged();
            });

            floatField.RegisterValueChangedCallback((evt) =>
            {
                data.refreshSource = RefreshSource.InputFieldValueChanged;
                data.operationsController.SetKeyOnCursor(evt.newValue, false);
                data.undoController.OverwriteUndo();
                data.StateChanged();
            });

            floatField.RegisterCallback<FocusInEvent>((_) =>
            {
                if (data.refreshSource != RefreshSource.KeyDrag)
                {
                    data.selectionController.SelectTrack(track.trackId);
                    data.StateChanged();
                }
            });

            floatFieldInputField.RegisterCallback<FocusInEvent>((_) =>
            {
                if (data.refreshSource != RefreshSource.KeyboardOperation && data.refreshSource != RefreshSource.KeyDrag)
                {
                    data.currentFocus.focusedElement = floatFieldInputField;
                    data.undoController.RecordUndo();
                }

                data.isGroupFieldInputFieldFocused = true;
            });

            floatFieldInputField.RegisterCallback<FocusOutEvent>((_) =>
            {
                data.isGroupFieldInputFieldFocused = false;
            });
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;
        }

        internal void SetData(Track track, EasingType easingType, float value)
        {
            var currentlySelectedTrack = data.selectionController.LastSelectedTrack;

            if (data.selectionController.LastSelectedTrack == track.trackId)
            {
                switch (data.clickType)
                {
                    case ClickType.None:
                        if (data.isGroupFieldInputFieldFocused && previousSelectedTrackID != currentlySelectedTrack && data.refreshSource != RefreshSource.KeyDrag)
                            floatFieldInputField.Focus();
                        break;
                    case ClickType.Single:
                        break;
                    case ClickType.Double:
                        floatFieldInputField.Focus();
                        data.undoController.OverwriteUndo();
                        break;
                }

                SelectVisuals();
            }
            else
            {
                DeselectVisuals();
            }

            //update all not selected tracks fields 
            if (!(data.refreshSource == RefreshSource.InputFieldValueChanged && data.selectionController.LastSelectedTrack == track.trackId))
            {
                floatField.SetValueWithoutNotify(value);
            }

            previousSelectedTrackID = currentlySelectedTrack;
            this.track = track;
            label.text = track.name;

            enumField.SetValueWithoutNotify(easingType);

            this.SetVisibility(!track.hidden);
        }

        private void SelectVisuals()
        {
            style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        }

        private void DeselectVisuals()
        {
            style.backgroundColor = new Color(0, 0, 0, 0);
        }
    }
}
