using NUnit.Framework;
using System.Linq;
using NBG.Undo;
using UnityEngine;
using Noodles.Animation;

namespace Noodles.Tests
{
    /// <summary>
    /// Tests to check if backend operations are working properly, simulating UI user operations.
    /// </summary>
    public class AnimationOperationTests
    {
        private const int trackCount = 51;
        private const int testTrack = 1;

        private class AnimationTestData
        {
            public PhysicalAnimation anim;

            public NoodleAnimationEditorData data;
            public SelectionController sc;
            public OperationsController oc;
            public UndoSystem uc;
            public PlaybackController pc;

            public AnimationTestData()
            {
                anim = ScriptableObject.CreateInstance<PhysicalAnimation>();
                anim.Initialize(NoodleAnimationLayout.ListAnimationGroups(),NoodleAnimationLayout.ListAnimationTracks());
                anim.frameLength = 100;

                data = new NoodleAnimationEditorData(anim);

                sc = new SelectionController(data);
                pc = new PlaybackController(data);

                uc = new UndoSystem(500);

                oc = new OperationsController(data, sc, uc);

                uc.StartSystem(data);
            }
        }


        [Test]
        public void CreateFrameTest()
        {
            const int targetFrame = 50;
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);
            td.pc.SetFrame(targetFrame);
            td.oc.CreateKey();

            bool found = td.anim.allTracks[testTrack].TryGetKeyIndex(targetFrame, out int index);

            Assert.AreEqual(1, td.anim.allTracks[testTrack].frames.Count);
            Assert.IsTrue(found);
            Assert.AreEqual(0, index);

            //Override the position
            td.oc.CreateKey();
            found = td.anim.allTracks[testTrack].TryGetKeyIndex(targetFrame, out index);

            Assert.AreEqual(1, td.anim.allTracks[testTrack].frames.Count);
            Assert.IsTrue(found);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void FrameDelete()
        {
            const int targetFrame = 50;
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);
            td.pc.SetFrame(targetFrame);
            td.oc.CreateKey();
            Assert.AreEqual(1, td.anim.allTracks[testTrack].frames.Count);

            //Override the position
            td.oc.DeleteSelection();
            Assert.AreEqual(0, td.anim.allTracks[testTrack].frames.Count);
        }

        [Test]
        public void DeleteNothing()
        {
            const int targetFrame = 50;
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);
            td.pc.SetFrame(targetFrame);
            td.oc.CreateKey();
            Assert.AreEqual(1, td.anim.allTracks[testTrack].frames.Count);

            td.sc.ClearSelection();
            td.sc.AddSelection(testTrack, targetFrame + 1);
            td.sc.AddSelection(testTrack, targetFrame - 1);

