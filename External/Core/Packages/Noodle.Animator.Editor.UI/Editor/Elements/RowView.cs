using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Visual representation of an animation track
    /// </summary>
    public class RowView : VisualElement
    {
        private const string k_UXMLGUID = "a49ac00a34fda4945b38ee9b55292318";
        public new class UxmlFactory : UxmlFactory<RowView, UxmlTraits> { }

        private AnimatorWindowAnimationData data;
        bool darkMode;

        public RowView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            style.height = NoodleAnimatorParameters.rowHeight;
            pickingMode = PickingMode.Ignore;

            darkMode = EditorGUIUtility.isProSkin;
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;
        }

        internal void SetData(int rowId, int columnCount)
        {
            style.top = rowId * NoodleAnimatorParameters.rowHeight;
            style.width = columnCount * NoodleAnimatorParameters.columnWidth;

            if (data.rowToTrackMap.ContainsKey(rowId))
            {
                if (data.selectionController.LastSelectedTrack == data.rowToTrackMap[rowId] || data.selectedTracks.Contains(data.rowToTrackMap[rowId]))
                    style.backgroundColor = NoodleAnimatorParameters.GetSelectedRowColor(darkMode);
                else
                    style.backgroundColor = new Color(0, 0, 0, 0);

                style.borderTopWidth = 1;
                style.borderBottomWidth = 1;
            }
            else
            {
                style.backgroundColor = NoodleAnimatorParameters.GetEmptyRowColor(darkMode);

                style.borderTopWidth = 0;
                style.borderBottomWidth = 0;
            }
        }
    }
}
