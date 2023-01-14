using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Undo;
using UnityEngine;

namespace Noodles.Animation
{
    internal class NoodleAnimationEditorDataUndoState : IUndoState
    {
        NoodleAnimationEditorData data;

        internal List<PhysicalAnimationTrack> tracks;
        internal List<SelectedKey> selection;
        internal int frameLength;
        internal float playbackSpeed;

        internal NoodleAnimationEditorDataUndoState(NoodleAnimationEditorData data)
        {
            this.data = data;
            CopyTracks(data.animation);
            CopySelection(data.selection);
            CopyMisc();
        }
        public void Undo()
        {
            ApplyToAnimation(data.animation);
            ApplySelection(data.selection);
            ApplyMisc();
        }
        private void CopyTracks(PhysicalAnimation animation)
        {
            int count = animation.allTracks.Count;
            tracks = new List<PhysicalAnimationTrack>(count);

            for (int i = 0; i < count; i++)
            {
                var track = new PhysicalAnimationTrack();
                track.frames = new List<PhysicalAnimationKeyframe>();

                var originalFrames = animation.allTracks[i].frames;
                int frameCount = originalFrames.Count;

                for (int j = 0; j < frameCount; j++)
                {
                    track.frames.Add(originalFrames[j]);
                }

                tracks.Add(track);
            }
        }

        private void ApplyToAnimation(PhysicalAnimation animation)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                var copyTrackFrames = tracks[i].frames;
                var animTrack = animation.allTracks[i].frames;

                animTrack.Clear();

                for (int j = 0; j < copyTrackFrames.Count; j++)
                {
                    animTrack.Add(copyTrackFrames[j]);
                }
            }
        }
        private void CopySelection(List<SelectedKey> inSelection)
        {
            if (selection != null)
                selection.Clear();
            else
                selection = new List<SelectedKey>(inSelection.Count);

            for (int i = 0; i < inSelection.Count; i++)
            {
                selection.Add(new SelectedKey(inSelection[i].track, inSelection[i].time));
            }
        }
        private void ApplySelection(List<SelectedKey> outSelection)
        {
            outSelection.Clear();

            for (int i = 0; i < selection.Count; i++)
            {
                outSelection.Add(new SelectedKey(selection[i].track, selection[i].time));
            }
        }
        private void CopyMisc()
        {
            frameLength = data.animation.frameLength;
            playbackSpeed = data.animation.playbackSpeed;
        }
        private void ApplyMisc()
        {
            data.animation.frameLength = frameLength;
            data.animation.playbackSpeed = playbackSpeed;
        }
    }
}
