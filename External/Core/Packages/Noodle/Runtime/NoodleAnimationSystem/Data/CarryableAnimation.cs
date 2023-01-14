using NBG.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles.Animation
{
    [System.Serializable]
    public struct CarryableAnimation
    {
        public PhysicalAnimation one;
        public PhysicalAnimation reach;
        public PhysicalAnimation two;
    }
    [System.Serializable]
    public class CarryableAnimationDB
    {
        [Header("Sticks")]
        public CarryableAnimation stick;
        public CarryableAnimation torch;
        public CarryableAnimation shield;
        public CarryableAnimation shuriken;
        public CarryableAnimation axe;
        public CarryableAnimation pole;

        [Header("Guns")]
        public CarryableAnimation gun;
        public CarryableAnimation tool;
        public CarryableAnimation jigsaw;

        [Header("Crates")]
        public CarryableAnimation crate;
        public CarryableAnimation microchip;
        public CarryableAnimation jackhammer;

        [Header("Stones")]
        public CarryableAnimation stone;
        public CarryableAnimation coin;
    }

    public enum CarryableAnimationType
    {
        None=0,
        Carry = 1,
        Reach = 2,
        TwoHanded=3
    }
    /// <summary>
    /// Represents a key to adress specific animation from CarryableAnimation database. Animations are described as:
    /// * carryable type,
    /// * animation subtype <see cref="CarryableAnimationType"/>
    /// * whether it's flipped (all animations are authored as carried with the right hand, so left hand carry references flipped animation)
    /// Default, non-carryable animations are also here <see cref="defaultReach"/> and <see cref="defaultCarry"/>
    /// </summary>
    public struct CarryableAnimationRef : IEquatable<CarryableAnimationRef>
    {
        public int hash;

        public static CarryableAnimationRef empty => default;
        public static CarryableAnimationRef defaultReach => new CarryableAnimationRef(0, CarryableAnimationType.Reach);
        public static CarryableAnimationRef defaultCarry(bool left) => new CarryableAnimationRef(0, CarryableAnimationType.Carry, left);
        public bool isEmpty => hash == 0;
        public CarryableAnimationType subtype => (CarryableAnimationType)(math.abs(hash) % 4);

        public CarryableAnimationRef(int hash)
        {
            this.hash = hash;
        }
        public CarryableAnimationRef(int typeHash, CarryableAnimationType subtype)
        {
            this.hash = typeHash/4*4 + (int)subtype;
        }
        public CarryableAnimationRef(int typeHash, CarryableAnimationType subtype, bool flip)
        {
            this.hash = typeHash / 4 * 4 + (int)subtype;
            if (flip) 
                hash = -hash;
        }
        public bool Equals(CarryableAnimationRef other)
        {
            return hash == other.hash;
        }
        public static CarryableAnimationRef FromType<T>() => new CarryableAnimationRef(CarryableBase.GetTypeId<T>());
        public static CarryableAnimationRef FromType<T>(CarryableAnimationType subtype) => new CarryableAnimationRef(CarryableBase.GetTypeId<T>(), subtype);//=> new CarryableAnimationRef(Unity.Burst.BurstRuntime.GetHashCode32<T>()+(int)subtype);

        public CarryableAnimationRef flipped=>new CarryableAnimationRef(-hash);
        public bool isFlipped => hash < 0;
        public override int GetHashCode() => hash;

        public override string ToString() => $"{hash} {subtype}";

    }
    public unsafe struct CarryableAnimator 
    {
        private UnsafeParallelHashMap<CarryableAnimationRef, IntPtr> map;
        private UnsafeParallelHashMap<int, IntPtr> allLoaded;

        public bool Contains(CarryableAnimationRef animRef) => animRef.isFlipped? map.ContainsKey(animRef.flipped): map.ContainsKey(animRef);
        public bool Contains(int type, CarryableAnimationType subtype) => Contains(new CarryableAnimationRef(type, subtype));


        private NativeAnimation* GetPtr(int type, CarryableAnimationType subtype)
        {
            var key = new CarryableAnimationRef(type,subtype);
            if (map.TryGetValue(key, out var animation))
                return (NativeAnimation*)animation;
            return null;
        }
        private NativeAnimation Get(CarryableAnimationRef key)
        {
            if (!key.isEmpty)
            {
                if (key.isFlipped)
                    key = key.flipped;
                if (map.TryGetValue(key, out var animation))
                    return *(NativeAnimation*) animation;
            }
            return NativeAnimation.empty;
        }

        public NoodlePose GetPose01(CarryableAnimationRef key, float aim01)
        {
            var animation = Get(key);
            Debug.Assert(!animation.isEmpty, "Animation not found");

            var pose =animation.GetPose01(aim01);
            if (key.isFlipped)
                pose.Flip();
            return pose;
        }
       
        public HandPose GetHandPose01(CarryableAnimationRef key, float aim01, bool left)
        {
            var animation = Get(key);
            Debug.Assert(!animation.isEmpty, "Animation not found");
            if (key.isFlipped)
                left = !left;

            var pose = (left ? NoodleAnimationLayout.armL : NoodleAnimationLayout.armR).GetPose01(animation, aim01);
            if (key.isFlipped)
                pose.Flip();
            return pose;
        }
        public PivotPose GetPivotPose01(int type, CarryableAnimationType subtype, float aim01) => GetPivotPose01(new CarryableAnimationRef(type, subtype), aim01);
        public PivotPose GetPivotPose01(CarryableAnimationRef key, float aim01)
        {
            var animation = Get(key);
            Debug.Assert(!animation.isEmpty, "Animation not found");

            var pose = NoodleAnimationLayout.pivotR.GetPose01(animation, aim01);
            if (key.isFlipped)
                pose.Flip();
            return pose;
        }


        public CarryableAnimator(NoodleAnimator animator, CarryableAnimationDB data, in NoodleDimensions dim)
        {
            map = new UnsafeParallelHashMap<CarryableAnimationRef, IntPtr>(100, Allocator.Persistent);
            allLoaded = new UnsafeParallelHashMap<int, IntPtr>(100, Allocator.Persistent);

            map[CarryableAnimationRef.defaultCarry(false)] = (IntPtr)FindOrLoad(dim, animator.hold);
            map[CarryableAnimationRef.defaultReach] = (IntPtr)FindOrLoad(dim, animator.grab);
            Load<CarryableBase>(dim, null, null, null); // default nothing

            // Sticks
            Load<MetaStickCarryable, CarryableBase>(dim, null,null,null);
            Load<StickCarryable, MetaStickCarryable>(dim, data.stick);
            Load<TorchCarryable, MetaStickCarryable>(dim, data.torch);
            Load<ShieldCarryable, MetaStickCarryable>(dim, data.shield);
            Load<ShurikenCarryable, MetaStickCarryable>(dim, data.shuriken);
            // Guns
            Load<MetaGunCarryable, CarryableBase>(dim, null, null, null);
            Load<GunCarryable, MetaGunCarryable>(dim, data.gun);
            Load<ToolCarryable, MetaGunCarryable>(dim, data.tool);
            Load<JigsawCarryable, MetaGunCarryable>(dim, data.jigsaw);

            // Stones
            Load<MetaStoneCarryable, CarryableBase>(dim, null, null, null);
            Load<StoneCarryable, MetaStoneCarryable>(dim, data.stone);
            Load<CoinCarryable, MetaStoneCarryable>(dim, data.coin);

            // 2 handed
            Load<MetaCrateCarryable, CarryableBase>(dim, null, null, null);
            Load<CrateCarryable, MetaCrateCarryable>(dim, data.crate);
            Load<MicrochipCarryable, MetaCrateCarryable>(dim, data.microchip);
            Load<JackhammerCarryable, MetaCrateCarryable>(dim, data.jackhammer);
            Load<AxeCarryable, MetaStickCarryable>(dim, data.axe);
            Load<PoleCarryable, MetaStickCarryable>(dim, data.pole);
            
        }

      

        private NativeAnimation* FindOrLoad(in NoodleDimensions dim, PhysicalAnimation animation)
        {
            if (animation == null) return null;

            if (allLoaded.TryGetValue(animation.GetHashCode(), out var val))
                return (NativeAnimation*)val;
            var native= Unsafe.MallocCopy(NativeAnimation.Create(animation, dim, loop: false), Allocator.Persistent);
            allLoaded[animation.GetHashCode()] = (IntPtr) native;
            return native;
        }


        private void Load<TCarryable, TMeta>(in NoodleDimensions dim, CarryableAnimation clips) =>
            Load<TCarryable, TMeta>(dim, clips.one, clips.reach, clips.two);

        private void Load<TCarryable, TMeta>(in NoodleDimensions dim, PhysicalAnimation hold, PhysicalAnimation grab, PhysicalAnimation twoH)
        {
            var carryable = CarryableBase.GetTypeId<TCarryable>();
            var meta = CarryableBase.GetTypeId<TMeta>();
            var holdAnim = FindOrLoad(dim, hold);
            var grabAnim = FindOrLoad(dim, grab);
            var twoHAnim = FindOrLoad(dim, twoH);
            if (holdAnim == null) holdAnim = GetPtr(meta, CarryableAnimationType.Carry);
            if (grabAnim == null) grabAnim = GetPtr(meta, CarryableAnimationType.Reach);
            if (twoHAnim == null) twoHAnim = GetPtr(meta, CarryableAnimationType.TwoHanded);
            if (holdAnim != null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.Carry)] = (IntPtr)holdAnim;
            if (grabAnim != null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.Reach)] = (IntPtr)grabAnim;
            if (twoHAnim != null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.TwoHanded)] = (IntPtr)twoHAnim;
        }
        private void Load<TCarryable>(in NoodleDimensions dim, PhysicalAnimation hold, PhysicalAnimation grab, PhysicalAnimation twoH)
        {
            var holdAnim = FindOrLoad(dim, hold);
            var grabAnim = FindOrLoad(dim, grab);
            var twoHAnim = FindOrLoad(dim, twoH);
            if(holdAnim!=null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.Carry)] = (IntPtr)holdAnim;
            if (grabAnim != null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.Reach)] = (IntPtr)grabAnim;
            if (twoHAnim != null)
                map[CarryableAnimationRef.FromType<TCarryable>(CarryableAnimationType.TwoHanded)] = (IntPtr)twoHAnim;
        }

        public void Dispose()
        {
            if(map.IsCreated)
                map.Dispose();
            if (allLoaded.IsCreated)
            {
                using (var keys = allLoaded.GetKeyArray(Allocator.Temp))
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        NativeAnimation* animation = (NativeAnimation*)allLoaded[keys[i]];
                        animation->Dispose();
                        Unsafe.Free(animation, Allocator.Persistent);
                    }
                }
                allLoaded.Dispose();
            }
        }

        
    }
    
}