using NBG.Unsafe;
using Noodles.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

namespace Noodles
{

    [System.Serializable]
    public struct NoodleAnimationDatabase : IDisposable
    {
        static readonly SharedStatic<IntPtr> _instancePtr = SharedStatic<IntPtr>.GetOrCreate<IntPtr>();
        int refCount;
        //static readonly SharedStatic<NoodleAnimationDatabase> _instance = SharedStatic<NoodleAnimationDatabase>.GetOrCreate<NoodleAnimationDatabase>();
        public unsafe static ref NoodleAnimationDatabase instance => ref *(NoodleAnimationDatabase*)_instancePtr.Data;
        public NativeAnimation idle;
        public NativeAnimation grab;
        public NativeAnimation hold;
        public NativeAnimation climb;
        public NativeAnimation swing;

        public NativeAnimation freeFall;
        public NativeAnimation hurt;

        public UnsafeArray<NativeAnimation> idleAnimations;

        public RingAnimation walk;
        public RingAnimation run;
        public NativeAnimation stepInPlace;

        public NativeAnimation jump;
        public RingAnimation fall;

        public CarryableAnimator carry;

        public NativeAnimation wakeUpProne;
        public NativeAnimation wakeUpSupine;

        [Space]
        public Emote emote0, emote1, emote2, emote3;

        public unsafe static NoodleAnimationDatabase Get(NoodleAnimator animator, in NoodleDimensions dim)
        {
            if (_instancePtr.Data== IntPtr.Zero)
            {
                _instancePtr.Data = (IntPtr)Unsafe.Malloc<NoodleAnimationDatabase>(Unity.Collections.Allocator.Persistent,Unity.Collections.NativeArrayOptions.ClearMemory);
                instance.Bake(animator, dim);
            }
            instance.refCount++;
            return instance;
        }
        public unsafe static void ReleaseRef()
        {
            instance.refCount--;
            if (instance.refCount == 0)
            {
                instance.Dispose();
                _instancePtr.Data = IntPtr.Zero;
            }
        }

        private void Bake(NoodleAnimator animator, in NoodleDimensions dim)
        {
            
            idle = NativeAnimation.Create(animator.idle, dim, loop: false);
            grab = NativeAnimation.Create(animator.grab, dim, loop: false);
            hold = NativeAnimation.Create(animator.hold, dim, loop: false);
            climb = NativeAnimation.Create(animator.climb, dim, loop: false);
            swing = NativeAnimation.Create(animator.swing, dim, loop: false);
            freeFall = NativeAnimation.Create(animator.freeFall, dim, loop: false);
            hurt = NativeAnimation.Create(animator.hurt, dim, loop: false);

            walk = new RingAnimation(animator.walkAnimations, dim);
            run = new RingAnimation(animator.runAnimations, dim);
            stepInPlace = NativeAnimation.Create(animator.stepInPlace, dim, loop: true);

            jump = NativeAnimation.Create(animator.jump, dim, loop: false);
            fall = new RingAnimation(animator.fall, dim);

            carry = new CarryableAnimator(animator, animator.carry, dim);

            idleAnimations = new UnsafeArray<NativeAnimation>(animator.idleAnimations.Length, Unity.Collections.Allocator.Persistent);
            for (int i=0; i<animator.idleAnimations.Length;i++)
                idleAnimations[i] = NativeAnimation.Create(animator.idleAnimations[i], dim, loop: false);


            wakeUpProne = NativeAnimation.Create(animator.wakeUpProne, dim, loop: false);
            wakeUpSupine = NativeAnimation.Create(animator.wakeUpSupine, dim, loop: false);

            if (animator.emote0 != null) emote0 = animator.emote0.Create(dim);
            if (animator.emote1 != null) emote1 = animator.emote1.Create(dim);
            if (animator.emote2 != null) emote2 = animator.emote2.Create(dim);
            if (animator.emote3 != null) emote3 = animator.emote3.Create(dim);
        }

        public void Dispose()
        {
            idle.Dispose();
            grab.Dispose();
            hold.Dispose();
            climb.Dispose();
            swing.Dispose();
            freeFall.Dispose();
            hurt.Dispose();
            for (int i = 0; i < idleAnimations.Length; i++)
                idleAnimations[i].Dispose();
            idleAnimations.Dispose();
            walk.Dispose();
            run.Dispose();
            stepInPlace.Dispose();
            jump.Dispose();
            fall.Dispose();
            carry.Dispose();
            wakeUpProne.Dispose();
            wakeUpSupine.Dispose();
            emote0.Dispose();
            emote1.Dispose();
            emote2.Dispose();
            emote3.Dispose();
        }
        public void Rebake(NoodleAnimator animator, in NoodleDimensions dim)
        {
            Dispose();
            Bake(animator, dim);
        }
    }
}
