using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NBG.Undo;
using System;

namespace Noodles.Animation
{
    public class OperationsController
    {
        private readonly SelectionController selectionController;
        private readonly UndoSystem undoController;
        private readonly NoodleAnimationEditorData data;

        private List<CopyPasteData> CopyPasteData => data.copyPasteData;

        public OperationsController(NoodleAnimationEditorData data, SelectionController selectionController, UndoSystem undoController)
        {
            this.data = data;
            this.selectionController = selectionController;
            this.undoController = undoController;
        }

        private List<SelectedKey> Selection => data.selection;

        /// <summary>
        /// Returns true if something is selected
        /// </summary>
        internal bool HasSelection => data.selection.Count > 0;
        /// <summary>
        /// Returns the last selected track.
        /// </summary>
        internal int LastSelectedTrack => data.lastSelectedTrack;

        public void CreateKey()
        {
            CreateKey(LastSelectedTrack);
        }

        /// <summary>
        /// Flips (mirrors) selected keys values and transitions on their tracks
        /// </summary>
        public void FlipSelectionHorizontaly()
        {
            foreach (var selection in data.selection)
            {
                if (data.animation.allTracks[selection.track].TryGetKeyIndex(selection.time, out int index))
                {
                    var frame = data.animation.allTracks[selection.track].frames[index];
                    frame.value *= -1;
                    data.animation.allTracks[selection.track].frames[index] = frame;
                }
            }

            if (data.selection.Count > 0)
                undoController.RecordUndo();
        }

        /// <summary>
        /// Create key in the track and time. If time is not given, it creates it in current frame.
        /// </summary>
        /// <param name="trackIndex">Track</param>
        /// <param name="time">Frame</param>
        /// <param name="recordUndo">Should this record undo?</param>
        public void CreateKey(int trackIndex, int time = -1, bool recordUndo = true)
        {
            if (trackIndex != -1)
            {
                int selectedFrameTime = time == -1 ? data.currentFrame : time;

                int nextIndex = data.animation.allTracks[trackIndex].NextKeyIndex(selectedFrameTime);
                float value = data.animation.allTracks[trackIndex].defaultValue;
                EasingType easing = EasingType.Default;

                if (data.animation.allTracks[trackIndex].TryGetKeyAtIndex(nextIndex, out var frame))
                {
                    value = data.animation.allTracks[trackIndex].Sample(selectedFrameTime, data.animation.looped, data.animation.frameLength);
                    easing = frame.easeType;
                }

                data.animation.allTracks[trackIndex].SetKey(
                    new PhysicalAnimationKeyframe
                    {
                        time = selectedFrameTime,
                        easeType = easing,
                        value = value
                    }
                );

                selectionController.ClearSelection();
                selectionController.AddSelection(trackIndex, selectedFrameTime);

                if (recordUndo)
                    undoController.RecordUndo();
            }
        }

        /// <summary>
        /// Sets the value in the current frame.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="recordUndo"></param>
        public void SetKeyOnCursor(float value, bool recordUndo = true)
        {
            if (data.animation.allTracks[LastSelectedTrack].TryGetKeyIndex(data.currentFrame, out int index))
            {
                var frame = data.animation.allTracks[LastSelectedTrack].frames[index];
                if (frame.value != value)
                {
                    frame.value = value;
                    data.animation.allTracks[LastSelectedTrack].frames[index] = frame;
                    if (recordUndo)
                        undoController.RecordUndo();
                }
            }
            else
            {
                EasingType easing = EasingType.Default;

                int next = data.animation.allTracks[LastSelectedTrack].NextKeyIndex(data.currentFrame);
                if (data.animation.allTracks[LastSelectedTrack].TryGetKeyAtIndex(next, out var frame))
                {
                    easing = frame.easeType;
                }

                data.animation.allTracks[LastSelectedTrack].SetKey(
                new PhysicalAnimationKeyframe
                {
                    time = data.currentFrame,
                    easeType = easing,
                    value = value
                }
                );
                if (recordUndo)
                    undoController.RecordUndo();
            }
        }

