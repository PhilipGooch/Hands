using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    public static class EditorStateManager
    {
        private const string kViewportSavedStateKey = "kViewportSavedStateKey";
        private const string kWindowFocusStateKey = "kWindowFocusStateKey";
        private const string kSearcherSizeKey = "kBlackboardCollapseStateKey";
        private const string kSearcherFoldoutStateKey = "kSearcherFoldoutStateKey";
        private const string kSearcherSelectionKey = "kSearcherSelectionKey";
        private const string kSearcherTypeVisibility = "kSearcherTypeVisibility";
        private const string kSettingsViewVisibility = "kSettingsViewVisibility";

        private readonly static Vector2 searcherDefaultSize = new Vector2(500, 450);

        public static Vector2 SearcherSize
        {
            get
            {
                return ParseVector2(SessionState.GetString(kSearcherSizeKey, Vector2ToString(searcherDefaultSize)));
            }
            set
            {
                SessionState.SetString(kSearcherSizeKey, Vector2ToString(value));
            }
        }

        public static bool WindowFocusState
        {
            get
            {
                return SessionState.GetBool(kWindowFocusStateKey, false);
            }
            set
            {
                SessionState.SetBool(kWindowFocusStateKey, value);
            }
        }

        public static bool SettingsViewVisibility
        {
            get
            {
                return SessionState.GetBool(kSettingsViewVisibility, false);
            }
            set
            {
                SessionState.SetBool(kSettingsViewVisibility, value);
            }

        }

        #region Viewport state

        public static void SetGraphViewViewportValues(GameObject graph, Vector3 position, Vector3 scale)
        {
            var converted = ParseViewportStateList(SessionState.GetString(kViewportSavedStateKey, ""));

            var exisiting = converted.Find(x => x.id == graph.GetInstanceID());
            if (exisiting != null)
            {
                exisiting.pos = position;
                exisiting.scale = scale;
            }
            else
            {
                converted.Add(new ViewportState()
                {
                    id = graph.GetInstanceID(),
                    pos = position,
                    scale = scale
                });
            }

            SaveViewportState(converted);
        }

        public static (bool exists, Vector3 position, Vector3 scale) GetGraphViewViewportValues(GameObject graph)
        {
            var converted = ParseViewportStateList(SessionState.GetString(kViewportSavedStateKey, ""));

            var exisiting = converted.Find(x => x.id == graph.GetInstanceID());
            if (exisiting != null)
            {
                return (true, exisiting.pos, exisiting.scale);
            }
            else
            {
                return (false, Vector2.zero, Vector2.zero);
            }
        }

        private static void SaveViewportState(List<ViewportState> converted)
        {
            string data = "";
            foreach (var item in converted)
            {
                data += IntToString(item.id);
                data += Vector3ToString(item.pos);
                data += Vector3ToString(item.scale);
            }
            SessionState.SetString(kViewportSavedStateKey, data);
        }

        private static List<ViewportState> ParseViewportStateList(string data)
        {
            List<ViewportState> converted = new List<ViewportState>();

            char[] separator = { ';' };
            string[] splitLines = data.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < splitLines.Length; i += 7)
            {
                ViewportState state = new ViewportState();

                state.id = ParseInt(splitLines[i]);

                state.pos = new Vector3(
                    ParseFloat(splitLines[i + 1]),
                    ParseFloat(splitLines[i + 2]),
                    ParseFloat(splitLines[i + 3]));

                state.scale = new Vector3(
                    ParseFloat(splitLines[i + 4]),
                    ParseFloat(splitLines[i + 5]),
                    ParseFloat(splitLines[i + 6]));

                converted.Add(state);
            }

            return converted;
        }

        #endregion

        public static void SetSearcherFoldoutState(string foldoutID, bool state)
        {
            SessionState.SetBool(kSearcherFoldoutStateKey + "/" + foldoutID, state);
        }

        public static bool GetSearcherFoldoutState(string foldoutID, bool defaultValue)
        {
            return SessionState.GetBool(kSearcherFoldoutStateKey + "/" + foldoutID, defaultValue);
        }

        public static void SetSearcherSelectionID(string selectedElementPath, string searcherID)
        {
            SessionState.SetString(kSearcherSelectionKey + "/" + searcherID, selectedElementPath);
        }

        public static string GetSearcherSelectionID(string searcherID)
        {
            return SessionState.GetString(kSearcherSelectionKey + "/" + searcherID, "");
        }

        public static void ClearSearcherSelectionID(string searcherID)
        {
            SessionState.EraseString(kSearcherSelectionKey + "/" + searcherID);
        }

        public static void SetSearcherTypeVisibility(string fullName, bool state)
        {
            SessionState.SetBool(fullName, state);
        }
        public static bool GetSearcherTypeVisibility(string fullName)
        {
            return SessionState.GetBool(fullName, true);
        }

        private static Vector2 ParseVector2(string text)
        {
            char[] separator = { ';' };
            string[] splitLines = text.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
            return new Vector2(
                  ParseFloat(splitLines[0]),
                  ParseFloat(splitLines[1]));
        }

        private static int ParseInt(string text)
        {
            int number;

            bool success = int.TryParse(text, out number);
            if (success)
            {
                return number;
            }
            else
            {
                Debug.Log($"int conversion failed");
                return -1;
            }
        }

        private static float ParseFloat(string text)
        {
            float number;

            bool success = float.TryParse(text, out number);
            if (success)
            {
                return number;
            }
            else
            {
                Debug.Log($"float conversion failed");
                return -1;
            }
        }

        private static string Vector3ToString(Vector3 data)
        {
            return $"{data.x};{data.y};{data.z};";
        }

        private static string Vector2ToString(Vector2 data)
        {
            return $"{data.x};{data.y};";
        }

        private static string IntToString(int data)
        {
            return $"{data};";
        }

        private class ViewportState
        {
            internal int id;
            internal Vector3 pos;
            internal Vector3 scale;
        }
    }
}
