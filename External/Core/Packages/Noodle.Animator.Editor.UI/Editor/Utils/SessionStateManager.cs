using UnityEditor;
using UnityEngine;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Saves temporary data to SessionState, mainly some settings and QOL features
    /// </summary>
    internal static class SessionStateManager
    {
        const string kEmptyTracksHiddenKey = "kEmptyTracksHiddenKey";
        const string kColapsedGroupsKey = "kColapsedGroupsKey";
        const string kCursorPositionKey = "kCursorPositionKey";

        internal static void SetHideEmptyTracks(bool hide)
        {
            SessionState.SetInt(kEmptyTracksHiddenKey, hide ? 1 : 0);
        }

        internal static bool GetHideEmptyTracks()
        {
            var hide = SessionState.GetInt(kEmptyTracksHiddenKey, 0);
            return hide == 1;
        }

        internal static void SetFoldoutGroup(int groupId, bool foldout)
        {
            SessionState.SetInt(groupId + kColapsedGroupsKey, foldout ? 1 : 0);
        }

        internal static bool GetFoldoutGroup(int groupId)
        {
            var foldout = SessionState.GetInt(groupId + kColapsedGroupsKey, 1);
            return foldout == 1;
        }

        internal static void SetCursorPosition(int pos)
        {
            SessionState.SetInt(kCursorPositionKey, pos);
        }

        internal static int GetCursorPosition()
        {
            return SessionState.GetInt(kCursorPositionKey, 0);
        }
    }
}
