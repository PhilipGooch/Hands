using NBG.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Visual representation of animation key
    /// </summary>
    public class KeyView : VisualElement
    {
        private const string k_UXMLGUID = "49436333a9b545e42ba874e2bc68ca7e";
        public new class UxmlFactory : UxmlFactory<KeyView, UxmlTraits> { }

        private readonly TransitionLineView transitionLine;

        private AnimatorWindowAnimationData data;

        internal Vector2Int TimeAndTrackCoords => TimelineUtils.CoordsToTimeAndTrack(coords);
        internal Vector2Int coords;

        bool darkMode;

        public KeyView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            usageHints = UsageHints.DynamicTransform;
            pickingMode = PickingMode.Ignore;

            style.width = NoodleAnimatorParameters.keyWidth;
            style.height = NoodleAnimatorParameters.keyHeight;

            if (transitionLine == null)
            {
                transitionLine = new TransitionLineView();
                Add(transitionLine);
            }

            darkMode = EditorGUIUtility.isProSkin;
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;
        }

        internal void SetData(Vector2Int coords, int prevKeyTime, bool isLast, EasingType easeType)
        {
            this.coords = coords;

            if (easeType != EasingType.Default && coords.x > 0)
                transitionLine.SetData(prevKeyTime, coords.x, isLast && data.noodleAnimatorData.animation.looped, AnimatorWindowAnimationData.TotalColumnCount);
            else if (transitionLine.visible)
                transitionLine.SetVisibility(false);

            if (data.IsSelected(TimeAndTrackCoords))
                SelectVisuals();
            else
                DeselectVisuals();
        }

        internal void SetPosition(int x, int y)
        {
            style.left = x;
            style.top = y;
        }

        internal void SetVisibilityState(bool state)
        {
            this.SetVisibility(state);
            if (!state)
                SetTransitionLineVisibility(false);

            RemoveFromHierarchy();
        }

        internal void SetTransitionLineVisibility(bool state)
        {
            if (state != transitionLine.visible)
                transitionLine.SetVisibility(state);
        }

        public void SelectVisuals()
        {
            style.unityBackgroundImageTintColor = NoodleAnimatorParameters.GetSelectedColor(darkMode);
            transitionLine.SelectVisuals();
        }

        public void DeselectVisuals()
        {
            style.unityBackgroundImageTintColor = NoodleAnimatorParameters.GetNotSelectedColor(darkMode);
            transitionLine.DeselectVisuals();
        }

        internal void ShiftVisual(int offset)
        {
            int clampedX = (int)transform.position.x + NoodleAnimatorParameters.columnWidth * offset;
            transform.position = new Vector2(clampedX, 0);
        }
    }
}