            //Delete nothing
            td.oc.DeleteSelection();
            Assert.AreEqual(1, td.anim.allTracks[testTrack].frames.Count);
        }

        [Test]
        public void CreateKeyframeOnEveryTrack()
        {
            const int targetFrame = 50;
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);
            td.pc.SetFrame(targetFrame);

            td.oc.CreateKeyForAllTracks();

            for (int i = 0; i < td.anim.allTracks.Count; i++)
                Assert.AreEqual(1, td.anim.allTracks[i].frames.Count);
        }

        [Test]
        public void Copy()
        {
            const float frameValue = 1.41f;
            const EasingType frameEasing = EasingType.sineIn;

            const int copyFrame1 = 50;
            const int copyFrame2 = 55;

            AnimationTestData td = new AnimationTestData();

            td.sc.AddSelection(1, copyFrame1);
            td.sc.AddSelection(1, copyFrame2);

            td.oc.SetKeyToSelectedFramesInCurrentTrack(frameValue);
            td.oc.SetKeyEasing(frameEasing);

            td.oc.Copy();

            Assert.AreEqual(2, td.data.copyPasteData.Count);

            Assert.AreEqual(frameValue, td.data.copyPasteData[0].frame.value);
            Assert.AreEqual(frameEasing, td.data.copyPasteData[0].frame.easeType);
            Assert.AreEqual(copyFrame1, td.data.copyPasteData[0].frame.time);

            Assert.AreEqual(frameValue, td.data.copyPasteData[1].frame.value);
            Assert.AreEqual(frameEasing, td.data.copyPasteData[1].frame.easeType);
            Assert.AreEqual(copyFrame2, td.data.copyPasteData[1].frame.time);
        }

        [Test]
        public void CopyPasteSingleFrame()
        {
            const float frameValue = 1.41f;
            const EasingType frameEasing = EasingType.sineIn;

            const int copyFrame1 = 50;

            AnimationTestData td = new AnimationTestData();

            td.sc.AddSelection(1, copyFrame1);

            td.oc.SetKeyToSelectedFramesInCurrentTrack(frameValue);
            td.oc.SetKeyEasing(frameEasing);

            td.oc.Copy();

            td.sc.ClearSelection();
            td.sc.SelectTrack(2);
            td.pc.SetFrame(10);
            td.oc.Paste();

            var frame = td.data.animation.allTracks[2].frames[0];
            Assert.AreEqual(frameValue, frame.value);
            Assert.AreEqual(frameEasing, frame.easeType);
            Assert.AreEqual(10, frame.time);
        }

        [Test]
        public void CopyPasteSingleTrack()
        {
            const float frameValue = 1.41f;
            //EasingType frameEasing = EasingType.sineIn;

            const int copyFrame1 = 50;
            const int copyFrame2 = 55;

            AnimationTestData td = new AnimationTestData();

            td.sc.AddSelection(1, copyFrame1);
            td.sc.AddSelection(1, copyFrame2);

            td.oc.SetKeyToSelectedFramesInCurrentTrack(frameValue);

            td.oc.Copy();

            td.sc.SelectTrack(2);

            td.oc.Paste();

            Assert.AreEqual(2, td.data.animation.allTracks[1].frames.Count);
            Assert.AreEqual(2, td.data.animation.allTracks[2].frames.Count);
            Assert.AreEqual(frameValue, td.data.animation.allTracks[1].frames[0].value);
            Assert.AreEqual(frameValue, td.data.animation.allTracks[1].frames[1].value);
            Assert.AreEqual(frameValue, td.data.animation.allTracks[2].frames[0].value);
            Assert.AreEqual(frameValue, td.data.animation.allTracks[2].frames[1].value);
        }

        [Test]
        public void CopyPasteMultiTrack()
        {
            const int copyFrame = 50;
            const int pasteFrame = 52;
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);
            td.pc.SetFrame(copyFrame);

            td.oc.CreateKeyForAllTracks();

            td.sc.ClearSelection();
            for (int i = 0; i < td.anim.allTracks.Count; i++)
                td.sc.AddSelection(i, copyFrame);

            td.oc.Copy();

            td.pc.SetFrame(pasteFrame);
            td.oc.Paste();

            for (int i = 0; i < td.anim.allTracks.Count; i++)
            {
                Assert.AreEqual(2, td.anim.allTracks[i].frames.Count);
                Assert.AreEqual(copyFrame, td.anim.allTracks[i].frames[0].time);
                Assert.AreEqual(pasteFrame, td.anim.allTracks[i].frames[1].time);
            }
        }

        [Test]
        public void UndoSingleTrack()
        {
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);

            const int maxKeyFrameNumber = 10;

            for (int i = 0; i < maxKeyFrameNumber; i++)
            {
                td.pc.SetFrame(i + 10);
                td.oc.CreateKey();
            }

            int frameCount = td.anim.allTracks[testTrack].frames.Count;
            const int undoTimes = 7;
            for (int i = 0; i < undoTimes; i++)
            {
                td.uc.Undo();
                frameCount--;
                Assert.AreEqual(frameCount, td.anim.allTracks[testTrack].frames.Count);
            }
        }

        [Test]
        public void UndoMultiTrack()
        {
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);

            const int maxKeyFrameNumber = 10;

            for (int i = 0; i < maxKeyFrameNumber; i++)
            {
                td.pc.SetFrame(i + 10);
                td.oc.CreateKeyForAllTracks();
            }

            int frameCount = td.anim.allTracks[testTrack].frames.Count;
            const int undoTimes = 7;
            for (int i = 0; i < undoTimes; i++)
            {
                td.uc.Undo();
                frameCount--;
                for (int j = 0; j < td.anim.allTracks.Count; j++)
                {
                    Assert.AreEqual(frameCount, td.anim.allTracks[j].frames.Count);
                }
            }
        }

        [Test]
        public void Deselect()
        {
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);

            const int maxKeyFrameNumber = 10;

            for (int i = 0; i < maxKeyFrameNumber; i++)
            {
                td.pc.SetFrame(i + 10);
                td.oc.CreateKey();
            }

            for (int i = 0; i < maxKeyFrameNumber; i++)
            {
                td.sc.AddSelection(testTrack, i + 10);
            }

            Assert.AreEqual(maxKeyFrameNumber, td.data.selection.Count);

            for (int i = 0; i < maxKeyFrameNumber; i++)
            {
                td.sc.Deselect(testTrack, i + 10);
            }

            Assert.AreEqual(0, td.data.selection.Count);
        }

        [Test]
        public void SelectNextPrev()
        {
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);

            td.pc.SetFrame(10);
            td.oc.CreateKey();
            td.pc.SetFrame(15);
            td.oc.CreateKey();
            td.pc.SetFrame(20);
            td.oc.CreateKey();

            td.sc.ClearSelection();

            td.pc.SetFrame(10);
            td.sc.AddSelection(testTrack, 10);

            td.sc.SelectNext();
            Assert.AreEqual(15, td.data.selection.Last().time);

            td.sc.SelectNext();
            Assert.AreEqual(20, td.data.selection.Last().time);

            td.sc.SelectNext();
            Assert.AreEqual(20, td.data.selection.Last().time);

            td.sc.ClearSelection();
            td.sc.AddSelection(testTrack, 20);

            td.sc.SelectPrevious();
            Assert.AreEqual(15, td.data.selection.Last().time);

            td.sc.SelectPrevious();
            Assert.AreEqual(10, td.data.selection.Last().time);

            td.sc.SelectPrevious();
            Assert.AreEqual(10, td.data.selection.Last().time);
        }

        [Test]
        public void FlipSelectionHorizontaly()
        {
            AnimationTestData td = new AnimationTestData();
            td.sc.SelectTrack(testTrack);

            td.pc.SetFrame(10);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(0);

            td.pc.SetFrame(15);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(-1);

            td.pc.SetFrame(16);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(2);

            td.pc.SetFrame(27);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(-3);

            td.sc.SelectTrack(3);

            td.pc.SetFrame(15);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(3);

            td.pc.SetFrame(16);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(-2);

            td.pc.SetFrame(20);
            td.oc.CreateKey();
            td.oc.SetKeyOnCursor(1);

            td.sc.SelectAll();
            td.oc.FlipSelectionHorizontaly();

            Assert.AreEqual(td.anim.allTracks[testTrack].frames[0].value, 0);
            Assert.AreEqual(td.anim.allTracks[testTrack].frames[1].value, 1);
            Assert.AreEqual(td.anim.allTracks[testTrack].frames[2].value, -2);
            Assert.AreEqual(td.anim.allTracks[testTrack].frames[3].value, 3);


            Assert.AreEqual(td.anim.allTracks[3].frames[0].value, -3);
            Assert.AreEqual(td.anim.allTracks[3].frames[1].value, 2);
            Assert.AreEqual(td.anim.allTracks[3].frames[2].value, -1);
        }

        [Test]
        public void SelectAllKeys()
        {
            AnimationTestData td = new AnimationTestData();

            td.sc.SelectTrack(testTrack);

            td.pc.SetFrame(10);
            td.oc.CreateKey();
            td.pc.SetFrame(15);
            td.oc.CreateKey();
            td.sc.SelectTrack(3);
            td.pc.SetFrame(20);
            td.oc.CreateKey();

            td.sc.ClearSelection();
            td.sc.SelectAll();

            Assert.AreEqual(3, td.data.selection.Count);
        }

        private static readonly int[] offsets = { 1, -1, -15, 16, 12, 46, 24, 34, -47 };
        [Test]
        public void MoveSelection([ValueSource(nameof(offsets))] int offset)
        {
            const float frameValue = 1.41f;
            const EasingType frameEasing = EasingType.sineIn;

            const int copyFrame1 = 50;
            int offsetedFrame = copyFrame1 + offset;

            AnimationTestData td = new AnimationTestData();

            td.sc.AddSelection(1, copyFrame1);

            td.oc.SetKeyToSelectedFramesInCurrentTrack(frameValue);
            td.oc.SetKeyEasing(frameEasing);

            td.oc.MoveSelection(offset);

            Assert.AreEqual(1, td.data.copyPasteData.Count);

            Assert.AreEqual(frameValue, td.data.copyPasteData[0].frame.value);
            Assert.AreEqual(frameEasing, td.data.copyPasteData[0].frame.easeType);
            Assert.AreEqual(copyFrame1, td.data.copyPasteData[0].frame.time);

            var existingFrame = td.data.animation.allTracks[1].frames[0];
            Assert.AreEqual(frameValue, existingFrame.value);
            Assert.AreEqual(frameEasing, existingFrame.easeType);
            Assert.AreEqual(offsetedFrame, existingFrame.time);

            bool found = td.anim.allTracks[testTrack].TryGetKeyIndex(copyFrame1, out _);
            Assert.IsFalse(found);
            found = td.anim.allTracks[testTrack].TryGetKeyIndex(offsetedFrame, out _);
            Assert.IsTrue(found);
        }

        [Test]
        public void SampleInterpolationTest()
        {
            AnimationTestData td = new AnimationTestData();
            Debug.Log(td.anim.allTracks[3].name);
            td.sc.SelectTrack(3);
            td.pc.SetFrame(10);
            td.oc.SetKeyOnCursor(0f);
            td.pc.SetFrame(20);
            td.oc.SetKeyOnCursor(5f);
            td.oc.SetKeyEasingOnCursor(EasingType.linear);

            Assert.AreEqual(0f, td.data.animation.Sample(3, 10));
            Assert.AreEqual(2.5f, td.data.animation.Sample(3, 15));
            Assert.AreEqual(5f, td.data.animation.Sample(3, 20));

            const bool loop = true;
            NativeAnimation anim = NativeAnimation.Create(td.anim, default, loop);
            for (int i = 10; i <= 20; i++)
            {
                var pose = anim.GetPose(i * NativeAnimation.perFrameTime, loop);

                //Debug.Log($"Frame: {i}\n \tNativeValue = { pose.torso.hipsRoll * Mathf.Rad2Deg } \n\tPhysicalAnimationValue= {td.data.animation.Sample(3, i)}");
            }
            anim.Dispose();
        }

        [Test]
        public void CopyPasteMultiTrackDifferentTrack()
        {
            AnimationTestData td = new AnimationTestData();

            for (int i = 2; i < 6; i++)
            {
                td.oc.CreateKey(i, 10);
                td.oc.CreateKey(i, 20);
            }

            td.sc.ClearSelection();

            for (int i = 2; i < 6; i++)
            {
                td.sc.AddSelection(i, 10);
                td.sc.AddSelection(i, 20);
            }

            td.oc.Copy();

            td.oc.PasteInPosition(30, 10);

            for (int i = 10; i < 14; i++)
            {
                Assert.IsTrue(td.data.animation.allTracks[i].TryGetKeyIndex(30, out int index));
                Assert.IsTrue(td.data.animation.allTracks[i].TryGetKeyAtIndex(index, out var _));

                Assert.IsTrue(td.data.animation.allTracks[i].TryGetKeyIndex(40, out index));
                Assert.IsTrue(td.data.animation.allTracks[i].TryGetKeyAtIndex(index, out _));
            }
        }

        [Test]
        public void SetKeyTest()
        {
            const int targetFrame = 50;
            AnimationTestData td = new AnimationTestData();

            td.oc.SetKey(testTrack, targetFrame, 10);
            td.oc.SetKey(testTrack, targetFrame + 15, 10);

            bool found = td.anim.allTracks[testTrack].TryGetKeyIndex(targetFrame, out int index);
            found &= td.anim.allTracks[testTrack].TryGetKeyIndex(targetFrame, out index);

            Assert.AreEqual(2, td.anim.allTracks[testTrack].frames.Count);
            Assert.IsTrue(found);

            found = td.anim.allTracks[testTrack].TryGetKeyAtIndex(index, out var key);

            Assert.IsTrue(found);
            Assert.AreEqual(10, key.value);
        }
    }
}
