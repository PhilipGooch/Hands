using UnityEditor;
using UnityEngine;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// UI constants
    /// 
    /// NOTE for size variables use ONLY even numbers, since they can be split evenly
    /// </summary>
    internal static class NoodleAnimatorParameters
    {
        internal const int columnWidth = 30;
        internal const int rowHeight = 20;

        internal const int keyWidth = 18;
        internal const int keyHeight = 18;

        internal const int offBorderColumnCount = 20;

        //because first row is reserved for frame numbers
        internal const int startRowsAt = 1;

        //MUST match timeline-transition-line-view height from NoodeAnimatorWindow.uss, since currently there is no way to read values directly from uss file
        internal const int transitionLineHeight = 2;

        internal const int cursorWidth = 2;

        //for better transition from keyframe to line
        internal const int additionalTransitionLineWidth = 4;

        internal const double doubleClickThresh = 0.40f;
        internal const float distanceFromEdgeToScroll = 40;

        #region Colors

        internal static readonly Color selectedColor_Dark = new Color32(0, 170, 170, 255);
        internal static readonly Color notSelectedColor_Dark = new Color32(170, 170, 170, 255);
        internal static readonly Color selectedColor_Light = new Color32(0, 220, 220, 255);
        internal static readonly Color notSelectedColor_Light = new Color32(220, 220, 220, 255);

        internal static Color GetSelectedColor(bool darkMode)
        {
            return darkMode ? selectedColor_Dark : selectedColor_Light;
        }

        internal static Color GetNotSelectedColor(bool darkMode)
        {
            return darkMode ? notSelectedColor_Dark : notSelectedColor_Light;
        }

        internal static readonly Color emptyRowColor_Dark = new Color32(92, 92, 92, 255);
        internal static readonly Color emptyRowColor_Light = new Color32(157, 157, 157, 255);

        internal static Color GetEmptyRowColor(bool darkMode)
        {
            return darkMode ? emptyRowColor_Dark : emptyRowColor_Light;
        }

        internal static readonly Color selectedTrackColor_Dark = new Color32(255, 240, 110, 25);
        internal static readonly Color selectedTrackColor_Light = new Color32(255, 235, 110, 50);

        internal static Color GetSelectedRowColor(bool darkMode)
        {
            return darkMode ? selectedTrackColor_Dark : selectedTrackColor_Light;
        }

        internal static readonly Color selectedPivotColor = new Color32(252, 186, 3, 255);

        #endregion

    }
}