        /// <summary>
        /// Sets the key in the target track and time.
        /// </summary>
        /// <param name="track">Track</param>
        /// <param name="time">Frame</param>
        /// <param name="value">Value</param>
        /// <param name="recordUndo">Should this record undo?</param>
        public void SetKey(int track, int time, float value, bool recordUndo = true)
        {
            if (data.animation.allTracks[track].TryGetKeyIndex(time, out int index))
            {
                var frame = data.animation.allTracks[track].frames[index];
                if (frame.value == value)
                    return;
                frame.value = value;
                data.animation.allTracks[track].frames[index] = frame;
            }
            else
            {
                data.animation.allTracks[track].SetKey(
                new PhysicalAnimationKeyframe
                {
                    time = time,
                    easeType = EasingType.Default,
                    value = value
                }
                );
            }

            if (recordUndo)
                undoController.RecordUndo();
        }

        /// <summary>
        /// Sets the value to selection.
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="recordUndo">Should this record undo?</param>
        public void SetKeyToSelectedFramesInCurrentTrack(float value, bool recordUndo = true)
        {
            if (HasSelection)
            {
                foreach (var selection in data.selection)
                {
                    if (LastSelectedTrack != -1 && LastSelectedTrack != selection.track)
                        continue;

                    if (data.animation.allTracks[selection.track].TryGetKeyIndex(selection.time, out int index))
                    {
                        var frame = data.animation.allTracks[selection.track].frames[index];
                        frame.value = value;
                        data.animation.allTracks[selection.track].frames[index] = frame;
                    }
                    else
                    {
                        data.animation.allTracks[selection.track].SetKey(
                        new PhysicalAnimationKeyframe
                        {
                            time = selection.time,
                            easeType = EasingType.Default,
                            value = value
                        }
                        );
                    }
                }
                if (recordUndo)
                    undoController.RecordUndo();
            }
        }

