using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Recoil;

namespace NBG.XPBDRope
{
    [BurstCompile]
    public struct XpbdRopeSolver : IJob
    {
        // pass endpoint inertias
        public int iterations;
        public int count;
        public float dt;
        public const int solveSplits = 50;
        // particles
        [ReadOnly]
        RopeData settings;

        public NativeArray<float3> oldX;
        public NativeArray<float3> x;
        public NativeArray<float> lambdaStretch;
        // Indexed by body ID to correctly handle duplicate entries
        public NativeParallelHashMap<int, RigidTransform> connectedBodyPositions;
        public NativeArray<float> lambdaConnectedStretch;
        public NativeArray<float> lambdaBend;
        public Unity.Profiling.ProfilerMarker iterationMarker;

        [ReadOnly]
        public NativeArray<CollisionConstraints> mainCollisionConstraints;
        [ReadOnly]
        public NativeArray<CollisionConstraints> secondaryCollisionConstraints;

        public bool IsCreated { get; private set; }

        internal static XpbdRopeSolver Createjob(RopeData ropeSettings, float dt, int iterations)
        {
            return new XpbdRopeSolver()
            {
                dt = dt,
                settings = ropeSettings,
                count = ropeSettings.pointCount,
                iterations = iterations,
                oldX = new NativeArray<float3>(ropeSettings.pointCount, Allocator.Persistent),
                x = new NativeArray<float3>(ropeSettings.pointCount, Allocator.Persistent),
                connectedBodyPositions = new NativeParallelHashMap<int, RigidTransform>(ropeSettings.connectedBodies.Length, Allocator.Persistent),
                // These could be smaller, but would require to offset the index by the rope number, which is less readable
                lambdaStretch = new NativeArray<float>(ropeSettings.pointCount - 1, Allocator.Persistent),
                lambdaConnectedStretch = new NativeArray<float>(ropeSettings.connectedBodies.Length, Allocator.Persistent),
                lambdaBend = new NativeArray<float>(ropeSettings.pointCount - 2, Allocator.Persistent),
                mainCollisionConstraints = new NativeArray<CollisionConstraints>(ropeSettings.pointCount * 16, Allocator.Persistent),
                secondaryCollisionConstraints = new NativeArray<CollisionConstraints>(ropeSettings.GetInnerSubdivisionCount() * 16, Allocator.Persistent),
                IsCreated = true
            };
        }

        public void Dispose()
        {
            oldX.Dispose();
            x.Dispose();
            connectedBodyPositions.Dispose();
            lambdaStretch.Dispose();
            lambdaConnectedStretch.Dispose();
            lambdaBend.Dispose();
            mainCollisionConstraints.Dispose();
            secondaryCollisionConstraints.Dispose();
            count = 0;
            IsCreated = false;
        }

        public void Execute()
        {
            // constraints, lambda init
            for (var c = 0; c < count - 1; c++)
                lambdaStretch[c] = 0;
            for (var c = 0; c < settings.connectedBodies.Length; c++)
                lambdaConnectedStretch[c] = 0;
            for (var c = 0; c < count - 2; c++)
                lambdaBend[c] = 0;

            for (var i = 0; i < iterations; i++)
            {
                iterationMarker.Begin();
                for (int rope = 0; rope < settings.ropeEndPoints.Length; rope++)
                {
                    var elasticCompliance = settings.elasticCompliance[rope];
                    var maxSeparation = settings.maxSegmentSeparation[rope];
                    var bendLimit = settings.bendLimit[rope];
                    var startPoint = settings.GetStartPoint(rope);
                    var endPoint = settings.ropeEndPoints[rope];

                    for (var c = startPoint; c < endPoint - 1; c++)
                        SolveStretch(c, c + 1, dt, elasticCompliance, maxSeparation);

                    SolveConnectedStretch(rope * 2, startPoint, dt, elasticCompliance, maxSeparation);
                    SolveConnectedStretch(rope * 2 + 1, endPoint - 1, dt, elasticCompliance, maxSeparation);

                    for (var c = startPoint; c < endPoint - 2; c++)
                        SolveBend(c, c + 1, c + 2, dt, settings.bendCompliance[rope], bendLimit);
                }

                SolveCollisionConstraints();

                for (int rope = 0; rope < settings.ropeEndPoints.Length; rope++)
                {
                    var elasticCompliance = settings.elasticCompliance[rope];
                    var maxSeparation = settings.maxSegmentSeparation[rope];
                    var bendLimit = settings.bendLimit[rope];
                    var startPoint = settings.GetStartPoint(rope);
                    var endPoint = settings.ropeEndPoints[rope];

                    for (var c = endPoint - 2; c >= startPoint; c--)
                        SolveStretch(c, c + 1, dt, elasticCompliance, maxSeparation);

                    SolveConnectedStretch(rope * 2, startPoint, dt, elasticCompliance, maxSeparation);
                    SolveConnectedStretch(rope * 2 + 1, endPoint - 1, dt, elasticCompliance, maxSeparation);

                    for (var c = endPoint - 3; c >= startPoint; c--)
                        SolveBend(c, c + 1, c + 2, dt, settings.bendCompliance[rope], bendLimit);
                }

                SolveCollisionConstraints();
                iterationMarker.End();
            }
        }

