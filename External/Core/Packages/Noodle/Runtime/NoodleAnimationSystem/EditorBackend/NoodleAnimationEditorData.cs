using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System;
using NBG.Undo;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Noodles.Animation
{
    /// <summary>
    /// Contains the data modified by the controllers and visualized by the UI.
    /// </summary>
    public class NoodleAnimationEditorData : IDisposable, IUndoStateCollector
    {
        public NoodleAnimator animator => UnityEngine.Object.FindObjectOfType<NoodleAnimator>();
        public PhysicalAnimation animation;

        #region Selection
        public List<SelectedKey> selection;
        internal int lastSelectedTrack = 0;
        public Vector2Int selectionPivot;
        #endregion

        #region Copy
        public List<CopyPasteData> copyPasteData;
        public bool CopyExists => copyPasteData.Count > 0;
        public bool IsCopyInSingleTrack
        {
            get
            {
                int count = copyPasteData.Count;
                int lastTrack = -1;
                for (int i = 0; i < count; i++)
                {
                    if (lastTrack == -1)
                        lastTrack = copyPasteData[i].track;

                    if (lastTrack != copyPasteData[i].track)
                        return false;
                }

                return true;
            }
        }
        #endregion

        #region Playback
        /// <summary>
        /// Represents the frame where the cursor is right now.
        /// </summary>
        public int currentFrame;
        public float currentTime;

        public int playCurrentFrame;
        internal float playTime;

        internal NativeAnimation nativeAnimation;
        public PlayBackMode playbackMode = PlayBackMode.Edit;
        public IOnPlayFrameChange onPlayFrameChange;
        #endregion

        public NoodleAnimationEditorData( PhysicalAnimation anim)
        {
            if(anim.allTracks == null)
                anim.Initialize(NoodleAnimationLayout.ListAnimationGroups(),NoodleAnimationLayout.ListAnimationTracks());
            selection = new List<SelectedKey>(20);

            copyPasteData = new List<CopyPasteData>();
            animation = anim;
            if (anim.isInitialized)
            {
                anim.OnAfterDeserialize();
            }
        }

        internal void RebakeNativeAnimation()
        {
            UnityEngine.Object.FindObjectOfType<NoodleAnimator>()?.RebakeAnimation(nativeAnimation, animation);
            
        }

        public void Dispose()
        {
            nativeAnimation.Dispose();
        }

        public IUndoState RecordUndoState()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(animation);
#endif

            if (Application.isPlaying && playbackMode == PlayBackMode.Preview)
                UnityEngine.Object.FindObjectOfType<NoodleAnimator>()?.RebakeAnimations();

            return new NoodleAnimationEditorDataUndoState(this);
        }
    }
}