        /// <summary>
        /// Sets the key easing to selection.
        /// </summary>
        /// <param name="easingType">Easing type</param>
        /// <param name="recordUndo">Should this record undo?</param>
        public void SetKeyEasing(EasingType easingType, bool recordUndo = true)
        {
            if (HasSelection)
            {
                foreach (var selection in data.selection)
                {
                    if (data.animation.allTracks[selection.track].TryGetKeyIndex(selection.time, out int index))
                    {
                        var frame = data.animation.allTracks[selection.track].frames[index];
                        frame.easeType = easingType;
                        data.animation.allTracks[selection.track].frames[index] = frame;
                    }
                }
                if (recordUndo)
                    undoController.RecordUndo();
            }
        }
        /// <summary>
        /// Changes easing in the current frame.
        /// </summary>
        /// <param name="easingType">Easing type</param>
        /// <param name="recordUndo">Should this record undo?</param>
        public void SetKeyEasingOnCursor(EasingType easingType, bool recordUndo = true)
        {
            if (data.animation.allTracks[LastSelectedTrack].TryGetKeyIndex(data.currentFrame, out int index))
            {
                var key = data.animation.allTracks[LastSelectedTrack].frames[index];
                key.easeType = easingType;
                data.animation.allTracks[LastSelectedTrack].frames[index] = key;
                if (recordUndo)
                    undoController.RecordUndo();
            }
            else
            {
                int next = data.animation.allTracks[LastSelectedTrack].NextKeyIndex(data.currentFrame, data.animation.looped);
                if (data.animation.allTracks[LastSelectedTrack].TryGetKeyAtIndex(next, out var frame))
                {
                    var key = data.animation.allTracks[LastSelectedTrack].frames[next];
                    if (key.easeType != easingType)
                    {
                        key.easeType = easingType;
                        data.animation.allTracks[LastSelectedTrack].frames[next] = key;
                        if (recordUndo)
                            undoController.RecordUndo();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a key in all tracks.
        /// </summary>
        public void CreateKeyForAllTracks()
        {
            int length = data.animation.allTracks.Count;
            for (int i = 0; i < length; i++)
            {
                CreateKey(i, recordUndo: false);
            }

            undoController.RecordUndo();
        }

        /// <summary>
        /// Copies the selection.
        /// </summary>
        public void Copy()
        {
            if (CanCopy())
            {
                int selectionCount = Selection.Count;
                int minTime = Selection.Min(p => p.time);

                ClearCopyPasteData();

                for (int i = 0; i < selectionCount; i++)
                {
                    SelectedKey element = Selection[i];
                    var track = data.animation.allTracks[element.track];
                    if (track.TryGetKeyIndex(element.time, out int index))
                    {
                        if (track.TryGetKeyAtIndex(index, out var frame))
                        {
                            AddToCopyData(
                                new CopyPasteData
                                {
                                    frame = frame,
                                    track = element.track,
                                    transitionOnly = false
                                });
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Standard cut operation like CTRL + X.
        /// </summary>
        public void Cut()
        {
            if (CanCopy())
            {
                Copy();
                DeleteSelection();
                undoController.RecordUndo();
            }
        }

        private bool CanCopy()
        {
            return HasSelection;
        }

        private int GetCopyMinTime()
        {
            return data.copyPasteData.Min(p => p.frame.time);
        }

        private int GetCopyMaxTime()
        {
            return data.copyPasteData.Max(p => p.frame.time);
        }

        private int GetCopyMinTrack()
        {
            return data.copyPasteData.Min(p => p.track);
        }

        private int GetCopyMaxTrack()
        {
            return data.copyPasteData.Max(p => p.track);
        }

        private int GetMinSelectionTime()
        {
            return data.selection.Min(p => p.time);
        }

        /// <summary>
        /// Pastes the current copy in the current frame or position.
        /// </summary>
        /// <param name="position">The frame where you want to paste the copy.</param>
        public void Paste(int position = int.MinValue, bool deleteKeysInArea = true, bool allowMoveToAnotherTrack = true)
        {
            if (data.CopyExists)
            {
                int minTime = GetCopyMinTime();
                int pasteTime = data.currentFrame == -1 ? 0 : data.currentFrame;
                pasteTime = position != int.MinValue ? position : pasteTime;

                int offset = pasteTime - minTime;

                bool isSingleTrack = data.IsCopyInSingleTrack;

                int minTrack = GetCopyMinTrack();
                int maxTrack = GetCopyMaxTrack();

                if (deleteKeysInArea)
                    DeleteKeysInArea(minTime + offset, GetCopyMaxTime() + offset, minTrack, maxTrack);

                selectionController.ClearSelection();
                var diff = allowMoveToAnotherTrack ? LastSelectedTrack - minTrack : 0;
                var copyHeight = maxTrack - minTrack;

                //move paste start up if pasted tracks move outside of existing tracks
                if (allowMoveToAnotherTrack && LastSelectedTrack + copyHeight > data.animation.allTracks.Count - 1)
                {
                    diff += (data.animation.allTracks.Count - 1) - (LastSelectedTrack + copyHeight);
                }

                foreach (CopyPasteData copy in data.copyPasteData)
                {
                    int pasteTrack = isSingleTrack ? LastSelectedTrack : copy.track + diff;
                    var frame = copy.frame;
                    frame.time += offset;
                    data.animation.allTracks[pasteTrack].SetKey(
                        frame
                    );
                    selectionController.AddSelection(pasteTrack, frame.time);
                }
                undoController.RecordUndo();
            }
        }

        /// <summary>
        /// Pastes the current copy in the time and track
        /// </summary>
        public void PasteInPosition(int pasteTime, int pasteTrack, bool deleteKeysInArea = true, bool recordUndo = true)
        {
            if (data.CopyExists)
            {
                int minTime = GetCopyMinTime();
                int minTrack = GetCopyMinTrack();

                int timeOffset = pasteTime - minTime;
                int trackOffset = pasteTrack - minTrack;

                if (deleteKeysInArea)
                    DeleteKeysInArea(minTime + timeOffset, GetCopyMaxTime() + timeOffset, GetCopyMinTrack() + trackOffset, GetCopyMaxTrack() + trackOffset);

                selectionController.ClearSelection();

                foreach (CopyPasteData copy in data.copyPasteData)
                {
                    int track = copy.track + trackOffset;
                    var frame = copy.frame;

                    if (track < data.animation.allTracks.Count)
                    {
                        frame.time += timeOffset;
                        data.animation.allTracks[track].SetKey(
                            frame
                        );
                        selectionController.AddSelection(track, frame.time);
                    }
                }

                if (recordUndo)
                    undoController.RecordUndo();
            }
        }

        void DeleteKeysInArea(int startTime, int endTime, int startTrack, int endTrack)
        {
            for (int time = startTime; time <= endTime; time++)
            {
                for (int trackId = startTrack; trackId <= endTrack; trackId++)
                {
                    var track = data.animation.allTracks[trackId];
                    if (track.TryGetKeyIndex(time, out int index))
                    {
                        track.DeleteKey(index);
                    }
                }
            }
        }

        /// <summary>
        /// Inverts the selection.
        /// </summary>
        /// <param name="recordUndo">Should this record undo?</param>
        public void InvertSelection(bool recordUndo = true)
        {
            int trackIndex;
            int time;

            if (recordUndo)
                undoController.OverwriteUndo();
            int selectionCount = Selection.Count;
            for (int i = 0; i < selectionCount; i++)
            {
                SelectedKey select = Selection[i];
                time = select.time;
                trackIndex = select.track;

                var track = data.animation.allTracks[trackIndex];
                if (track.TryGetKeyIndex(time, out int index))
                    track.SetValue(time, -track.GetKeyAtIndex(index).value);
            }

            selectionController.ClearSelection();

            if (recordUndo)
                undoController.RecordUndo();
        }

        /// <summary>
        /// Deletes the selection.
        /// </summary>
        /// <param name="recordUndo">Should this record undo?</param>
        public void DeleteSelection(bool recordUndo = true)
        {
            int trackIndex;
            int time;

            if (recordUndo)
                undoController.OverwriteUndo();
            int selectionCount = Selection.Count;
            for (int i = 0; i < selectionCount; i++)
            {
                SelectedKey select = Selection[i];
                time = select.time;
                trackIndex = select.track;

                var track = data.animation.allTracks[trackIndex];
                if (track.TryGetKeyIndex(time, out int index))
                {
                    track.DeleteKey(index);
                }
            }

            selectionController.ClearSelection();

            if (recordUndo)
                undoController.RecordUndo();
        }
        /// <summary>
        /// Clears the current copy data.
        /// </summary>
        internal void ClearCopyPasteData()
        {
            CopyPasteData.Clear();
        }

        /// <summary>
        /// Adds more things to existing copy data.
        /// </summary>
        /// <param name="data"></param>
        internal void AddToCopyData(CopyPasteData data)
        {
            CopyPasteData.Add(data);
        }
        /// <summary>
        /// Returns true if key exist in track and time.
        /// </summary>
        /// <param name="track">Track</param>
        /// <param name="time">Time</param>
        /// <returns></returns>
        public bool KeyExists(int track, int time)
        {
            if (track >= 0 && track < data.animation.allTracks.Count)
                return data.animation.allTracks[track].KeyExists(time);
            return false;
        }


        /// <summary>
        /// Moves selection in time if possible.
        /// </summary>
        /// <param name="offset">How many units in time to offset this.</param>
        public void MoveSelection(int offset)
        {
            if (offset != 0)
            {
                int min = GetMinSelectionTime();
                int dest = min + offset;

                undoController.OverwriteUndo();
                Copy();
                DeleteSelection(false);
                Paste(dest, false, false);
            }
        }


        /// <summary>
        /// Make a local copy of copy-paste data allowing copy paste between clips
        /// </summary>
        /// <param name="copyPasteData">the frames in copy paste buffer</param>
        public void TransferCopyPasteData(List<CopyPasteData> copyPasteData)
        {
            data.copyPasteData = new List<CopyPasteData>(copyPasteData);
        }

        /// <summary>
        /// Calculates limb FK or IK data to keep effector position synced
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <param name="mode"></param>
        public void SyncIKFK(int track, int time, SyncIKFKMode mode)
        {
            Debug.Log(track);
            if (NoodleAnimationLayout.SyncFKtoIK(data.animation, data.animator.dimensions, track, time, mode, out var trackStart, out var trackCount))
            {
                selectionController.ClearSelection();
                for (int i = trackStart; i < trackStart + trackCount; i++)
                    selectionController.AddSelection(track, time);
                undoController.RecordUndo();
            }
        }

    }
}