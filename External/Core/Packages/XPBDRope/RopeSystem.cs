//#define DEBUG_ROPES

using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Recoil;
using Recoil.Util;
using NBG.Core;
using NBG.Core.GameSystems;

namespace NBG.XPBDRope
{
    public class RopeSystem : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate, IOnPhysicsAfterSolve
    {
        [SerializeField]
        int iterations = 20;
        [SerializeField]
        [HideInInspector]
        LayerMask staticCollisionLayers;

        List<Rope> ropes = new List<Rope>();
        XpbdRopeSolver job;
        CollisionConstraintGenerationJob constraintJob;
        bool needFullRegeneration = true;

        NativeArray<CollisionData> previousCollisionData;
        public const float minDistanceForNearbyCollisionPoint = 0.05f;
        NativeArray<RaycastHit> collisionResults;
        NativeArray<SpherecastCommand> spherecastCommands;
        NativeArray<int> staticCollisionCount;
        NativeArray<int> ropeBodyIDs;
        NativeParallelHashMap<int, RigidTransform> originalBodyPositions;
        RopeData ropeSettings;
        RopeArticulation ropeArticulations = new RopeArticulation();

        [ClearOnReload]
        static RopeSystem instance;
        public static RopeSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RopeSystem>();
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        public void AddRope(Rope rope)
        {
            SetSystemActions(ScheduleJob, SchedulePositionReset);
            ropes.Add(rope);
            needFullRegeneration = true;
        }

        public void RemoveRope(Rope rope)
        {
            if (ropes.Contains(rope))
            {
                ropes.Remove(rope);
                needFullRegeneration = true;
            }
            if (ropes.Count == 0)
            {
                SetSystemActions(null, null);
            }
        }

        void SetSystemActions(System.Func<JobHandle, JobHandle> solveAction, System.Func<JobHandle, JobHandle> resetPositionAction)
        {
            if (instance == this)
            {
                // Note that RopeSolver system might have been destroyed by now
                var rs = GameSystemWorldDefault.Instance?.GetExistingSystem<RopeSolver>();
                var posReset = GameSystemWorldDefault.Instance?.GetExistingSystem<RopePositionReset>();
                if (rs != null && posReset != null)
                {
                    rs.solveAction = solveAction;
                    posReset.resetAction = resetPositionAction;
                }
            }
        }

        private void Regenerate()
        {
            CleanupExistingJob();

            if (ropes.Count > 0)
            {
                int pointCount = 0;
                for (int i = 0; i < ropes.Count; i++)
                {
                    pointCount += ropes[i].BoneCount + 1;
                }

                var activeBoneCounts = new NativeArray<int>(ropes.Count, Allocator.Persistent);
                var totalBoneCounts = new NativeArray<int>(ropes.Count, Allocator.Persistent);
                var endPoints = new NativeArray<int>(ropes.Count, Allocator.Persistent);
                var ropeLengths = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var ropeRadii = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var elasticCompliance = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var bendCompliance = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var bendLimit = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var twistLimits = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var connectedBodies = new NativeArray<int>(ropes.Count * 2, Allocator.Persistent);
                var connectedBodyAnchor = new NativeArray<float3>(ropes.Count * 2, Allocator.Persistent);
                var segmentLengths = new NativeArray<float>(pointCount, Allocator.Persistent);
                var invMass = new NativeArray<float>(pointCount, Allocator.Persistent);
                var maxSegmentSeparation = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var staticFriction = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                var dynamicFriction = new NativeArray<float>(ropes.Count, Allocator.Persistent);
                ropeBodyIDs = new NativeArray<int>(pointCount, Allocator.Persistent);

                ropeSettings = new RopeData(pointCount, staticFriction, dynamicFriction, activeBoneCounts, totalBoneCounts, endPoints,
                    ropeLengths, ropeRadii, segmentLengths, invMass, elasticCompliance, bendCompliance, bendLimit, twistLimits, connectedBodies,
                    connectedBodyAnchor, maxSegmentSeparation);

                RegenerateAllRopeData();

                job = XpbdRopeSolver.Createjob(ropeSettings, World.main.dt, iterations);

                var castCount = ropeSettings.totalSubdivisions;
                previousCollisionData = new NativeArray<CollisionData>(castCount, Allocator.Persistent);
                spherecastCommands = new NativeArray<SpherecastCommand>(castCount, Allocator.Persistent);
                collisionResults = new NativeArray<RaycastHit>(castCount, Allocator.Persistent);
                staticCollisionCount = new NativeArray<int>(1, Allocator.Persistent);
                // Skip the last point of each rope since it uses the same body and add two extra slots for each rope for attached bodies.
                originalBodyPositions = new NativeParallelHashMap<int, RigidTransform>(pointCount - ropes.Count + ropes.Count * 2, Allocator.Persistent);

                job.iterationMarker = new Unity.Profiling.ProfilerMarker("Iteration");

                constraintJob = new CollisionConstraintGenerationJob(job.x, job.oldX, ropeSettings, previousCollisionData, false);
                needFullRegeneration = false;
            }
        }

