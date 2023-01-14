using NBG.Undo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Used to store/restore previously focused input field
    /// </summary>
    internal class AnimationWindowFocus : IUndoState
    {
        internal Focusable focusedElement;

        private readonly AnimatorWindowAnimationData data;

        internal AnimationWindowFocus(AnimatorWindowAnimationData data)
        {
            this.data = data;
        }

        public AnimationWindowFocus Copy()
        {
            return new AnimationWindowFocus(data)
            {
                focusedElement = focusedElement
            };
        }

        public void Undo()
        {
            if (focusedElement != null && data.refreshSource != RefreshSource.KeyDrag)
            {
                focusedElement.Focus();
            }
        }
    }

    /// <summary>
    /// Contains UI specific data and acts as a middleman between controllers and UI (to extend controller actions with THIS ui specific actions)
    /// </summary>
    internal class AnimatorWindowAnimationData : IUndoStateCollector
    {
        internal NoodleAnimationEditorData noodleAnimatorData;
        internal SelectionController selectionController;
        internal OperationsController operationsController;
        internal UndoSystem undoController;
        internal PlaybackController playbackController;

        //including separator rows;
        internal int RowsCount => GetRowCount();
        //columns count until red zone
        internal int FramesCount => noodleAnimatorData.animation.frameLength;

        internal int RightMostKeyTime { get; private set; }
        internal int LeftMostKeyTime { get; private set; }

        internal int KeyCount => GetKeyCount();
        internal List<int> selectedTracks;

        internal Dictionary<int, int> rowToTrackMap;

        internal Dictionary<string, TracksGroup> groupedTracks;

        internal AnimationWindowFocus currentFocus;

        internal bool stateChanged = false;
        internal bool isGroupFieldInputFieldFocused = false;

        internal int leftMostSelectedKey;
        internal int rightMostSelectedKey;

        internal ClickType clickType;
        internal RefreshSource refreshSource;

        internal static int TotalNegativeColumnCount => NoodleAnimatorParameters.offBorderColumnCount + negativeColumnCount;
        private static int negativeColumnCount = 0;
        internal static int TotalPositiveColumnCount => NoodleAnimatorParameters.offBorderColumnCount + positiveColumnCount;
        private static int positiveColumnCount = 40;

        internal static int TotalColumnCount => TotalPositiveColumnCount + TotalNegativeColumnCount;

        internal AnimatorWindowAnimationData(NoodleAnimationEditorData data, AnimatorWindowAnimationData oldData) : this(data)
        {
            if (oldData != null)
            {
                operationsController.TransferCopyPasteData(oldData.noodleAnimatorData.copyPasteData);
            }
        }
        internal AnimatorWindowAnimationData(NoodleAnimationEditorData data)
        {
            noodleAnimatorData = data;
            currentFocus = new AnimationWindowFocus(this);
            undoController = new UndoSystem(500);

            selectionController = new SelectionController(data);
            playbackController = new PlaybackController(data);
            operationsController = new OperationsController(data, selectionController, undoController);
            groupedTracks = new Dictionary<string, TracksGroup>();
            selectedTracks = new List<int>();

            undoController.StartSystem(data, this);

            GroupData();
            Update();
        }

        public IUndoState RecordUndoState()
        {
            return currentFocus.Copy();
        }

        internal void StateChanged()
        {
            stateChanged = true;
        }

        internal void Update()
        {
            rowToTrackMap = MapRowsToTracks();
            selectedTracks = GetSelectedTracks();

            UpdateTimelineLength();

            positiveColumnCount = Mathf.Max(RightMostKeyTime, FramesCount);
            negativeColumnCount = Mathf.Abs(LeftMostKeyTime);

            (leftMostSelectedKey, rightMostSelectedKey) = GetEdgeKeysFromSelection();
        }

        //call after everything updates
        internal void ResetState()
        {
            clickType = ClickType.None;
            refreshSource = RefreshSource.Other;
        }

        private void GroupData()
        {
            string currentGroup = "";
            int groupId = 0;
            for (int i = 0; i < noodleAnimatorData.animation.allTracks.Count; i++)
            {
                var split = noodleAnimatorData.animation.allTracks[i].name.Split('.');
                if (split[0] != currentGroup)
                {
                    currentGroup = split[0];
                    groupedTracks.Add(currentGroup, new TracksGroup(groupId));
                    groupId++;
                }
                if (split.Length == 3)
                    groupedTracks[currentGroup].Add($"{split[1]}.{split[2]}", i, noodleAnimatorData.animation.allTracks[i]);
                else
                    groupedTracks[currentGroup].Add(split[1], i, noodleAnimatorData.animation.allTracks[i]);
            }
        }

        private int GetRowCount()
        {
            int c = NoodleAnimatorParameters.startRowsAt;
            foreach (var group in groupedTracks)
            {
                if (!group.Value.FullyHidden)
                {
                    foreach (var track in group.Value.tracks)
                    {
                        if (!track.hidden)
                            c++;
                    }
                    c++;
                }
            }

            return c;
        }

        private int GetKeyCount()
        {
            int c = 0;

            foreach (var group in groupedTracks)
            {
                if (group.Value.Foldout)
                {
                    foreach (var track in group.Value.tracks)
                    {
                        c += track.animationTrack.frames.Count;
                    }
                }
            }

            return c;
        }

        private void UpdateTimelineLength()
        {
            RightMostKeyTime = int.MinValue;
            LeftMostKeyTime = int.MaxValue;

            foreach (var group in groupedTracks)
            {
                foreach (var track in group.Value.tracks)
                {
                    foreach (var frame in track.animationTrack.frames)
                    {
                        if (frame.time > RightMostKeyTime)
                            RightMostKeyTime = frame.time;

                        if (frame.time < LeftMostKeyTime)
                            LeftMostKeyTime = frame.time;
                    }
                }
            }

            if (RightMostKeyTime == int.MinValue)
                RightMostKeyTime = 0;

            if (LeftMostKeyTime == int.MaxValue)
                LeftMostKeyTime = 0;

            RightMostKeyTime++;
        }

        private List<int> GetSelectedTracks()
        {
            List<int> selectedTracks = new List<int>();

            foreach (var group in groupedTracks)
            {
                if (group.Value.Foldout)
                {
                    foreach (var track in group.Value.tracks)
                    {
                        foreach (var frame in track.animationTrack.frames)
                        {
                            if (selectionController.IsSelected(track.trackId, frame.time))
                            {
                                selectedTracks.Add(track.trackId);
                                break;
                            }
                        }
                    }
                }
            }
            return selectedTracks;
        }

        private Dictionary<int, int> MapRowsToTracks()
        {
            Dictionary<int, int> rowToTrackMap = new Dictionary<int, int>();

            int rowId = NoodleAnimatorParameters.startRowsAt;
            int trackId = 0;

            bool hideNoDataRows = SessionStateManager.GetHideEmptyTracks();

            foreach (var group in groupedTracks)
            {
                foreach (var track in group.Value.tracks)
                {
                    track.hidden = !group.Value.Foldout || (hideNoDataRows && track.animationTrack.frames.Count == 0);

                    if (!track.hidden)
                    {
                        ++rowId;
                        rowToTrackMap.Add(rowId, trackId);
                    }
                    trackId++;
                }

                group.Value.Update();

                if (!group.Value.FullyHidden)
                {
                    ++rowId;
                }
            }

            return rowToTrackMap;
        }

        internal (int left, int right) GetEdgeKeysFromSelection()
        {
            int max = -int.MaxValue;
            int min = int.MaxValue;

            foreach (var key in noodleAnimatorData.selection)
            {
                if (key.time > max)
                    max = key.time;

                if (key.time < min)
                    min = key.time;
            }

            return (min, max);
        }

        #region Operations
        internal bool IsSelected(Vector2Int coords)
        {
            return selectionController.IsSelected(coords.y, coords.x);
        }

        internal bool KeyExists(Vector2Int coords)
        {
            return operationsController.KeyExists(coords.y, coords.x);
        }

        internal EasingType GetEasing(Vector2Int coords)
        {
            var track = noodleAnimatorData.animation.allTracks[coords.y];
            return track.GetEasingAt(coords.x, noodleAnimatorData.animation.looped);
        }

        internal void CreateKey(Vector2Int coords)
        {
            SetCursor(coords.x);
            operationsController.CreateKey(coords.y, coords.x, false);

            StateChanged();
        }

        internal void PasteHere(Vector2Int coords)
        {
            if (coords.x >= 0 && coords.y >= 0)
            {
                operationsController.PasteInPosition(coords.x, coords.y);
                refreshSource = RefreshSource.KeyboardOperation;
                StateChanged();
            }
        }

        internal void MoveTrackAndCursor(Vector2Int coords)
        {
            SetCursor(coords.x);
            selectionController.SelectTrack(coords.y);
            StateChanged();
        }
        internal void InvertSelection()
        {
            operationsController.InvertSelection();
            StateChanged();
        }
        internal void DeleteSelection()
        {
            operationsController.DeleteSelection();
            StateChanged();
        }

        internal void SetEasing(EasingType easingType)
        {
            operationsController.SetKeyEasing(easingType);
            StateChanged();
        }

        internal void SetCursorPosition(int columnID)
        {
            SetCursor(columnID);
            StateChanged();
        }

        internal void Select(Vector2Int coords, bool moveCursor)
        {
            if (moveCursor)
                SetCursor(coords.x);

            if (KeyExists(coords))
                selectionController.SingleSelection(coords.y, coords.x);
            else if (coords.y > 0)
                selectionController.SelectTrack(coords.y);

            StateChanged();
        }

        internal void SelectAdditive(Vector2Int coords, bool moveCursor)
        {
            if (moveCursor)
                SetCursor(coords.x);

            if (KeyExists(coords))
                selectionController.AddSelection(coords.y, coords.x);
            else if (coords.y > 0)
                selectionController.SelectTrack(coords.y);

            StateChanged();
        }

        internal void Deselect(Vector2Int coords, bool moveCursor)
        {
            if (moveCursor)
                SetCursor(coords.x);

            selectionController.Deselect(coords.y, coords.x);
            StateChanged();
        }

        internal void SyncIKFK(Vector2Int coords, SyncIKFKMode mode)
        {
            operationsController.SyncIKFK(coords.y, coords.x, mode);
            StateChanged();
        }

        internal void ClearSelection()
        {
            selectionController.ClearSelection();
            StateChanged();
        }

        private void SetCursor(int frame, bool saveCursor = true)
        {
            playbackController.SetFrame(frame);
            if (saveCursor)
                SessionStateManager.SetCursorPosition(frame);
        }
        #endregion
    }

    /// <summary>
    /// Groups tracks, since groups are UI only concept
    /// </summary>
    internal class TracksGroup
    {
        internal bool Foldout
        {
            get
            {
                return foldout;
            }
            set
            {
                foldout = value;
                SessionStateManager.SetFoldoutGroup(id, foldout);
            }
        }
        private bool foldout;

        internal List<Track> tracks;

        internal bool FullyHidden { get; private set; }

        private int id;

        internal TracksGroup(int id)
        {
            this.id = id;
            foldout = SessionStateManager.GetFoldoutGroup(id);
            tracks = new List<Track>();
        }

        internal void Add(string name, int trackId, PhysicalAnimationTrack animationTrack)
        {
            tracks.Add(new Track(name, trackId, animationTrack));
        }

        internal void Update()
        {
            UpdateIsHidden();
            UpdateFouldout();
        }

        private void UpdateFouldout()
        {
            foldout = SessionStateManager.GetFoldoutGroup(id);
        }

        private void UpdateIsHidden()
        {
            bool hideNoDataRows = SessionStateManager.GetHideEmptyTracks();

            if (!hideNoDataRows)
            {
                FullyHidden = false;
                return;
            }

            foreach (var track in tracks)
            {
                if (!track.hidden)
                {
                    FullyHidden = false;
                    return;
                }
            }

            FullyHidden = true;
        }
    }

    /// <summary>
    /// Encapsulates PhysicalAnimationTrack from backend with additional UI data
    /// </summary>
    internal class Track
    {
        internal bool hidden;
        internal string name;
        internal int trackId;
        internal PhysicalAnimationTrack animationTrack;

        public Track(string name, int trackId, PhysicalAnimationTrack animationTrack)
        {
            this.name = name;
            this.trackId = trackId;
            this.animationTrack = animationTrack;
        }
    }

    internal enum ClickType
    {
        None,
        Single,
        Double
    }

    internal enum RefreshSource
    {
        /// <summary>
        /// cut, copy, paste, undo, redo
        /// </summary>
        KeyboardOperation,
        KeyDrag,
        InputFieldValueChanged,
        Other
    }
}
