using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles.Animation
{
    /// <summary>
    /// The selection controller is responsible for managing the selection operations.
    /// </summary>
    public class SelectionController
    {
        private readonly NoodleAnimationEditorData data;
        private PhysicalAnimation Animation => data.animation;
        private List<SelectedKey> Selection => data.selection;
        internal bool HasSelection => data.selection.Count > 0;
        public int LastSelectedTrack { get { return data.lastSelectedTrack; } set { data.lastSelectedTrack = value; } }
        internal int LastSelectedTime => data.selection[data.selection.Count - 1].time;

        public SelectionController(NoodleAnimationEditorData data)
        {
            this.data = data;
        }

        /// <summary>
        /// Adds the key to selection using track and time
        /// </summary>
        /// <param name="track"></param>
        /// <param name="time"></param>
        public void AddSelection(int track, int time)
        {
            AddSelection(new SelectedKey(track, time));
        }

        /// <summary>
        /// Selectes the key to selection using track and time removing the existing selection.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="time"></param>
        public void SingleSelection(int track, int time)
        {
            ClearSelection();
            AddSelection(track, time);
        }

        /// <summary>
        /// Select all keys in the animation
        /// </summary>
        public void SelectAll()
        {
            ClearSelection();
            for (int i = 0; i < Animation.allTracks.Count; i++)
            {
                foreach (var key in Animation.allTracks[i].frames)
                {
                    Selection.Add(new SelectedKey(i, key.time));
                }
            }
            CalculatePivot();
        }

        /// <summary>
        /// Adds the selected key to selection
        /// </summary>
        /// <param name="newSelection"></param>
        internal void AddSelection(SelectedKey newSelection)
        {
            int selectionCount = Selection.Count;
            for (int j = 0; j < selectionCount; j++)
            {
                if (Selection[j].time == newSelection.time && Selection[j].track == newSelection.track)
                    return;
            }

            Selection.Add(newSelection);
            LastSelectedTrack = newSelection.track;
            CalculatePivot();
        }

        /// <summary>
        /// Deselect specific keyframe at track and time.
        /// </summary>
        public void Deselect(int track, int time)
        {
            int selectionCount = Selection.Count;
            for (int j = 0; j < selectionCount; j++)
            {
                if (Selection[j].time == time && Selection[j].track == track)
                {
                    Selection.RemoveAt(j);
                    return;
                }
            }
            CalculatePivot();
        }

        /// <summary>
        /// Selects the track. This moves the track cursor by modifying LastSelectedTrack.
        /// </summary>
        /// <param name="trackIndex">Target track index.</param>
        public void SelectTrack(int trackIndex)
        {
            if (trackIndex < 0)
                trackIndex = 0;
            if (trackIndex >= Animation.allTracks.Count)
            {
                trackIndex = Animation.allTracks.Count - 1;
            }
            LastSelectedTrack = trackIndex;
        }

        /// <summary>
        /// Arrow selection, pressing up. It can be a single selection or an additive selection.
        /// </summary>
        /// <param name="singleSelection">It can be a single selection or an additive selection.</param>
        public void SelectUp(bool singleSelection = false)
        {
            if (LastSelectedTrack >= 1)
            {
                SelectTrack(LastSelectedTrack - 1);
                SelectNext(singleSelection, true);
            }
        }

        /// <summary>
        /// Arrow selection, pressing down. It can be a single selection or an additive selection.
        /// </summary>
        /// <param name="singleSelection">It can be a single selection or an additive selection.</param>
        public void SelectDown(bool singleSelection = false)
        {
            if (LastSelectedTrack < data.animation.allTracks.Count - 1)
            {
                SelectTrack(LastSelectedTrack + 1);
                SelectNext(singleSelection, true);
            }
        }

        /// <summary>
        /// Arrow selection, pressing right. It can be a single selection or an additive selection.
        /// </summary>
        /// <param name="singleSelection">It can be a single selection or an additive selection.</param>
        public int SelectNext(bool singleSelection = false, bool inclusive = false)
        {
            int initialFrame = data.currentFrame;
            int searchTime = inclusive ? data.currentFrame : data.currentFrame + 1;
            var track = Animation.allTracks[LastSelectedTrack];
            int next = track.NextKeyIndex(searchTime);

            if (track.TryGetKeyAtIndex(next, out var frame))
            {
                if (singleSelection)
                    ClearSelection();
                AddSelection(LastSelectedTrack, frame.time);
                data.currentFrame = frame.time;
            }

            return initialFrame - data.currentFrame;
        }

        /// <summary>
        /// Arrow selection, pressing left. It can be a single selection or an additive selection.
        /// </summary>
        /// <param name="singleSelection">It can be a single selection or an additive selection.</param>
        public int SelectPrevious(bool singleSelection = false, bool inclusive = false)
        {
            int initialFrame = data.currentFrame;

            int searchTime = inclusive ? data.currentFrame : data.currentFrame - 1;
            var track = Animation.allTracks[LastSelectedTrack];
            int next = track.PrevKeyIndex(searchTime);

            if (track.TryGetKeyAtIndex(next, out var frame))
            {
                int trackIndex = LastSelectedTrack;
                if (singleSelection)
                    ClearSelection();
                AddSelection(trackIndex, frame.time);
                data.currentFrame = frame.time;
            }

            return initialFrame - data.currentFrame;
        }

        /// <summary>
        /// Returns true if the key in track and time is selected.
        /// </summary>
        public bool IsSelected(int track, int time)
        {
            int length = Selection.Count;
            SelectedKey selected;
            for (int i = 0; i < length; i++)
            {
                selected = Selection[i];
                if (selected.time == time && selected.track == track)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clear the current selection.
        /// </summary>
        public void ClearSelection()
        {
            Selection.Clear();
        }

        /// <summary>
        /// Recalculates the pivot position based on current selection.
        /// </summary>
        public void CalculatePivot()
        {
            data.selectionPivot.x = GetCopyMinTime();
            data.selectionPivot.y = GetCopyMinTrack();
        }

        private int GetCopyMinTime()
        {
            int minTime = 999999999;
            foreach (var selection in data.selection)
            {
                if (selection.time < minTime)
                    minTime = selection.time;
            }
            return minTime;
        }

        private int GetCopyMinTrack()
        {
            int minTrack = 999999999;
            foreach (var selection in data.selection)
            {
                if (selection.track < minTrack)
                    minTrack = selection.track;
            }
            return minTrack;
        }

    }
}