        void SolveStretch(int idx1, int idx2, float dt, float compliance, float maxSeparation)
        {
            float3 x1 = x[idx1];
            float3 x2 = x[idx2];
            var lambda = lambdaStretch[idx1];

            XpbdConstraints.Solve(ref x1, settings.segmentInvMass[idx1], ref x2, settings.segmentInvMass[idx2], settings.segmentLengths[idx1],
                ref lambda, compliance, dt, maxSeparation);

            x[idx1] = x1;
            x[idx2] = x2;
            lambdaStretch[idx1] = lambda;
        }

        void SolveConnectedStretch(int bodyIndex, int pointIndex, float dt, float compliance, float maxSeparation)
        {
            var bodyId = settings.connectedBodies[bodyIndex];
            if (bodyId != World.environmentId)
            {
                float3 x1 = x[pointIndex];
                var bodyTransform = connectedBodyPositions[bodyId];
                var targetBody = World.main.GetBody(bodyId);
                var anchor = settings.connectedBodyAnchor[bodyIndex];
                var lambda = lambdaConnectedStretch[bodyIndex];
                var tensorRot = math.mul(bodyTransform.rot, World.main.PhysXInertiaTensorRotation(bodyId));
                var bodyI = RigidBodyInertia.CalculatIFromTensor(tensorRot, World.main.PhysXInertiaTensor(bodyId));
                var bodyInvI = re.inverse(bodyI);
                XpbdConstraints.SolveAttachedBody(ref x1, settings.segmentInvMass[pointIndex], ref bodyTransform, targetBody.invM, bodyInvI, anchor,
                    ref lambda, compliance, dt, maxSeparation);
                x[pointIndex] = x1;
                connectedBodyPositions[bodyId] = bodyTransform;
                lambdaConnectedStretch[bodyIndex] = lambda;
            }
        }

        void SolveBend(int idx1, int idx2, int idx3, float dt, float compliance, float bendLimit)
        {
            float3 x1 = x[idx1];
            float3 x2 = x[idx2];
            float3 x3 = x[idx3];
            var lambda = lambdaBend[idx1];
            var complianceBend = compliance; //0.01f;// 0.000000000001f;// 0.0001f;
            var angle_limit = math.radians(bendLimit); // 60 degrees

            XpbdConstraints.SolveBend(ref x1, settings.segmentInvMass[idx1], ref x2, settings.segmentInvMass[idx2], ref x3, settings.segmentInvMass[idx3], ref lambda, complianceBend, angle_limit, dt);

            x[idx1] = x1;
            x[idx2] = x2;
            x[idx3] = x3;
            lambdaBend[idx1] = lambda;
        }

        void SolveCollisionConstraints()
        {
            for (int i = 0; i < mainCollisionConstraints.Length; i++)
            {
                var target = mainCollisionConstraints[i];
                if (target.point1 == -1)
                {
                    // No more collision constraints left.
                    break;
                }
                SolveCollisionConstraint(target);
            }

            for (int i = 0; i < secondaryCollisionConstraints.Length; i++)
            {
                var target = secondaryCollisionConstraints[i];
                if (target.point1 == -1)
                {
                    break;
                }
                SolveCollisionConstraint(target);
            }
        }

