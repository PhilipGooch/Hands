using NBG.Core;
using UnityEngine;

namespace Noodles.Animation
{
    public enum PlayBackMode
    {
        Edit,
        Play,
        Preview
    }

    public interface IOnPlayFrameChange
    {
        void OnChange();
    }
    /// <summary>
    /// The playback controller is the responsible for managing the animation reproduction and modes. It moves the cursor by changing the frame too.
    /// </summary>

    public class PlaybackController : IOnFixedUpdate
    {
        private readonly NoodleAnimationEditorData data;
        internal bool CanPlay => data.playbackMode != PlayBackMode.Preview && data.animation != null;

        bool IOnFixedUpdate.Enabled => true;

        public PlaybackController(NoodleAnimationEditorData data)
        {
            this.data = data;
#if UNITY_EDITOR
            NoodleAnimator.animationEditorPlaybackController = this;
#endif
        }

        public void OnFixedUpdate()
        {
#if UNITY_EDITOR
            if (NoodleAnimator.previewAnimator != null)
            {
                if (CanPlay)
                {
                    if (data.playbackMode == PlayBackMode.Play)
                        Play(Time.fixedDeltaTime);

                    data.nativeAnimation = NoodleAnimator.previewAnimator.BakeAnimation(data.animation, loop: true);

                    NoodleAnimator.previewAnimator.usePoseOverride = true;

                    switch (data.playbackMode)
                    {
                        case PlayBackMode.Edit:
                            NoodleAnimator.previewAnimator.poseOverride = data.nativeAnimation.GetPose(data.currentFrame * NativeAnimation.perFrameTime);
                            break;
                        case PlayBackMode.Play:
                            NoodleAnimator.previewAnimator.poseOverride = data.nativeAnimation.GetPose(data.playTime);
                            break;
                    }
                }
                else
                {
                    if (NoodleAnimator.previewAnimator.usePoseOverride)
                        NoodleAnimator.previewAnimator.usePoseOverride = false;
                }
            }
#endif
        }

        private void Play(float dt)
        {
            dt *= data.animation.playbackSpeed;
            data.playTime += dt;
            int newFrame = (int)(data.playTime / NativeAnimation.perFrameTime);
            if (newFrame != data.playCurrentFrame)
            {
                data.playCurrentFrame = newFrame;

                if (data.playCurrentFrame >= data.animation.frameLength)
                {
                    data.playCurrentFrame = 0;
                    data.playTime = 0;
                }

                data.onPlayFrameChange?.OnChange();
            }
        }
        /// <summary>
        /// Moves to the next frame
        /// </summary>
        internal void NextFrame()
        {
            data.currentFrame++;
            if (data.currentFrame >= data.animation.frameLength)
                SetFrame(0);
        }
        /// <summary>
        /// Moves to the previous frame
        /// </summary>
        internal void PreviousFrame()
        {
            data.currentFrame--;
            if (data.currentFrame < 0)
                SetFrame(data.animation.frameLength - 1);
        }
        /// <summary>
        /// Sets the frame
        /// </summary>
        /// <param name="frame">Target frame</param>
        public void SetFrame(int frame)
        {
            data.currentFrame = frame;
            data.currentTime = frame * NativeAnimation.perFrameTime;
        }
        /// <summary>
        /// Changes the frame length
        /// </summary>
        /// <param name="length">Frame length</param>
        public void SetFrameLength(int length)
        {
            if (length >= 1)
                data.animation.frameLength = length;
        }
        /// <summary>
        /// Change the playback mode
        /// </summary>
        /// <param name="mode"></param>
        public void SetMode(PlayBackMode mode)
        {
            data.playbackMode = mode;
            if(Application.isPlaying)
                data.RebakeNativeAnimation();
        }
        /// <summary>
        /// Change playback speed
        /// </summary>
        /// <param name="playbackSpeed"></param>
        public void SetPlaybackSpeed(float playbackSpeed)
        {
            data.animation.playbackSpeed = playbackSpeed;
        }
        /// <summary>
        /// Gets the current playback speed
        /// </summary>
        public float GetPlaybackSpeed()
        {
            return data.animation.playbackSpeed;
        }
    }
}