        void GetConnectedBodyData(Rigidbody target, ConfigurableJoint joint, int id, NativeArray<int> connectedBodies, NativeArray<float3> connectedBodyAnchor, int ropeVersion)
        {
            if (target != null)
            {
                connectedBodies[id] = ManagedWorld.main.FindBody(target, false);
                if (ropeVersion > 0)
                {
                    connectedBodyAnchor[id] = Vector3.Scale(joint.anchor, target.transform.lossyScale) - target.centerOfMass;
                }
                else // Old ropes have joints on the rope segments
                {
                    connectedBodyAnchor[id] = Vector3.Scale(joint.connectedAnchor, target.transform.lossyScale) - target.centerOfMass;
                }
            }
            else
            {
                connectedBodies[id] = World.environmentId;
                connectedBodyAnchor[id] = float3.zero;
            }
        }

        void RegenerateAllRopeData()
        {
            ropeArticulations.RebuildAllArticulations(ropes);

            for (int i = 0; i < ropes.Count; i++)
            {
                UpdateRopeData(i, false);
            }
            ropeSettings.RecalculateSubdivisions();
        }

        void UpdateRopeData(int id, bool rebuildArticulations)
        {
            var rope = ropes[id];

            var startPoint = id > 0 ? ropeSettings.ropeEndPoints[id - 1] : 0;

            ropeSettings.ropeEndPoints[id] = startPoint + rope.BoneCount + 1;
            ropeSettings.ropeLengths[id] = rope.CurrentRopeLength;
            ropeSettings.activeBoneCounts[id] = rope.ActiveBoneCount;
            ropeSettings.totalBoneCounts[id] = rope.BoneCount;
            ropeSettings.ropeRadii[id] = rope.Radius;
            ropeSettings.elasticCompliance[id] = rope.ElasticCompliance;
            ropeSettings.bendCompliance[id] = rope.BendCompliance;
            ropeSettings.bendLimit[id] = rope.BendLimit;
            ropeSettings.twistLimits[id] = rope.UseTwistLimits ? rope.TwistLimit : -1f;
            ropeSettings.maxSegmentSeparation[id] = rope.MaxSegmentSeparation;
            ropeSettings.staticFriction[id] = rope.StaticFriction;
            ropeSettings.dynamicFriction[id] = rope.DynamicFriction;
            GetConnectedBodyData(rope.BodyStartIsAttachedTo, rope.StartBodyJoint, id * 2, ropeSettings.connectedBodies, ropeSettings.connectedBodyAnchor, rope.Version);
            GetConnectedBodyData(rope.BodyEndIsAttachedTo, rope.EndBodyJoint, id * 2 + 1, ropeSettings.connectedBodies, ropeSettings.connectedBodyAnchor, rope.Version);

            for (int x = 0; x < rope.BoneCount; x++)
            {
                var bone = rope.bones[x];
                var point = startPoint + x;
                ropeSettings.segmentLengths[point] = bone.BoneLength;
                ropeBodyIDs[point] = bone.Id;
                ropeSettings.segmentInvMass[point] = rope.GetPointInvMass(x);
            }
            if (rope.BoneCount > 0)
            {
                // Since all the other arrays are indexed via points and not bones, we add an extra element for each rope to match the points.
                // This allows working in parallel jobs without the issues of writing in the wrong index
                var lastSegment = rope.BoneCount - 1;
                var lastPoint = startPoint + rope.BoneCount;
                var bone = rope.bones[lastSegment];
                ropeSettings.segmentLengths[lastPoint] = bone.BoneLength;
                ropeBodyIDs[lastPoint] = bone.Id;
                ropeSettings.segmentInvMass[lastPoint] = rope.GetPointInvMass(lastSegment + 1);
            }

            if (rebuildArticulations)
            {
                ropeArticulations.RebuildArticulationForRope(rope);
            }
        }

        void CleanupExistingJob()
        {
            if (job.IsCreated)
            {
                job.Dispose();
                spherecastCommands.Dispose();
                staticCollisionCount.Dispose();
                collisionResults.Dispose();
                originalBodyPositions.Dispose();
                constraintJob.Dispose();
                previousCollisionData.Dispose();
                ropeBodyIDs.Dispose();
                ropeSettings.Dispose();
                ropeArticulations.Dispose();
            }
        }

        JobHandle ScheduleJob(JobHandle dependsOn)
        {
            if (job.IsCreated)
            {
                job.dt = World.main.dt;
                job.iterations = iterations;

                job.connectedBodyPositions.Clear();
                originalBodyPositions.Clear();

                var readJob = new ReadDataForJob(ropeBodyIDs, ropeSettings, job.x, job.oldX, job.connectedBodyPositions, originalBodyPositions);
                dependsOn = readJob.Schedule(ropeBodyIDs.Length, 16, dependsOn);

                bool useStaticCollisions = false;//staticCollisionLayers.value != 0;

                if (useStaticCollisions)
                {
                    dependsOn = PrepareForCollisionDetection(dependsOn);
                    dependsOn = DetectCollisions(dependsOn);
                }
                constraintJob.useStaticCollisions = useStaticCollisions;
                dependsOn = constraintJob.Schedule(ropeSettings.ropeLengths.Length, 1, dependsOn);
                dependsOn = WriteCollisionData(constraintJob, job, dependsOn);
                dependsOn = job.Schedule(dependsOn);
                
                var writeJob = new WriteDataJob(ropeBodyIDs, ropeSettings, job.x, job.oldX, job.connectedBodyPositions);
                dependsOn = writeJob.Schedule(ropeBodyIDs.Length, 16, dependsOn);
            }


            return dependsOn;
        }