        void SolveCollisionConstraint(CollisionConstraints collisionConstraint)
        {
            var targetPoint = collisionConstraint.point1;
            var targetRope = settings.GetRopeIndexForPoint(targetPoint);
            var targetProgress = settings.GetProgress(targetRope, collisionConstraint.subdivision1);
            var targetInvMass = GetInvMass(targetPoint);

            var otherPoint = collisionConstraint.point2;
            var otherRope = settings.GetRopeIndexForPoint(otherPoint);
            var otherProgress = settings.GetProgress(otherRope, collisionConstraint.subdivision2);
            var otherInvMass = GetInvMass(otherPoint);

            float targetDisplacement = 0;
            float otherDisplacement = 0;
            {
                var targetEndPos = GetEndPoint(targetRope, targetPoint, targetProgress);
                var otherEndPos = collisionConstraint.collisionPoint;
                if (otherPoint >= 0)
                {
                    otherEndPos = GetEndPoint(otherRope, otherPoint, otherProgress);
                }
                var diff = targetEndPos - otherEndPos;

                var normal = collisionConstraint.normal;
                var minDistance = collisionConstraint.minDistance;

                var constraint = math.dot(diff, normal) - minDistance;

                if (constraint < 0)
                {
                    var massSum = targetInvMass + otherInvMass;
                    var s = constraint / massSum;

                    var targetDelta = -s * targetInvMass * normal;
                    var otherDelta = -s * otherInvMass * -normal;

                    targetDisplacement = math.length(targetDelta);
                    otherDisplacement = math.length(otherDelta);

                    MovePointsBasedOnSubdivisionDelta(targetRope, targetPoint, targetProgress, targetDelta);

                    if (otherPoint >= 0)
                    {
                        MovePointsBasedOnSubdivisionDelta(otherRope, otherPoint, otherProgress, otherDelta);
                    }
                }
                else
                {
                    return;
                }
            }

            // Friction
            {
                var targetStartPos = GetStartPoint(targetRope, targetPoint, targetProgress);
                var targetEndPos = GetEndPoint(targetRope, targetPoint, targetProgress);

                var staticFrict = settings.staticFriction[targetRope];
                var dynamicFrict = settings.dynamicFriction[targetRope];

                var otherStartPos = collisionConstraint.collisionPoint;
                var otherEndPos = collisionConstraint.collisionPoint;
                if (otherPoint >= 0)
                {
                    otherStartPos = GetStartPoint(otherRope, otherPoint, otherProgress);
                    otherEndPos = GetEndPoint(otherRope, otherPoint, otherProgress);
                    staticFrict += settings.staticFriction[otherRope];
                    dynamicFrict += settings.dynamicFriction[otherRope];
                    staticFrict /= 2f;
                    dynamicFrict /= 2f;
                }
                var diff = targetEndPos - otherEndPos;

                var normal = collisionConstraint.normal;
                if (otherPoint >= 0)
                {
                    normal = diff / math.length(diff);
                }


                var targetDiff = targetEndPos - targetStartPos;
                var otherDiff = otherEndPos - otherStartPos;
                var deltaX = math.cross(targetDiff - otherDiff, normal);
                var deltaXLength = math.length(deltaX);

                var massSum = targetInvMass + otherInvMass;
                var targetMassMult = targetInvMass / massSum;
                var otherMassMult = otherInvMass / massSum;

                var targetDelta = deltaX * targetMassMult;
                var otherDelta = -deltaX * otherMassMult;

                if (deltaXLength > staticFrict * targetDisplacement)
                {
                    targetDelta *= math.min(dynamicFrict * targetDisplacement / deltaXLength, 1f);
                }

                if (deltaXLength > staticFrict * otherDisplacement)
                {
                    otherDelta *= math.min(dynamicFrict * otherDisplacement / deltaXLength, 1f);
                }

                targetDelta = math.cross(targetDelta, normal);
                otherDelta = math.cross(otherDelta, normal);

                MovePointsBasedOnSubdivisionDelta(targetRope, targetPoint, targetProgress, targetDelta);

                if (otherPoint >= 0)
                {
                    MovePointsBasedOnSubdivisionDelta(otherRope, otherPoint, otherProgress, otherDelta);
                }
            }
        }

        float GetInvMass(int targetPoint)
        {
            if (targetPoint < 0)
            {
                // infinitely heavy, can't move
                return 0;
            }
            else
            {
                return settings.segmentInvMass[targetPoint];
            }
        }

        float3 GetStartPoint(int targetRope, int targetPoint, float progress)
        {
            var targetStart = oldX[targetPoint];
            var targetEnd = targetStart;
            if (!settings.IsEndPoint(targetRope, targetPoint))
            {
                targetEnd = oldX[targetPoint + 1];
            }
            return math.lerp(targetStart, targetEnd, progress);
        }

        float3 GetEndPoint(int targetRope, int targetPoint, float progress)
        {
            var targetStart = x[targetPoint];
            var targetEnd = targetStart;
            if (!settings.IsEndPoint(targetRope, targetPoint))
            {
                targetEnd = x[targetPoint + 1];
            }
            return math.lerp(targetStart, targetEnd, progress);
        }

