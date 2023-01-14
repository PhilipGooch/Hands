using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Recoil;
using NBG.Core;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace NBG.XPBDRope
{
    [Unity.Burst.BurstCompile]
    struct CollisionConstraintGenerationJob : IJobParallelFor
    {
        [ReadOnly]
        public RopeData ropeData;
        [ReadOnly]
        public NativeArray<float3> x;
        [ReadOnly]
        public NativeArray<float3> oldX;
        [NativeDisableParallelForRestriction]
        public NativeArray<CollisionConstraints> collisionConstraints;
        [ReadOnly]
        public NativeArray<CollisionData> collisionData;
        public const int maxCollisions = 16;
        public bool useStaticCollisions;

        Unity.Profiling.ProfilerMarker selfMarker;
        Unity.Profiling.ProfilerMarker otherMarker;

        public CollisionConstraintGenerationJob(NativeArray<float3> x, NativeArray<float3> oldX, RopeData ropeData, NativeArray<CollisionData> collisionData, bool useStaticCollisions)
        {
            this.x = x;
            this.oldX = oldX;
            this.ropeData = ropeData;
            this.collisionData = collisionData;
            this.useStaticCollisions = useStaticCollisions;
            selfMarker = new Unity.Profiling.ProfilerMarker("Self");
            otherMarker = new Unity.Profiling.ProfilerMarker("Other");
            collisionConstraints = new NativeArray<CollisionConstraints>(this.ropeData.totalSubdivisions * maxCollisions, Allocator.Persistent);
        }

        public void Execute(int targetRope)
        {
            int globalIndex = 0;
            for(int i = 0; i < targetRope; i++)
            {
                globalIndex += ropeData.subdivisions[i] * ropeData.totalBoneCounts[i] + 1;
            }

            var ropeLength = ropeData.ropeLengths[targetRope];
            var thisRopeRadius = ropeData.ropeRadii[targetRope];
            int resultIndex = globalIndex;
            int maxRopeCollisionCount = ropeData.subdivisions[targetRope] * ropeData.totalBoneCounts[targetRope] + 1;
            // This is where the next rope constraints start or the array ends
            var lastConstraintIndex = globalIndex + maxRopeCollisionCount;


            // TODO: Static collisions
            // For each bone, for each subdivision, check collision info
            /*if (useStaticCollisions)
            {
                var collisionPoint = collisionData[index].point;
                var collisionSubdivision = collisionData[index].subdivision;
                if (collisionPoint == target && collisionSubdivision == targetSubdivision)
                {
                    var position = collisionData[index].position;
                    var normal = collisionData[index].normal;

                    AddCollisionConstraint(index, target, targetSubdivision, -1, -1, ref detectedCollisions, position, normal, thisRopeRadius);

                }
            }*/

            var targetStartPoint = ropeData.GetStartPoint(targetRope);
            var targetEndPoint = ropeData.ropeEndPoints[targetRope];

            // Self collision
            selfMarker.Begin();
            for (int targetPoint = targetStartPoint; targetPoint < targetEndPoint; targetPoint++)
            {
                const int pointsToSkip = 3;
                // Check if each point collides with any other point that's at least 3 points away
                CheckForCollisionsBetweenRopes(targetRope, targetRope, targetPoint, targetPoint + pointsToSkip, targetPoint + 1, targetEndPoint, lastConstraintIndex, ref resultIndex);
            }
            selfMarker.End();

            // Other rope collision
            otherMarker.Begin();
            for(int otherRope = targetRope + 1; otherRope < ropeData.ropeLengths.Length; otherRope++)
            {
                var otherStartPoint = ropeData.GetStartPoint(otherRope);
                var otherEndPoint = ropeData.ropeEndPoints[otherRope];
                CheckForCollisionsBetweenRopes(targetRope, otherRope, targetStartPoint, otherStartPoint, targetEndPoint, otherEndPoint, lastConstraintIndex, ref resultIndex);
            }
            otherMarker.End();

            if (resultIndex < lastConstraintIndex)
            {
                collisionConstraints[resultIndex] = CollisionConstraints.CreateEmpty();
            }
        }

        void CheckForCollisionsBetweenRopes(int targetRope, int otherRope, int targetRopeStartPoint, int otherRopeStartPoint, int targetRopeEndPoint,
            int otherRopeEndPoint, int lastConstraintIndex, ref int resultIndex)
        {
            var ropeLength = ropeData.ropeLengths[targetRope];
            var otherRopeLength = ropeData.ropeLengths[otherRope];
            var maxDistance = (otherRopeLength + ropeLength) * 1.5f;
            var maxDistanceSq = maxDistance * maxDistance;
            var ropeDistance = math.lengthsq(x[targetRopeEndPoint - 1] - x[otherRopeEndPoint - 1]);
            var targetRadius = ropeData.ropeRadii[targetRope];
            var otherRadius = ropeData.ropeRadii[otherRope];

            float minDistance = targetRadius + otherRadius;
            float minDistanceSq = minDistance * minDistance;

            // Ropes are too far apart, skip the rope entirely
            if (ropeDistance > maxDistanceSq)
            {
                return;
            }

            for (int targetPoint = targetRopeStartPoint; targetPoint < targetRopeEndPoint; targetPoint++)
            {
                var thisSegmentLength = ropeData.segmentLengths[targetPoint];
                var targetPointStartPos = oldX[targetPoint];
                var targetPointEndPos = x[targetPoint];
                var isTargetEndPoint = ropeData.IsEndPoint(targetRope, targetPoint);
                var targetPointStartDir = isTargetEndPoint ? float3.zero : oldX[targetPoint + 1] - oldX[targetPoint];
                var targetPointEndDir = isTargetEndPoint ? float3.zero : x[targetPoint + 1] - x[targetPoint];

                float3 targetDelta = targetPointEndPos - targetPointStartPos;
                float targetDeltaDistanceSq = math.lengthsq(targetDelta);

                for (int otherPoint = otherRopeStartPoint; otherPoint < otherRopeEndPoint; otherPoint++)
                {
                    var otherPointStartPos = oldX[otherPoint];
                    var otherPointEndPos = x[otherPoint];
                    var isOtherEndPoint = ropeData.IsEndPoint(otherRope, otherPoint);
                    var otherPointStartDir = isOtherEndPoint ? float3.zero : oldX[otherPoint + 1] - oldX[otherPoint];
                    var otherPointEndDir = isOtherEndPoint ? float3.zero : x[otherPoint + 1] - x[otherPoint];

                    var otherSegmentLength = ropeData.segmentLengths[otherPoint];
                    float maxSegmentMovement = thisSegmentLength * thisSegmentLength + otherSegmentLength * otherSegmentLength;

                    var pointDiffSq = math.lengthsq(targetPointStartPos - otherPointStartPos);
                    var otherDeltaSq = math.lengthsq(otherPointEndPos - otherPointStartPos);

                    if (pointDiffSq - otherDeltaSq - targetDeltaDistanceSq - maxSegmentMovement > minDistanceSq)
                    {
                        // Points are too far away and not moving fast enough. Skip.
                        continue;
                    }

                    int subdivisionsForBone = isTargetEndPoint ? 1 : ropeData.subdivisions[targetRope];
                    for (int targetSubdivision = 0; targetSubdivision < subdivisionsForBone; targetSubdivision++)
                    {
                        var targetProgress = ropeData.GetProgress(targetRope, targetSubdivision);
                        var targetStart = targetPointStartPos + targetPointStartDir * targetProgress;
                        var targetEnd = targetPointEndPos + targetPointEndDir * targetProgress;

                        int subdivisionsForOtherBone = isOtherEndPoint ? 1 : ropeData.subdivisions[otherRope];
                        for(int otherSubdivision = 0; otherSubdivision < subdivisionsForOtherBone; otherSubdivision++)
                        {
                            var otherProgress = ropeData.GetProgress(otherRope, otherSubdivision);
                            var otherStart = otherPointStartPos + otherPointStartDir * otherProgress;
                            var otherEnd = otherPointEndPos + otherPointEndDir * otherProgress;

                            float3 diff = targetEnd - otherEnd;

                            float lengthSq = math.lengthsq(diff);

                            if (lengthSq - minDistanceSq < 0)
                            {
                                var normal = math.normalize(diff);
                                var collisionPoint = otherStart + normal * otherRadius;
                                // Colliding from the start. Add constraint.
                                // TODO: Reevaluate - the constraint position might be incorrect or the distance might be too big
                                AddCollisionConstraint(targetPoint, targetSubdivision, otherPoint, otherSubdivision, ref resultIndex, collisionPoint, normal, minDistance, lastConstraintIndex);
                            }
                            else
                            {
                                var otherDelta = otherEnd - otherStart;

                                var collisionDistance = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(targetStart, targetDelta, otherStart, otherDelta, minDistance);
                                if (collisionDistance >= 0.0f && collisionDistance <= 1f)
                                {
                                    var targetAtCollision = targetStart + targetDelta * collisionDistance;
                                    var otherAtCollision = otherStart + otherDelta * collisionDistance;
                                    var normal = math.normalize(targetAtCollision - otherAtCollision);
                                    var collisionPoint = otherAtCollision + normal * otherRadius;
                                    AddCollisionConstraint(targetPoint, targetSubdivision, otherPoint, otherSubdivision, ref resultIndex, collisionPoint, normal, minDistance, lastConstraintIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        void AddCollisionConstraint(int point1, int subdivision1, int point2, int subdivision2, ref int detectedCollisions, float3 collisionPoint, float3 normal, float minDistance, int lastConstraintIndex)
        {
            if (detectedCollisions < lastConstraintIndex)
            {
                collisionConstraints[detectedCollisions] = new CollisionConstraints(point1, subdivision1, point2, subdivision2, collisionPoint, normal, minDistance);
                detectedCollisions++;
            }
        }

        public void Dispose()
        {
            collisionConstraints.Dispose();
        }
    }

    [Unity.Burst.BurstCompile]
    struct ReadCollisionDataJob : IJob
    {
        [ReadOnly]
        public RopeData settings;
        [ReadOnly]
        public NativeArray<RaycastHit> collisionResults;
        public NativeArray<CollisionData> previousCollisionData;

        public ReadCollisionDataJob(RopeData settings, NativeArray<RaycastHit> collisionResults, NativeArray<CollisionData> previousCollisionData)
        {
            this.settings = settings;
            this.collisionResults = collisionResults;
            this.previousCollisionData = previousCollisionData;
        }

        public void Execute()
        {
            int startPoint = 0;
            int previousSubdivisions = 0;

            for (int rope = 0; rope < settings.activeBoneCounts.Length; rope++)
            {
                var ropeSubdivisions = settings.subdivisions[rope];
                var pointCount = settings.totalBoneCounts[rope] + 1;
                for (int p = 0; p < pointCount; p++)
                {
                    var globalPointId = startPoint + p;

                    for (int i = 0; i < ropeSubdivisions; i++)
                    {
                        if (p == pointCount - 1 && i > 0)
                        {
                            // Last point does not contain any subdivisions
                            break;
                        }

                        var currentIndex = previousSubdivisions + p * ropeSubdivisions + i;

                        var hit = collisionResults[currentIndex];
                        if (hit.distance > 0)
                        {
                            previousCollisionData[currentIndex] = new CollisionData(globalPointId, i, hit.point, hit.normal);
                        }
                        else
                        {
                            previousCollisionData[currentIndex] = CollisionData.Empty();
                        }
                    }
                }
                previousSubdivisions += ropeSubdivisions * settings.activeBoneCounts[rope] + 1;
                startPoint += pointCount;
            }

#if ROPE_DEBUGGING
        foreach (var collision in previousCollisionData)
        {
            Debug.DrawRay(collision.position, collision.normal, Color.magenta);
        }
#endif
        }
    }

    [Unity.Burst.BurstCompile]
    struct WriteCollisionConstraintsJob : IJob
    {
        [ReadOnly]
        public NativeArray<CollisionConstraints> collisionConstraints;
        [ReadOnly]
        public RopeData ropeData;
        [WriteOnly]
        public NativeArray<CollisionConstraints> mainCollisionConstraints;
        [WriteOnly]
        public NativeArray<CollisionConstraints> secondaryCollisionConstraints;

        public void Execute()
        {
            int mainCount = 0;
            int secondaryCount = 0;
            int startIndex = 0;
            for(int targetRope = 0; targetRope < ropeData.ropeLengths.Length; targetRope++)
            {
                var maxConstraintsForRope = ropeData.totalBoneCounts[targetRope] * ropeData.subdivisions[targetRope] + 1;
                for(int i = startIndex; i < startIndex + maxConstraintsForRope; i++)
                {
                    var constraint = collisionConstraints[i];
                    if (constraint.point1 != -1)
                    {
                        if (constraint.subdivision1 == 0)
                        {
                            mainCollisionConstraints[mainCount] = constraint;
                            mainCount++;
                        }
                        else
                        {
                            secondaryCollisionConstraints[secondaryCount] = constraint;
                            secondaryCount++;
                        }
                    }
                    else
                    {
                        // No more constraints for this rope
                        break;
                    }
                }
                startIndex += maxConstraintsForRope;
            }

            mainCollisionConstraints[mainCount] = CollisionConstraints.CreateEmpty();
            secondaryCollisionConstraints[secondaryCount] = CollisionConstraints.CreateEmpty();
        }
    }

    [Unity.Burst.BurstCompile]
    struct PrepareForCollisionDetectionJob : IJob
    {
        [ReadOnly]
        NativeArray<float3> x;
        [ReadOnly]
        NativeArray<float3> oldX;
        [ReadOnly]
        RopeData settings;
        NativeArray<CollisionData> previousCollisionData;
        [ReadOnly]
        int collisionLayers;
        [WriteOnly]
        NativeArray<SpherecastCommand> spherecastCommands;

        public PrepareForCollisionDetectionJob(NativeArray<float3> x, NativeArray<float3> oldX, RopeData settings, NativeArray<CollisionData> previousCollisionData,
            int collisionLayers, NativeArray<SpherecastCommand> spherecastCommands)
        {
            this.x = x;
            this.oldX = oldX;
            this.settings = settings;
            this.previousCollisionData = previousCollisionData;
            this.collisionLayers = collisionLayers;
            this.spherecastCommands = spherecastCommands;
        }

        public void Execute()
        {
            UpdatePreviousCollisionData();
            WriteSpherecastCommands();
        }

        void UpdatePreviousCollisionData()
        {
            for (int i = 0; i < previousCollisionData.Length; i++)
            {
                var collision = previousCollisionData[i];
                var targetPoint = collision.point;
                if (targetPoint < 0)
                {
                    continue;
                }
                var pointPosition = oldX[targetPoint];
                var ropeDirection = float3.zero;
                var collisionRope = settings.GetRopeIndexForPoint(targetPoint);
                var collisionRopeRadius = settings.ropeRadii[collisionRope];

                bool isLastPoint = settings.IsEndPoint(settings.GetRopeIndexForPoint(targetPoint), targetPoint);

                if (!isLastPoint)
                {
                    ropeDirection = oldX[targetPoint + 1] - oldX[targetPoint];
                }

                var progress = settings.GetProgress(collisionRope, collision.subdivision);
                var actualPosition = pointPosition + ropeDirection * progress;
                var diff = collision.position - actualPosition;
                if (math.lengthsq(diff) > collisionRopeRadius * collisionRopeRadius * 2)
                {
                    previousCollisionData[i] = CollisionData.Empty();
                }
            }
        }

        void WriteSpherecastCommands()
        {
            int startPoint = 0;
            int previousSubdivisions = 0;

            for (int rope = 0; rope < settings.activeBoneCounts.Length; rope++)
            {
                var boneCount = settings.activeBoneCounts[rope];
                var castOffset = settings.ropeRadii[rope];
                var subdivisions = settings.subdivisions[rope];

                for (int i = 0; i < boneCount; i++)
                {
                    var currentPoint = startPoint + i;
                    var startDirection = oldX[currentPoint + 1] - oldX[currentPoint];
                    var endDirection = x[currentPoint + 1] - x[currentPoint];

                    for (int x = 0; x < subdivisions; x++)
                    {
                        var progress = settings.GetProgress(rope, x);
                        var commandId = previousSubdivisions + i * subdivisions + x;
                        var collision = previousCollisionData[commandId];
                        if (collision.point >= 0)
                        {
                            spherecastCommands[commandId] = CreateSpherecastCommandChecker(currentPoint, progress, startDirection, castOffset, collision.position);
                        }
                        else
                        {
                            spherecastCommands[commandId] = CreateSpherecastCommand(currentPoint, progress, startDirection, endDirection, castOffset);
                        }
                    }
                }

                previousSubdivisions += boneCount * subdivisions + 1;
                var lastPoint = startPoint + boneCount;
                var lastCollision = previousCollisionData[previousSubdivisions - 1];
                if (lastCollision.point >= 0)
                {
                    spherecastCommands[previousSubdivisions - 1] = CreateSpherecastCommandChecker(lastPoint, 0f, float3.zero, castOffset, lastCollision.position);
                }
                else
                {
                    spherecastCommands[previousSubdivisions - 1] = CreateSpherecastCommand(lastPoint, 0f, float3.zero, float3.zero, castOffset);
                }
                startPoint += boneCount + 1;
            }
        }

        SpherecastCommand CreateSpherecastCommand(int targetPoint, float progress, float3 startDirection, float3 endDirection, float castOffset)
        {
            var targetRope = settings.GetRopeIndexForPoint(targetPoint);
            var castStart = oldX[targetPoint] + startDirection * progress;
            var castEnd = x[targetPoint] + endDirection * progress;
            var movementDiff = castEnd - castStart;
            var movementDir = math.normalize(movementDiff);
            var castDistance = math.length(movementDiff) + castOffset;
            return new SpherecastCommand(castStart - movementDir * castOffset, settings.ropeRadii[targetRope], movementDir, castDistance, collisionLayers);
        }

        SpherecastCommand CreateSpherecastCommandChecker(int targetPoint, float progress, float3 startDirection, float castOffset, float3 positionToCheck)
        {
            var targetRope = settings.GetRopeIndexForPoint(targetPoint);
            var castStart = oldX[targetPoint] + startDirection * progress;

            var movementDiff = positionToCheck - castStart;
            var movementDir = math.normalize(movementDiff);
            var castDistance = math.length(movementDiff) + castOffset;
            return new SpherecastCommand(castStart - movementDir * castOffset, settings.ropeRadii[targetRope], movementDir, castDistance, collisionLayers);
        }
    }

    [Unity.Burst.BurstCompile]
    struct ConstrainTwistJob : IJob
    {
        [ReadOnly]
        NativeArray<int> ids;
        [ReadOnly]
        RopeData settings;

        public ConstrainTwistJob(NativeArray<int> ids, RopeData settings)
        {
            this.ids = ids;
            this.settings = settings;
        }

        public void Execute()
        {
            int firstPoint = 0;
            for (int rope = 0; rope < settings.ropeEndPoints.Length; rope++)
            {
                var twistLimit = settings.twistLimits[rope];
                if (twistLimit >= 0)
                {
                    // Move from first point to last
                    // Start from the 2nd point
                    for (int i = firstPoint + 1; i < settings.ropeEndPoints[rope] - 1; i++)
                    {
                        ConstrainTwist(i, i - 1, twistLimit);
                    }

                    // Move from last point to first
                    // Start from the 2nd point
                    for (int i = settings.ropeEndPoints[rope] - 2; i >= firstPoint; i--)
                    {
                        ConstrainTwist(i, i + 1, twistLimit);
                    }
                }
                firstPoint = settings.ropeEndPoints[rope];
            }
        }

        void ConstrainTwist(int targetPoint, int relativeToPoint, float twistLimit)
        {
            ref var body = ref World.main.GetBody(ids[targetPoint]);
            var prevBody = World.main.GetBody(ids[relativeToPoint]);

            var prevRot = prevBody.x.rot;
            var prevAxis = new float3(0, 0, 1);

            var rotDiff = math.mul(math.inverse(prevRot), body.x.rot);
            re.ToSwingTwist(rotDiff, prevAxis, out var swing, out var twist);
            twist = math.normalize(twist);
            twist.ToAngleAxis(out var twistAngle, out var twistAxis);
            twistAngle = re.NormalizeAngle(twistAngle);
            twistAngle = math.clamp(math.degrees(twistAngle), -twistLimit, twistLimit);
            twist = quaternion.AxisAngle(twistAxis, math.radians(twistAngle));

            rotDiff = math.mul(swing, twist);

            var targetRot = math.mul(prevRot, rotDiff);
            var deltaRot = math.mul(body.x.rot, math.inverse(targetRot));
            var angularVel = -(float3)Math3d.QuaternionToAngleAxis(deltaRot);
            var currentVel = World.main.GetVelocity(ids[targetPoint]);
            currentVel.angular = currentVel.angular + (angularVel / World.main.dt);
            World.main.SetVelocity(ids[targetPoint], currentVel);
        }
    }

    [Unity.Burst.BurstCompile]
    struct ReadDataForJob : IJobParallelFor
    {
        [ReadOnly]
        NativeArray<int> ids;
        [ReadOnly]
        RopeData settings;

        [WriteOnly]
        NativeArray<float3> x;
        [WriteOnly]
        NativeArray<float3> oldX;

        [WriteOnly]
        // Only 2 writes per rope for this array. Could split into a different job, but not sure if worth it.
        NativeParallelHashMap<int, RigidTransform>.ParallelWriter connectedBodyPositions;
        [WriteOnly]
        NativeParallelHashMap<int, RigidTransform>.ParallelWriter originalBodyPositions;

        readonly static float3 forward = new float3(0, 0, 1);

        public ReadDataForJob(NativeArray<int> ids, RopeData settings, NativeArray<float3> x, NativeArray<float3> oldX,
            NativeParallelHashMap<int, RigidTransform> connectedBodyPositions, NativeParallelHashMap<int, RigidTransform> originalBodyPositions)
        {
            this.ids = ids;
            this.settings = settings;
            this.x = x;
            this.oldX = oldX;
            this.connectedBodyPositions = connectedBodyPositions.AsParallelWriter();
            this.originalBodyPositions = originalBodyPositions.AsParallelWriter();
        }

        // This parallel job potentially writes the same data to the same array positions
        public void Execute(int index)
        {
            var rope = settings.GetRopeIndexForPoint(index);
            var startPoint = settings.GetStartPoint(rope);

            // Don't read data of disabled bones
            if (index < startPoint)
                return;

            bool isStartPoint = index == startPoint;
            bool isEndPoint = settings.IsEndPoint(rope, index);

            int boneCount = 0;
            float3 p0, p1, v0, v1;
            p0 = p1 = v0 = v1 = float3.zero;

            // First point taken from the previous bone
            if (!isStartPoint)
            {
                var previousIndex = index - 1;
                var id = ids[previousIndex];
                var firstBody = World.main.GetBody(id);
                var localPos = forward * settings.segmentLengths[previousIndex] / 2;
                GetLocalPointPositionAndVelocity(firstBody, id, localPos, out p0, out v0);
                boneCount++;
            }

            // Second point taken from the current bone
            if (!isEndPoint)
            {
                var id = ids[index];
                var secondBody = World.main.GetBody(id);
                // We skip the last points for each rope
                originalBodyPositions.TryAdd(id, secondBody.x);
                var localPos = -forward * settings.segmentLengths[index] / 2;
                GetLocalPointPositionAndVelocity(secondBody, id, localPos, out p1, out v1);
                boneCount++;
            }

            if (boneCount == 0)
            {
                oldX[index] = float3.zero;
                x[index] = float3.zero;
            }

            if (isStartPoint)
            {
                var bodyIndex = rope * 2;
                ReadConnectedBody(bodyIndex);
            }

            if (isEndPoint)
            {
                var bodyIndex = rope * 2 + 1;
                ReadConnectedBody(bodyIndex);
            }

            var pointPosition = (p0 + p1) / boneCount;
            var pointVelocity = (v0 + v1) / boneCount * World.main.dt;
            oldX[index] = pointPosition - pointVelocity;
            x[index] = pointPosition;
        }

        public void GetLocalPointPositionAndVelocity(Body body, int id, float3 localPoint, out float3 position, out float3 velocity)
        {
            var rTransform = body.x;
            position = math.transform(rTransform, localPoint);
            velocity = World.main.GetLocalPointVelocity(id, localPoint).linear;
        }

        void ReadConnectedBody(int bodyIndex)
        {
            var bodyId = settings.connectedBodies[bodyIndex];
            if (bodyId != World.environmentId)
            {
                var currentPos = World.main.GetBodyPosition(bodyId);
                connectedBodyPositions.TryAdd(bodyId, currentPos);
                originalBodyPositions.TryAdd(bodyId, currentPos);
            }
        }
    }

    [Unity.Burst.BurstCompile]
    struct WriteDataJob : IJobParallelFor
    {
        [ReadOnly]
        NativeArray<int> ids;
        [ReadOnly]
        RopeData settings;
        [ReadOnly]
        NativeArray<float3> x;
        [ReadOnly]
        NativeArray<float3> oldX;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        NativeParallelHashMap<int, RigidTransform> connectedBodyPositions;

        public WriteDataJob(NativeArray<int> ids, RopeData settings, NativeArray<float3> x, NativeArray<float3> oldX,
            NativeParallelHashMap<int, RigidTransform> connectedBodyPositions)
        {
            this.ids = ids;
            this.settings = settings;
            this.x = x;
            this.oldX = oldX;
            this.connectedBodyPositions = connectedBodyPositions;
        }

        public void Execute(int index)
        {
            float dt = World.main.dt;
            int rope = settings.GetRopeIndexForPoint(index);
            int startPoint = settings.GetStartPoint(rope);

            if (index == 0)
            {
                var e = connectedBodyPositions.GetEnumerator();
                while (e.MoveNext())
                {
                    WriteConnectedBodyData(e.Current.Key);
                }
                
            }

            // Don't write data for disabled bones
            if (index < startPoint)
                return;


            if (settings.IsEndPoint(rope, index))
            {
                return;
            }

            var axis = math.normalize(oldX[index + 1] - oldX[index]);

            var v0 = (x[index] - oldX[index]) / dt;
            var v1 = (x[index + 1] - oldX[index + 1]) / dt;

            var id = ids[index];

            float3 previousCenter = (oldX[index] + oldX[index + 1]) / 2f;
            var wantedCenter = (x[index + 1] + x[index]) / 2f;

            var linearVelocity = (wantedCenter - previousCenter) / dt;
            var angularVelocity = math.cross((v0 - v1), axis) / math.clamp(settings.segmentLengths[index], settings.ropeRadii[rope] * 2f, float.MaxValue);

            World.main.SetVelocity4(id, new Velocity4(angularVelocity, linearVelocity));

            var currentPos = World.main.GetBodyPosition(id);

            currentPos.pos = wantedCenter;
            var targetRot = quaternion.LookRotation(math.normalize(x[index + 1] - x[index]), new float3(0, 1, 0));
            currentPos.rot = re.SwingTo(currentPos.rot, targetRot, new float3(0, 0, 1));
            World.main.SetBodyPosition(id, currentPos);
            World.main.UpdateInertia(id);
        }

        void WriteConnectedBodyData(int bodyId)
        {
            if (bodyId != World.environmentId)
            {
                var position = connectedBodyPositions[bodyId];

                var currentVel = World.main.GetVelocity(bodyId);
                var oldPosition = re.Integrate(World.main.GetBodyPosition(bodyId), currentVel, -World.main.dt);

                var positionLinearDelta = position.pos - oldPosition.pos;
                var positionAngularDelta = math.normalize(math.mul(position.rot, math.inverse(oldPosition.rot)));
                currentVel.linear = positionLinearDelta / World.main.dt;
                currentVel.angular = positionAngularDelta.ToAngleAxis() / World.main.dt;

                World.main.SetBodyPosition(bodyId, position);
                World.main.SetVelocity(bodyId, currentVel);
                World.main.UpdateInertia(bodyId);
            }
        }
    }

    [Unity.Burst.BurstCompile]
    struct ResetPositionJob : IJob
    {
        [ReadOnly]
        NativeParallelHashMap<int,RigidTransform> originalBodyPositions;

        public ResetPositionJob(NativeParallelHashMap<int, RigidTransform> originalBodyPositions)
        {
            this.originalBodyPositions = originalBodyPositions;
        }

        public void Execute()
        {
            var e = originalBodyPositions.GetEnumerator();
            while(e.MoveNext())
            {
                ResetPositionAndCompensateVelocity(e.Current.Key, e.Current.Value);
            }
        }

        void ResetPositionAndCompensateVelocity(int bodyId, RigidTransform originalPos)
        {
            if (bodyId != World.environmentId)
            {
                ref Body body = ref World.main.GetBody(bodyId);
                var currentPos = body.x;
                var posDiff = currentPos.pos - originalPos.pos;
                var positionAngularDelta = math.normalize(math.mul(currentPos.rot, math.inverse(originalPos.rot)));
                var dt = World.main.dt;
                var velocity = body.v;
                velocity.linear += posDiff / dt;
                velocity.angular += positionAngularDelta.ToAngleAxis() / dt;
                body.v = velocity;
                body.x = originalPos;
                World.main.UpdateInertia(bodyId);
            }
        }
    }
}