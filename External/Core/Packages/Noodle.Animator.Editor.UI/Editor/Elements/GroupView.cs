using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Groups GroupFieldView(s) to allow foldout
    /// </summary>
    public class GroupView : VisualElement
    {
        private const string k_UXMLGUID = "f3b65d7392063b243b3028fe1c786567";
        public new class UxmlFactory : UxmlFactory<GroupView, UxmlTraits> { }

        private readonly Button foldoutButton;
        private readonly VisualElement titleGroup;
        private readonly Label label;

        private readonly List<GroupFieldView> fields = new List<GroupFieldView>();
        private TracksGroup tracksGroup;

        private AnimatorWindowAnimationData data;

        public GroupView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            titleGroup = this.Q<VisualElement>("titleGroup");
            label = this.Q<Label>("groupName");
            foldoutButton = this.Q<Button>("foldout");

            foldoutButton.clickable.clicked += () =>
            {
                tracksGroup.Foldout = !tracksGroup.Foldout;
                data.selectionController.ClearSelection();
                data.StateChanged();
            };

            titleGroup.style.height = NoodleAnimatorParameters.rowHeight;
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;

            foreach (var field in fields)
            {
                field.SetNewDataFile(data);
            }
        }

        internal void Update(string label)
        {
            tracksGroup = data.groupedTracks[label];

            if (tracksGroup.Foldout)
                foldoutButton.text = "▼";
            else
                foldoutButton.text = "▶";

            SetLabel(label);
            CreateFieldViews();

            HandleVisibility(tracksGroup.FullyHidden);
        }

        private void SetLabel(string label)
        {
            this.label.text = label;
        }

        private void CreateFieldViews()
        {
            int cursorPosition = data.noodleAnimatorData.currentFrame;
            bool looped = data.noodleAnimatorData.animation.looped;
            var id = 0;
            foreach (var track in tracksGroup.tracks)
            {
                var field = GetFieldView(id);
                var keyframe = track.animationTrack.frames.Find(x => x.time == cursorPosition);
                var value = data.noodleAnimatorData.animation.Sample(track.trackId, cursorPosition);
                var easing = track.animationTrack.GetEasingAt(cursorPosition, looped);

                field.SetData(
                    track,
                    easing,
                    value
                    );

                id++;
            }
        }

        private GroupFieldView GetFieldView(int id)
        {
            if (fields.Count > id)
                return fields[id];

            GroupFieldView floatField = new GroupFieldView();
            floatField.SetNewDataFile(data);

            fields.Add(floatField);
            Add(floatField);

            return floatField;
        }

        private void HandleVisibility(bool hide)
        {
            if (hide)
            {
                if (visible)
                {
                    visible = false;
                    style.display = DisplayStyle.None;
                }
            }
            else
            {
                if (!visible)
                {
                    visible = true;
                    style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}