        void MovePointsBasedOnSubdivisionDelta(int targetRope, int target, float subdivisionProgress, float3 delta)
        {
            if (math.lengthsq(delta) > 0f)
            {
                var adjustedPosition1 = x[target];
                var adjustedPosition2 = float3.zero;
                var targetIsAnEndPoint = settings.IsEndPoint(targetRope, target);
                if (!targetIsAnEndPoint)
                {
                    adjustedPosition2 = x[target + 1];
                }

                MoveCapsuleAccordingToPoint(ref adjustedPosition1, ref adjustedPosition2, subdivisionProgress, delta);

                x[target] = adjustedPosition1;
                if (!targetIsAnEndPoint)
                {
                    x[target + 1] = adjustedPosition2;
                }
            }
        }

        void MoveStartPointsBasedOnSubdivisionDelta(int targetRope, int target, float subdivisionProgress, float3 delta)
        {
            if (math.lengthsq(delta) > 0f)
            {
                var adjustedPosition1 = oldX[target];
                var adjustedPosition2 = float3.zero;
                var targetIsAnEndPoint = settings.IsEndPoint(targetRope, target);
                if (!targetIsAnEndPoint)
                {
                    adjustedPosition2 = oldX[target + 1];
                }
                MoveCapsuleAccordingToPoint(ref adjustedPosition1, ref adjustedPosition2, subdivisionProgress, delta);
                oldX[target] = adjustedPosition1;
                if (!targetIsAnEndPoint)
                {
                    oldX[target + 1] = adjustedPosition2;
                }
            }
        }

        UnityEngine.Color GetColor(int id)
        {
            id %= 6;
            switch (id)
            {
                case 0:
                    return UnityEngine.Color.red;
                case 1:
                    return UnityEngine.Color.yellow;
                case 2:
                    return UnityEngine.Color.green;
                case 3:
                    return UnityEngine.Color.cyan;
                case 4:
                    return UnityEngine.Color.blue;
                case 5:
                    return UnityEngine.Color.magenta;
            }

            return UnityEngine.Color.green;
        }

        public static void MoveCapsuleAccordingToPoint(ref float3 p1, ref float3 p2, float pointPositionRelativeToCapsule, float3 pointDelta)
        {
            // If we're moving the capsule edge, it will move the full delta distance. If we're moving the middle, it will only move half way
            // This creates stable knots. 
            // If we always moved both points the delta amount, we would get spinning.
            pointDelta /= 2f;

            // from 0 to 0.5 coef goes from 2 to 1. Then lerp to 0
            float p1MovementCoef = math.lerp(0f, 1f, math.clamp((1f - pointPositionRelativeToCapsule) * 2f, 0f, 2f));
            // from 1 to 0.5 coef goes from 2 to 1. Then lerp to 0
            float p2MovementCoef = math.lerp(0f, 1f, math.clamp(pointPositionRelativeToCapsule * 2f, 0f, 2f));

            var movementDir = math.normalizesafe(pointDelta, float3.zero);
            var capsuleDir = math.normalizesafe(p2 - p1, float3.zero);
            var rotationAmount = math.length(math.cross(capsuleDir, movementDir));

            var movementAmount = 1f - rotationAmount;

            p1MovementCoef = math.lerp(p1MovementCoef, 1f, movementAmount);
            p2MovementCoef = math.lerp(p2MovementCoef, 1f, movementAmount);

            p1 += pointDelta * p1MovementCoef;
            p2 += pointDelta * p2MovementCoef;
        }

        public static float DetectTwoMovingSphereCollisionTime(float3 xBase, float3 xDiff, float3 yBase, float3 yDiff, float extraC)
        {
            var tSquaredVector = xDiff * xDiff - 2f * xDiff * yDiff + yDiff * yDiff;
            var tSquared = tSquaredVector.x + tSquaredVector.y + tSquaredVector.z;

            var tBaseVector = 2f * xBase * xDiff - 2f * xBase * yDiff - 2f * xDiff * yBase + 2f * yBase * yDiff;
            var tBase = tBaseVector.x + tBaseVector.y + tBaseVector.z;

            var cVector = xBase * xBase - 2f * xBase * yBase + yBase * yBase;
            var c = cVector.x + cVector.y + cVector.z;

            var extraCSquared = extraC * extraC;
            c -= extraCSquared;

            var discriminant = tBase * tBase - 4f * tSquared * c;
            if (discriminant < 0 || tSquared == 0 || tBase == 0)
            {
                return float.MaxValue;
            }

            var sqrtDisc = math.sqrt(discriminant);

            var solution1 = (-tBase + sqrtDisc) / (2f * tSquared);
            var solution2 = (-tBase - sqrtDisc) / (2f * tSquared);

            var final = 0f;

            if (solution1 > 0 && solution2 > 0)
            {
                final = math.min(solution1, solution2);
            }
            else
            {
                final = math.max(solution1, solution2);
            }

            return final;
        }
    }
}
