using NBG.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NBG.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using NBG.Core;

namespace Recoil
{

    public struct Velocity4
    {
        public float4 angular;
        public float4 linear;

        public Velocity4(float3 angular, float3 linear)
        {
            this.angular = angular.To4D();
            this.linear = linear.To4D();
        }
        public Velocity4(float4 angular, float4 linear)
        {
            this.angular = angular;
            this.linear = linear;
        }
        public Velocity4(MotionVector v) : this(v.angular, v.linear) { }


        public float3 angular3 { get => angular.To3D(); set => angular = value.To4D(); }
        public float3 linear3 { get => linear.To3D(); set => linear = value.To4D(); }

        public static explicit operator Velocity4(MotionVector v) => new Velocity4(v);
        public static explicit operator MotionVector(Velocity4 v) => new MotionVector(v.angular3,v.linear3);
        public static Velocity4 operator +(Velocity4 a, Velocity4 b) => new Velocity4(a.angular+b.angular,a.linear+b.linear);
        public static Velocity4 operator -(Velocity4 a, Velocity4 b) => new Velocity4(a.angular - b.angular, a.linear - b.linear);
        public static Velocity4 operator -(Velocity4 b) => new Velocity4( - b.angular,  - b.linear);
    }
    public struct ArticulationLinkReference
    {
        public static ArticulationLinkReference Empty => new ArticulationLinkReference(-1, -1);
        int articulationLinkReference;

        public ArticulationLinkReference(int articulationId, int linkId)
        {
            articulationLinkReference = articulationId * 10000 + linkId;
        }
        public int articulationId => articulationLinkReference / 10000;
        public int linkId => articulationLinkReference % 10000;
        public bool isEmpty => articulationLinkReference < 0;
    }
    public struct Body
    {
        public Velocity4 v4;
        public MotionVector v { get => (MotionVector)v4; set => v4 = (Velocity4)value; }
        public RigidTransform x;
        public lt3x3 I;
        public lt3x3 invI;
        public float m;
        public float invM;

        public bool alive;
        
        public ArticulationLinkReference linkRef;
        public Entity entity; // reference to entity owning this body [optional]

        public bool sleepAllowed;
        public int sleepFrameCounter;

#if UNITY_EDITOR
        public float _lastE;
        public float _lastEphysX;
#endif
    }

    // entity referencing back to body
    public struct BodyId
    {
        public int bodyId;
    }
 
    public struct BodyStateCache
    {
        public MotionVector v;
        public RigidTransform x;
    }
    public struct BodyPhysXData
    {
        // from PhysX
        public bool isKinematic;
        public CollisionDetectionMode collisionDetectionMode;
        public quaternion inertiaTensorRotation;
        public float3 inertiaTensor;
        public float3 centerOfMass;
        public float angularVelocityLimit;
    }
   
    [BurstCompatible]
    public unsafe struct WorldImpl
    {
        public const float DefaultSleepThresholdMul = 1.02f; // Fudge a little bit to be on the safe side
        public const int DefaultStopApplyingSmallEnergyVelocityAfterFrames = 50; // Don't stop writing back small velocities immediately

        internal static readonly SharedStatic<WorldImpl> _mainWorld = SharedStatic<WorldImpl>.GetOrCreate<WorldImpl>(); 
        public int iterations;
        public int currentIteration;

        public int count;
        public int capacity;
        [NativeDisableUnsafePtrRestriction] public Body * bodies;
        [NativeDisableUnsafePtrRestriction] public BodyPhysXData* bodiesPhysX;

        public float dt;
        public float time;
        public float3 gravity;
        
        public float sleepThreshold;
        public float sleepThresholdMul;
        public int stopApplyingSmallEnergyVelocityAfterFrames;

        public WorldImpl(ManagedWorld world, int iterations, int nBodies)
        {
            this.iterations = iterations;
            this.currentIteration = 0;
            count = 0;
            capacity = nBodies;

            bodies = Unsafe.Malloc<Body>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            bodiesPhysX = Unsafe.Malloc<BodyPhysXData>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            articulations = new UnsafeList<Articulation>(0, Allocator.Persistent);
            constraints = new UnsafeList<ConstraintBlock>(0, Allocator.Persistent);

            gravity = Physics.gravity;
            dt = Time.fixedDeltaTime;
            time = 0;
            sleepThreshold = Physics.sleepThreshold;
            sleepThresholdMul = DefaultSleepThresholdMul;
            stopApplyingSmallEnergyVelocityAfterFrames = DefaultStopApplyingSmallEnergyVelocityAfterFrames; 
        }
        