        JobHandle SchedulePositionReset(JobHandle dependsOn)
        {
            if (job.IsCreated)
            {
                dependsOn = new ResetPositionJob(originalBodyPositions).Schedule(dependsOn);
            }
            return dependsOn;
        }

        void RegenerateRopeDataIfNeeded()
        {
            UnityEngine.Profiling.Profiler.BeginSample("RegenerateRopes");

            for(int i = 0; i < ropes.Count; i++)
            {
                var rope = ropes[i];
                if (rope.NeedsRegeneration)
                {
                    rope.Regenerate();
                    if (!needFullRegeneration)
                    {
                        UpdateRopeData(i, true);
                    }
                }
            }

            if (needFullRegeneration)
            {
                Regenerate();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void ExecuteBeforeSolve()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Send BeforeSolve Events");

            foreach (var rope in ropes)
            {
                rope.BeforeSolve();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        JobHandle PrepareForCollisionDetection(JobHandle dependsOn)
        {

            var previousCollisionUpdateJob = new PrepareForCollisionDetectionJob(job.x, job.oldX, ropeSettings, previousCollisionData, staticCollisionLayers.value, spherecastCommands);
            var handle = previousCollisionUpdateJob.Schedule(dependsOn);
            return handle;
        }

        JobHandle DetectCollisions(JobHandle dependency = default)
        {
            var handle = SpherecastCommand.ScheduleBatch(spherecastCommands, collisionResults, 1, dependency);

            // Jobs do not support scalar return values, must use nativearray to get back a single number!
            var readCollisionDataJob = new ReadCollisionDataJob(ropeSettings, collisionResults, previousCollisionData);
            handle = readCollisionDataJob.Schedule(handle);
            return handle;
        }

        void ExecuteAfterSolve()
        {
            UnityEngine.Profiling.Profiler.BeginSample("SendAfterSolveEvents");
            foreach (var rope in ropes)
            {
                rope.AfterSolve();
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        JobHandle WriteCollisionData(CollisionConstraintGenerationJob constraintJob, XpbdRopeSolver solver, JobHandle dependency = default)
        {
            var writeJob = new WriteCollisionConstraintsJob();
            writeJob.collisionConstraints = constraintJob.collisionConstraints;
            writeJob.mainCollisionConstraints = solver.mainCollisionConstraints;
            writeJob.secondaryCollisionConstraints = solver.secondaryCollisionConstraints;
            writeJob.ropeData = ropeSettings;

            var handle = writeJob.Schedule(dependency);
            return handle;
        }

        private void OnDrawGizmos()
        {
#if DEBUG_ROPES
            for(int i = 0; i < job.x.Length; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(job.oldX[i], 0.25f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(job.x[i], 0.25f);

            }
#endif
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IManagedBehaviour.OnLevelLoaded()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogError("Multiple rope system instances detected!", gameObject);
            }
            OnFixedUpdateSystem.Register(this);
            OnPhysicsAfterSolveSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            SetSystemActions(null, null);
            OnFixedUpdateSystem.Unregister(this);
            OnPhysicsAfterSolveSystem.Unregister(this);
            CleanupExistingJob();
        }

        void OnDestroy()
        {
            CleanupExistingJob();
        }

        void IOnFixedUpdate.OnFixedUpdate()
        {
            RegenerateRopeDataIfNeeded();
            ExecuteBeforeSolve();
        }

        void IOnPhysicsAfterSolve.OnPhysicsAfterSolve()
        {
            ExecuteAfterSolve();
        }

        [UpdateInGroup(typeof(PhysicsBeforeSolve))]
        public class RopeSolver : GameSystemWithJobs
        {
            public RopeSolver()
            {
                WritesData(typeof(WorldJobData));
            }

            public System.Func<JobHandle, JobHandle> solveAction;

            protected override JobHandle OnUpdate(JobHandle dependsOn)
            {
                if (solveAction != null)
                {
                    return solveAction(dependsOn);
                }
                return dependsOn;
            }
        }

        [UpdateInGroup(typeof(PhysicsAfterSolve))]
        public class RopePositionReset : GameSystemWithJobs
        {
            public RopePositionReset()
            {
                WritesData(typeof(WorldJobData));
            }

            public System.Func<JobHandle, JobHandle> resetAction;

            protected override JobHandle OnUpdate(JobHandle dependsOn)
            {
                if (resetAction != null)
                {
                    return resetAction(dependsOn);
                }
                return dependsOn;
            }
        }
    }
}