        public void Resize(int nBodies)
        {
            Unsafe.Resize(ref bodies, capacity, nBodies, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Unsafe.Resize(ref bodiesPhysX, capacity, nBodies, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            capacity = nBodies;
        }
        
        public void Dispose()
        {


            Unsafe.Free(bodies, Allocator.Persistent);
            Unsafe.Free(bodiesPhysX, Allocator.Persistent);

            articulations.Dispose();
            constraints.Dispose();
        }


        public UnsafeList<Articulation> articulations;
        public UnsafeList<ConstraintBlock> constraints;
    }

    // this is public unmanaged API for world
    public unsafe struct World : IGetBody
    {
        public const float TERMINAL_FALL_VELOCITY = 20;
        public const float TERMINAL_VELOCITY = 40;
        public const float TERMINAL_RELATIVE_VELOCITY = 10; // inside the articulation
        public static World main => new World(WorldImpl._mainWorld.Data.AsPointer());
        [NativeDisableUnsafePtrRestriction] WorldImpl* impl;

        public int iterations => impl->iterations;
        public ref int currentIteration => ref impl->currentIteration;
        public int count => impl->capacity;


        public int constraintCount => impl->constraints.Length;
        public int articulationCount => impl->articulations.Length;

        public float dt => impl->dt;
        public float time => impl->time;
        public ref float3 gravity => ref impl->gravity;
        public World(WorldImpl* world)
        {
            this.impl = world;
        }
        public const int environmentId = -1;
        public static bool IsEnvironment(int id) => id == environmentId;
        public ref Body GetBody(int id) {
            Unsafe.CheckIndex(id, count);
            return ref impl->bodies[id];
        }
        public ref BodyPhysXData GetBodyPhysXData(int id)
        {
            Unsafe.CheckIndex(id, count);
            return ref impl->bodiesPhysX[id];
        }

        public bool PhysXKinematicState(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodiesPhysX[id].isKinematic;
        }

        public CollisionDetectionMode PhysXCollisionDetectionMode(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodiesPhysX[id].collisionDetectionMode;
        }

        public float3 PhysXInertiaTensor(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodiesPhysX[id].inertiaTensor;
        }

        public quaternion PhysXInertiaTensorRotation(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodiesPhysX[id].inertiaTensorRotation;
        }

        public void UpdateInertia(int id)
        {
            Unsafe.CheckIndex(id, count);
            ref Body body = ref impl->bodies[id];
            var info = impl->bodiesPhysX[id];
            if (body.m != 0 && body.alive)
            {
                var tensorRot = math.mul(body.x.rot, info.inertiaTensorRotation);
                body.I = RigidBodyInertia.CalculatIFromTensor(tensorRot, info.inertiaTensor);
                body.invI = info.isKinematic ? lt3x3.zero : re.inverse(body.I);
            }
        }

        public float3 LocalBodyToPhysX(int id, float3 pos)
        {
            if (IsEnvironment(id)) return pos;
            Unsafe.CheckIndex(id, count);
            return pos+impl->bodiesPhysX[id].centerOfMass;
        }
        public float3 LocalPhysXToBody(int id, float3 pos)
        {
            if (IsEnvironment(id)) return pos;
            Unsafe.CheckIndex(id, count);
            return pos - impl->bodiesPhysX[id].centerOfMass;
        }        

        // returns at unity transform origin
        public RigidTransform GetTransformPosition(int id)
        {
            Unsafe.CheckIndex(id, count);
            return math.mul(impl->bodies[id].x, RigidTransform.Translate(-impl->bodiesPhysX[id].centerOfMass));
        }
        public RigidTransform IntegrateTransformPosition(int id, float dt)
        {
            Unsafe.CheckIndex(id, count);
            var body = impl->bodies[id];
            var futureX = re.Integrate(body.x, body.v, dt);
            return math.mul(futureX, RigidTransform.Translate(-impl->bodiesPhysX[id].centerOfMass));
        }
        public void SetTransformPosition(int id, quaternion rot, float3 pos)
        {
            SetTransformPosition(id, new RigidTransform(rot, pos));
        }
        public void SetTransformPosition(int id, RigidTransform x)
        {
            Unsafe.CheckIndex(id, count);
            x = math.mul(x, RigidTransform.Translate(impl->bodiesPhysX[id].centerOfMass));

            ref var body = ref impl->bodies[id];
            if (!body.alive)
            {
                Debug.LogError("Moving dead body");
                return;
            }
            body.x.pos = x.pos;
            body.x.rot = x.rot;

        }
        public RigidTransform GetBodyPosition(int id)
        {
            Unsafe.CheckIndex(id, count);
            ref var body = ref impl->bodies[id];
            if (!body.alive)
                Debug.LogError("Getting position of a dead body");
            return body.x;
        }
        public RigidTransform IntegrateBodyPosition(int id, float dt)
        {
            Unsafe.CheckIndex(id, count);
            var body = impl->bodies[id];
            var futureX = re.Integrate(body.x, body.v, dt);
            return futureX;
        }
        public void SetBodyPosition(int id, RigidTransform x)
        {
            Unsafe.CheckIndex(id, count);
            ref var body = ref impl->bodies[id];
            if (!body.alive)
            {
                Debug.LogError("Moving dead body");
                return;
            }
            body.x.pos = x.pos;
            body.x.rot = x.rot;
        }
        public ref Velocity4 GetVelocity4(int id)
        {
            Unsafe.CheckIndex(id, count);
            return ref impl->bodies[id].v4;
        }
        //public void AddBodyReference(int id)
        //{
        //    if (IsEnvironment(id)) return;
        //    Unsafe.ChekIndex(id, count);
        //    if (id == 18) Debug.Log("alloc");
        //    impl->bodies[id].alive++;
        //}
        //public void ReleaseBody(int id)
        //{
        //    if (IsEnvironment(id)) return;
        //    Unsafe.ChekIndex(id, count);
        //    if (id == 18) Debug.Log("free");
        //    impl->bodies[id].alive--;
        //}
        public ref Articulation GetArticulation(int id)
        {
            Unsafe.CheckIndex(id, articulationCount);
            return ref impl->articulations.ElementAt(id);
        }
        public ref ConstraintBlock GetConstraint(int id)
        {
            Unsafe.CheckIndex(id, constraintCount);
            return ref impl->constraints.ElementAt(id);
        }

        public Entity GetEntity(int bodyId)
        {
            Unsafe.CheckIndex(bodyId, count);
            return impl->bodies[bodyId].entity;
        }
        public int GetBodyId(Entity entity, bool optional=false)
        {
            if (EntityStore.TryGetComponentData<BodyId>(entity, out var bodyId))
                return bodyId.bodyId;
            if(optional)
                return -1;
            throw new InvalidOperationException($"Entity does not have body associated");
        }
        public void LimitBodyVelocity(ref Velocity4 v, float angularVelocityLimit)
        {
            v.angular = v.angular.Clamp(angularVelocityLimit);

            if (v.linear.y < -World.TERMINAL_FALL_VELOCITY || math.length(v.linear) > World.TERMINAL_VELOCITY)
                v.linear = v.linear.To3D().SetY(math.max(v.linear.y, -World.TERMINAL_FALL_VELOCITY)).Clamp(World.TERMINAL_VELOCITY).To4D();
        }
        public void LimitArticulationVelocity(ref Articulation articulation)
        {
            var bodies = articulation.GetBodies();
            var v = bodies.CalculateVelocity(0, bodies.count);
            if (v.y < -World.TERMINAL_FALL_VELOCITY || math.length(v) > World.TERMINAL_VELOCITY)
            {
                var targetV = v.SetY(math.max(v.y, -World.TERMINAL_FALL_VELOCITY)).Clamp(World.TERMINAL_VELOCITY);
                var deltaV = targetV - v;
                for (int i = 0; i < bodies.count; i++)
                {
                    ref var bodyPhysX = ref bodies.GetBodyPhysXData(i);

                    var bodyV = bodies.GetVelocity(i);
                    bodyV.angular = bodyV.angular.Clamp(bodyPhysX.angularVelocityLimit);
                    bodyV.linear += +deltaV; // applu articulation wide deltaV
                    bodyV.linear = re.Clamp(bodyV.linear - targetV, TERMINAL_RELATIVE_VELOCITY) + targetV; // clamp relative to articulation velocity
                    bodies.SetVelocity(i, bodyV);
                }
            }
        }

        /// <summary>
        /// Frame count since the last time a body was assumed awake.
        /// </summary>
        /// <param name="id">Body id</param>
        /// <returns>Frame count</returns>
        public float GetSleepFrames(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodies[id].sleepFrameCounter;
        }

        /// <summary>
        /// Checks if sleeping is allowed.
        /// Certain systems, particularly while still in development, might not have sleep-aware implementations.
        /// </summary>
        /// <param name="id">Body id</param>
        /// <returns>Is sleeping allowed</returns>
        public bool GetIsSleepAllowed(int id)
        {
            Unsafe.CheckIndex(id, count);
            return impl->bodies[id].sleepAllowed;
        }

        /// <summary>
        /// Configures Recoil body sleep assistance.
        /// Certain systems, particularly while still in development, might not have sleep-aware implementations.
        /// </summary>
        /// <param name="id">Body id</param>
        /// <param name="allowed">Is sleeping allowed</param>
        public void ConfigureBodySleep(int id, bool allowed)
        {
            Unsafe.CheckIndex(id, count);
            impl->bodies[id].sleepAllowed = allowed;
        }

        /// <summary>
        /// Configures Recoil sleep assistance.
        /// </summary>
        /// <param name="thresholdMultiplier">Multiply the standard threshold (mass-normalized kinetic energy as used by PhysX) by this value. Increasing this value will permit faster moving bodies to sleep.</param>
        /// <param name="thresholdFrames">Amount of frames to wait before writing back small velocities. Decresing this value will speed up native PhysX sleep.</param>
        public void ConfigureSleep(
            float thresholdMultiplier = WorldImpl.DefaultSleepThresholdMul,
            int thresholdFrames = WorldImpl.DefaultStopApplyingSmallEnergyVelocityAfterFrames)
        {
            impl->sleepThresholdMul = thresholdMultiplier;
            impl->stopApplyingSmallEnergyVelocityAfterFrames = thresholdFrames;
        }

        /// <summary>
        /// Resets sleep state, waking up the body.
        /// </summary>
        /// <param name="id">Body id</param>
        public void SleepWakeUp(int id)
        {
            impl->bodies[id].sleepFrameCounter = 0;
        }
    }

    [BurstCompile]
    public unsafe class ManagedWorld
    {
        public const int ITERATIONS = 3;
        [ClearOnReload]
        public static ManagedWorld main;

        public static bool enablePhysXDataValidation = true;

        WorldImpl* impl;
        //public World worldRef => new World(impl);

        private BodyStateCache[] stateCache;
        private Rigidbody[] rigidbodies;
        private Dictionary<Rigidbody, int> rigidbodyMap = new Dictionary<Rigidbody, int>();

        public static void Create(int nBodies)
        {
            main = new ManagedWorld(nBodies);
        }
        private ManagedWorld(int nBodies)
        {
            WorldImpl._mainWorld.Data = new WorldImpl(this, ITERATIONS, nBodies);
            impl = WorldImpl._mainWorld.Data.AsPointer();
            rigidbodies = new Rigidbody[nBodies];
            stateCache = new BodyStateCache[nBodies];

        }

        private void Resize(int count)
        {
            if (impl->capacity >= count) return;
            count = math.ceilpow2(count);

            Unsafe.Resize(ref rigidbodies, count);
            Unsafe.Resize(ref stateCache, count);
            impl->Resize(count);

        }

        public static void Destroy() => main.Dispose();
        void Dispose()
        {
            ref var articulations = ref impl->articulations;
            for (int i = 0; i < articulations.Length; i++)
                if (!articulations[i].destroyed)
                    RemoveArticulation(i);
            ref var constraints = ref impl->constraints;
            for (int i = 0; i < constraints.Length; i++)
                if (!constraints[i].destroyed)
                    RemoveConstraint(i);


            impl->Dispose();
            impl = null;
            //Unsafe.Free(impl, Allocator.Persistent);

        }

        public Rigidbody GetRigidbody(int id) => id >= 0 && id < rigidbodies.Length ? rigidbodies[id] : null;
        public int RegisterBody(Rigidbody rigidbody)
        {
            if (rigidbody == null) return World.environmentId;
            //Throw argument exception (just like Dictionary) when body is already registered
            if (rigidbodyMap.ContainsKey(rigidbody))
                Debug.LogError("Body is already registered", rigidbody);
            // for now simple scan for empty slot
            int idx;
            for (idx = 0; idx < impl->count; idx++)
                if (!impl->bodies[idx].alive)
                    break;
            if (idx == impl->count) // did not find unused element, increase count
            {
                impl->count = idx + 1;
                if (impl->count >= impl->capacity)
                    Resize(impl->capacity + 1);
            }

            rigidbodies[idx] = rigidbody;
            rigidbodyMap[rigidbody] = idx;
            impl->bodies[idx].alive = true;
            impl->bodies[idx].linkRef = ArticulationLinkReference.Empty;
            impl->bodies[idx].entity = Entity.Null;
            impl->bodies[idx].sleepAllowed = true;
            impl->bodies[idx].sleepFrameCounter = 0;

            try
            {
                // write body and velocity
                ReadPhysXData(rigidbody, ref impl->bodiesPhysX[idx], ref impl->bodies[idx]);
                ReadDynamicState(rigidbody, ref impl->bodies[idx], ref stateCache[idx]);
                World.main.UpdateInertia(idx);
            }
            catch (Exception)
            {
                //Something during setup went wrong. Unregister and rethrow
                UnregisterBody(rigidbody);
                throw;
            }
            return idx;
        }

        public void ResyncPhysXBody(Rigidbody rigidbody)
        {
            int idx = rigidbodyMap[rigidbody];

            // write body and velocity
            ReadPhysXData(rigidbody, ref impl->bodiesPhysX[idx], ref impl->bodies[idx]);
            ReadDynamicState(rigidbody, ref impl->bodies[idx], ref stateCache[idx]);
            World.main.UpdateInertia(idx);
        }

        public void UnregisterBody(Rigidbody body)
        {
            UnregisterBody(FindBody(body));
        }
        public void UnregisterBody(int id)
        {
            if (World.IsEnvironment(id)) return;
            Unsafe.CheckIndex(id, impl->count);
            impl->bodies[id].alive = false;
            rigidbodyMap.Remove(rigidbodies[id]);
            rigidbodies[id] = null;

        }
        public int FindBody(Rigidbody rigidbody, bool optional=false)
        {
            if (rigidbody == null) return World.environmentId;

            if (rigidbodyMap.TryGetValue(rigidbody, out var idx))
                return idx;
            else if (!optional)
            {
                Debug.LogError($"Rigidbody {rigidbody} not registered!", rigidbody);
                return World.environmentId;
            }
            else
                return World.environmentId;
        }
        // creating references beweeen entities and bodies
        static ComponentTypeList bodyIdType;
        public void BindEntity(Entity entity, int bodyId)
        {
            if (bodyIdType.Length == 0)
            {
                bodyIdType = ComponentTypeList.Create();
                bodyIdType.AddType<BodyId>();
            }

            EntityStore.AddComponents(entity, bodyIdType);
            EntityStore.GetComponentData<BodyId>(entity).bodyId = bodyId;
            World.main.GetBody(bodyId).entity = entity;
        }

        /// <summary>
        /// Syncs recoil body position, transform position and rigidbody position immediately
        /// </summary>
        /// <param name="idx">Recoil body id</param>
        /// <param name="targetPlacement">Recoil body placement (where the object center of mass will be placed)</param>
        public void SetBodyPlacementImmediate (int idx, RigidTransform targetPlacement)
        {
            Unsafe.CheckIndex(idx, impl->count);
            ref var body = ref impl->bodies[idx];
            if (!body.alive)
            {
                Debug.LogError("Moving dead body", GetRigidbody(idx));
                return;
            }
            body.x = targetPlacement;
            stateCache[idx].x = body.x;

            quaternion targetRotation = targetPlacement.rot;
            float3 targetPosition = targetPlacement.pos - math.mul(body.x.rot, impl->bodiesPhysX[idx].centerOfMass);

            rigidbodies[idx].rotation = targetRotation;
            rigidbodies[idx].position = targetPosition;

            rigidbodies[idx].transform.SetPositionAndRotation(targetPosition, targetRotation);
        }


        public void MoveBody(int idx, float3 offset)
        {
            Unsafe.CheckIndex(idx, impl->count);
            ref var body = ref impl->bodies[idx];
            if (!body.alive)
            {
                Debug.LogError("Moving dead body", GetRigidbody(idx));
                return;
            }
            body.x.pos += offset;
            stateCache[idx].x.pos = body.x.pos;
            rigidbodies[idx].position = body.x.pos - math.mul(body.x.rot, impl->bodiesPhysX[idx].centerOfMass);
        }
        public void SetVelocity(int idx, MotionVector v)
        {
            Unsafe.CheckIndex(idx, impl->count);
            ref var body = ref impl->bodies[idx];
            if (!body.alive)
            {
                Debug.LogError("Moving dead body", GetRigidbody(idx));
                return;
            }
            body.v = v;
            stateCache[idx].v= v;
            rigidbodies[idx].velocity = v.linear;
            rigidbodies[idx].angularVelocity = v.angular;
        }

        private static void ValidatePhysXData(Rigidbody rigid, ref BodyPhysXData info)
        {
            if (rigid == null)
                throw new Exception($"Rigidbody is missing for a Recoil body that is still alive. Possible issue with unregistration.");

            Debug.Assert(rigid.isKinematic == info.isKinematic, "Can't change Rigidbody.isKinematic after registering it with Recoil", rigid);
        }

        private static void ReadPhysXData(Rigidbody rigid, ref BodyPhysXData info, ref Body body)
        {
            info.isKinematic = rigid.isKinematic;
            info.collisionDetectionMode = rigid.collisionDetectionMode;
            body.m = rigid.mass;
            body.invM = info.isKinematic?0: 1 / rigid.mass;
            
            info.inertiaTensor = rigid.inertiaTensor;
            if (math.lengthsq(info.inertiaTensor) == 0.0f)
            {
                Debug.LogError("Rigidbody has zero inertia tensor.", rigid);
                info.inertiaTensor = 1;
            }
            info.inertiaTensorRotation = rigid.inertiaTensorRotation;
            info.centerOfMass = rigid.centerOfMass;
            info.angularVelocityLimit = rigid.maxAngularVelocity;
        }
        private static void ReadDynamicState(Rigidbody rigid, ref Body body, ref BodyStateCache cache)
        {
            cache.x=body.x = new RigidTransform(rigid.rotation, rigid.worldCenterOfMass);
            cache.v=body.v = new MotionVector(rigid.angularVelocity, rigid.velocity);
        }


        public ref Articulation AddArticulation(out int id)
        {
            ref var articulations = ref impl->articulations;
            for (int i = 0; i < articulations.Length; i++)
            {
                if (articulations.ElementAt(i).destroyed)
                {
                    id = i;
                    articulations[id] = new Articulation();
                    return ref articulations.ElementAt(id);
                }
            }
            id = articulations.Length;
            articulations.Add(new Articulation());
            return ref articulations.ElementAt(id);
        }

        public void RemoveArticulation(int id)
        {
            ref var articulations = ref impl->articulations;
            articulations.ElementAt(id).Dispose();
            articulations.ElementAt(id).destroyed = true;
        }

        public ref ConstraintBlock AddConstraint(out int id)
        {
            ref var constraints = ref impl->constraints;
            for (int i = 0; i < constraints.Length; i++)
            {
                if (constraints.ElementAt(i).destroyed)
                {
                    id = i;
                    constraints[id] = new ConstraintBlock();
                    return ref constraints.ElementAt(id);
                }
            }
            id = constraints.Length;
            constraints.Add(new ConstraintBlock());
            return ref constraints.ElementAt(id);
        }

        public void RemoveConstraint(int id)
        {
            ref var constraints = ref impl->constraints;
            constraints.ElementAt(id).Dispose();
            constraints.ElementAt(id).destroyed = true;
        }


        //ComponentArray //https://medium.com/@5argon/all-of-the-unitys-ecs-job-system-gotchas-so-far-6ca80d82d19f

        public JobHandle ScheduleRead(JobHandle dependsOn = default)
        {
            ReadState();
            return new BuildInertiasJob().Schedule(World.main.count, 16, dependsOn);
        }


        [BurstCompile(CompileSynchronously = true)]
        public struct BuildInertiasJob : IJobParallelFor
        {
            public void Execute(int index)
            {
                World.main.UpdateInertia(index);
            }
        }
        [BurstCompile]
        public struct FricitonAndSelfAlignJob : IJobParallelFor
        {
            public void Execute(int b)
            {
                ref var body = ref World.main.GetBody(b);
                if (!body.alive) return;
                //Friction to allow body sleep
                var selfAligning = 1f;
                var angularFrictionAcceleration = .05f;
                var linearFrictionAcceleration = .05f;
                ref var vel = ref World.main.GetVelocity4(b);
                bool movingAngular = math.any(((MotionVector)vel).angular != float3.zero);
                bool movingLinear = math.any(((MotionVector)vel).linear != float3.zero);

                if (movingAngular)
                {
                    vel.angular = re.MoveTowards(vel.angular, 0, angularFrictionAcceleration * World.main.dt);

                    // self aligning torque
                    var v = vel.angular3.Clamp(10);
                    var selfAligningTorque = math.cross(v, re.mul(body.I, v));
                    var selfAligningAcc = re.mul(body.invI, selfAligningTorque);

                    vel.angular -= selfAligningAcc.To4D() * selfAligning * World.main.dt;
                }

                if (movingLinear)
                {
                    vel.linear = re.MoveTowards(vel.linear, 0, linearFrictionAcceleration * World.main.dt);
                }

                if ((movingAngular || movingLinear) && body.linkRef.isEmpty)
                {
                    ref var bodyPhysX = ref World.main.GetBodyPhysXData(b);
                    World.main.LimitBodyVelocity(ref vel, bodyPhysX.angularVelocityLimit);
                }
            }
        }

        private void ReadState()
        {
            Profiler.BeginSample("ReadDynamicState");
            var nBodies = impl->count;
            for (var b = 0; b < nBodies; b++)
            {
                if (impl->bodies[b].alive)
                {
                    if (enablePhysXDataValidation)
                        ValidatePhysXData(rigidbodies[b], ref impl->bodiesPhysX[b]);
                    ReadDynamicState(rigidbodies[b], ref impl->bodies[b], ref stateCache[b]);
                }

            }
            Profiler.EndSample();
        }

        public static float GetMassNormalizedKineticEnergy(Rigidbody r)
        {
            // Linear energy
            float E = 0.5f * r.mass * Mathf.Pow(r.velocity.magnitude, 2f);

            // Angular energy
            E += 0.5f * r.inertiaTensor.x * Mathf.Pow(r.angularVelocity.x, 2f);
            E += 0.5f * r.inertiaTensor.y * Mathf.Pow(r.angularVelocity.y, 2f);
            E += 0.5f * r.inertiaTensor.z * Mathf.Pow(r.angularVelocity.z, 2f);

            // Mass-normalized
            return E /= r.mass;
        }

        public static float GetMassNormalizedKineticEnergy(ref Body body)
        {
            // Linear energy
            float E = 0.5f * body.m * math.pow(math.length(body.v.linear), 2f);

            // Angular energy
            E += 0.5f * body.I.m00 * math.pow(body.v.angular.x, 2f);
            E += 0.5f * body.I.m11 * math.pow(body.v.angular.y, 2f);
            E += 0.5f * body.I.m22 * math.pow(body.v.angular.z, 2f);

            // Mass-normalized
            return E /= body.m;
        }

        public void WriteState()
        {
            Profiler.BeginSample("WriteState");

            var finalSleepThreshold = impl->sleepThreshold * impl->sleepThresholdMul;

            var nBodies = impl->count;
            for (var b = 0; b < nBodies; b++)
            {
                ref var body = ref impl->bodies[b];
                if (!body.alive)
                    continue;
                var rigid = rigidbodies[b];
                var cache = stateCache[b];
                var physX = impl->bodiesPhysX[b];
                var v = body.v;

                var bodyE = GetMassNormalizedKineticEnergy(ref body);

#if UNITY_EDITOR
                body._lastE = bodyE;
                body._lastEphysX = GetMassNormalizedKineticEnergy(rigid);
#endif

                var shouldSleep = (bodyE < finalSleepThreshold);
                if (shouldSleep)
                    body.sleepFrameCounter++;
                else
                    body.sleepFrameCounter = 0;

                if (body.sleepFrameCounter < impl->stopApplyingSmallEnergyVelocityAfterFrames || !body.sleepAllowed)
                {
                    if (math.any(v.linear != cache.v.linear))
                    {
                        if (math.isfinite(math.csum(v.linear)))
                            rigid.velocity = v.linear;
                        else
                            Log($"Writing NaN velocity {rigid.name}", rigid);
                    }

                    if (math.any(v.angular != cache.v.angular))
                    {
                        if (math.isfinite(math.csum(v.angular)))
                            rigid.angularVelocity = v.angular;
                        else
                            Log($"Writing NaN angular velocity {rigid.name}", rigid);
                    }
                }

                if (math.any(body.x.pos != cache.x.pos))
                {
                    rigid.position = body.x.pos - math.mul(body.x.rot, physX.centerOfMass);
                }

                if (math.any(body.x.rot.value != cache.x.rot.value))
                {
#if UNITY_SWITCH
                    rigid.rotation = math.normalizesafe(body.x.rot, quaternion.identity); // HACK: figure out why Switch complains here
#else
                    rigid.rotation = body.x.rot;
#endif
                }
            }
            impl->time += impl->dt;
            Profiler.EndSample();
        }

        public void ForceWritePosition(int b)
        {
            Unsafe.CheckIndex(b, impl->count);
            var body = impl->bodies[b];
            if (!body.alive) return;
            var rigid = rigidbodies[b];
            ref var cache = ref stateCache[b];
            var physX = impl->bodiesPhysX[b];
            if (math.any(body.x.pos != cache.x.pos))
                rigid.position = body.x.pos - math.mul(body.x.rot, physX.centerOfMass);
            if (math.any(body.x.rot.value != cache.x.rot.value))
                rigid.rotation = body.x.rot;
            cache.x = body.x;
        }
        public void ForceWriteVelocity(int b)
        {
            Unsafe.CheckIndex(b, impl->count);
            var body = impl->bodies[b];
            if (!body.alive) return;
            var rigid = rigidbodies[b];
            ref var cache = ref stateCache[b];
            var v = body.v;
            if (math.any(v.angular != cache.v.angular))
                if (math.isfinite(math.csum(v.angular)))
                    rigid.angularVelocity = v.angular.Clamp(10);
                else
                    Log($"Writing NaN angular velocity {rigid.name}", rigid);
            if (math.any(v.linear != cache.v.linear))
                if (math.isfinite(math.csum(v.linear)))
                    rigid.velocity = v.linear.SetY(math.max(v.linear.y, -10)).Clamp(20);
                else
                    Log($"Writing NaN velocity {rigid.name}", rigid);
            cache.v = body.v;
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void Log(string str, UnityEngine.Object context = null)
        {
            Debug.LogError(str, context);
        }

        public float3 debugTotalAngularMomentum
        {
            get
            {
                var pos = float3.zero;
                var total = float3.zero;
                var nBodies = impl->count;
                for (var b = 0; b < nBodies; b++)
                {
                    if (!impl->bodies[b].alive) continue;
                    var body = impl->bodies[b];
                    var f = new ForceVector(re.mul(body.I, body.v.angular), body.m * body.v.linear);
                    total += f.TranslateBy(pos - body.x.pos).angular;

                }
                return total;
            }
        }
        public float3 debugTotalLinearMomentum
        {
            get
            {
                var pos = float3.zero;
                var total = float3.zero;
                var nBodies = impl->count;
                for (var b = 0; b < nBodies; b++)
                {
                    if (!impl->bodies[b].alive) continue;

                    var body = impl->bodies[b];
                    var f = new ForceVector(re.mul(body.I, body.v.angular), body.m * body.v.linear);
                    total += f.TranslateBy(pos - body.x.pos).linear;

                }
                return total;
            }
        }
    }
}
