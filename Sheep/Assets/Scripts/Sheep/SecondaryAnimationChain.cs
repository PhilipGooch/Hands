#define SEPARATEAPPLY
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public class SecondaryAnimationChain : MonoBehaviour
{
    public Transform[] bodies;
    public float[] springs;
    public float[] dampers;
    public float[] drags;
    public float[] gravities;
    public Vector3[] rotationConstraints;
    NativeArray<quaternion> localRotations;
    NativeArray<float3> positions;
    NativeArray<quaternion> parentRotations;
    NativeArray<float3> localPositions;
    NativeArray<float3> velocities;

    NativeArray<float> springArray;
    NativeArray<float> damperArray;
    NativeArray<float> dragArray;
    NativeArray<float> gravityArray;
    NativeArray<float3> constraintArray;

    AnimationChainJob job;

    void Awake()
    {
        positions = new NativeArray<float3>(bodies.Length, Allocator.Persistent);
        velocities = new NativeArray<float3>(bodies.Length, Allocator.Persistent);
        parentRotations = new NativeArray<quaternion>(bodies.Length, Allocator.Persistent);
        localRotations = new NativeArray<quaternion>(bodies.Length, Allocator.Persistent);
        localPositions = new NativeArray<float3>(bodies.Length, Allocator.Persistent);

        springArray = new NativeArray<float>(springs.Length, Allocator.Persistent);
        damperArray = new NativeArray<float>(dampers.Length, Allocator.Persistent);
        dragArray = new NativeArray<float>(drags.Length, Allocator.Persistent);
        gravityArray = new NativeArray<float>(gravities.Length, Allocator.Persistent);
        constraintArray = new NativeArray<float3>(rotationConstraints.Length, Allocator.Persistent);

        for (int i = 0; i < bodies.Length; i++)
        {
            localRotations[i] = bodies[i].localRotation;
            localPositions[i] = bodies[i].localPosition;
        }

        for (int i = 0; i < springs.Length; i++)
        {
            springArray[i] = springs[i];
            damperArray[i] = dampers[i];
            dragArray[i] = drags[i];
            gravityArray[i] = gravities[i];
        }

        for(int i = 0; i < rotationConstraints.Length; i++)
        {
            constraintArray[i] = rotationConstraints[i];
        }

        job = new AnimationChainJob
        {
            positions = positions,
            localPositions = localPositions,
            parentRotations = parentRotations,
            localRotations = localRotations,
            velocities = velocities,
            springs = springArray,
            dampers = damperArray,
            drags = dragArray,
            gravities = gravityArray,
            constraints = constraintArray
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        Restart();
    }

    void OnDestroy()
    {
        positions.Dispose();
        velocities.Dispose();
        parentRotations.Dispose();
        localRotations.Dispose();
        localPositions.Dispose();
        springArray.Dispose();
        damperArray.Dispose();
        dragArray.Dispose();
        gravityArray.Dispose();
        constraintArray.Dispose();
    }

    void OnEnable()
    {
        SecondaryAnimationChainSystem.Instance?.AddChain(this);
    }

    void OnDisable()
    {
        SecondaryAnimationChainSystem.Instance?.RemoveChain(this);
    }

    public void Restart()
    {
        if (!positions.IsCreated)
        {
            Start();
            return;
        }
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].localRotation = localRotations[i];
            bodies[i].localPosition = localPositions[i];
            positions[i] = bodies[i].position;
            parentRotations[i] = bodies[i].rotation;
            velocities[i] = Vector3.zero;
        }
    }

    public void WriteData()
    {
#if SEPARATEAPPLY
        UnityEngine.Profiling.Profiler.BeginSample("SecondaryAnimationWrite");
        for (int i = 1; i < bodies.Length; i++)
        {
            bodies[i - 1].rotation = parentRotations[i];
        }
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    public JobHandle Execute(JobHandle dependsOn)
    {
        job.topos = bodies[0].position;
        job.torot = bodies[0].parent.rotation;
        job.deltaTime = Time.deltaTime;
        return job.Schedule(dependsOn);
    }

    [BurstCompile]
    struct AnimationChainJob : IJob
    {
        public NativeArray<float3> positions;
        public NativeArray<quaternion> parentRotations;
        public NativeArray<float3> localPositions;
        public NativeArray<quaternion> localRotations;
        public NativeArray<float3> velocities;
        public NativeArray<float> springs;
        public NativeArray<float> dampers;
        public NativeArray<float> drags;
        public NativeArray<float> gravities;
        public NativeArray<float3> constraints;

        public float deltaTime;
        public float3 topos;
        public quaternion torot;

        public void Execute()
        {
            var steps = Mathf.CeilToInt(deltaTime * 80); // at least 100 hz
            var dt = deltaTime / steps;
            var frompos = positions[0];
            var fromrot = parentRotations[0];
            velocities[0] = (topos - frompos) / deltaTime;
            for (int s = 0; s < steps; s++)
            {
                var ts = (s + 1f) / steps;
                positions[0] = math.lerp(frompos, topos, ts);
                parentRotations[0] = math.slerp(fromrot, torot, ts);

                for (int i = 1; i < positions.Length; i++)
                {
                    // acceleration: spring
#if SEPARATEAPPLY
                    var targetPos = positions[i - 1] + math.mul(math.mul(parentRotations[i - 1], localRotations[i - 1]), localPositions[i]);
#else
                var targetPos = bodies[i - 1].parent.TransformPoint(localPositions[i - 1] + localRotations[i - 1] * localPositions[i]);
#endif
                    velocities[i] += (targetPos - positions[i]) * springs[i - 1] * dt;

                    // acceleration: damper

                    velocities[i] += (velocities[i - 1] - velocities[i]) * dampers[i - 1] * dt; // relative to parent)
                    velocities[i] += -velocities[i] * math.sqrt(math.length(velocities[i])) * drags[i - 1] * dt; // air resistance, but use power 1.5 instead of 2 for artistic purposes

                    // acceleration: gravity
                    velocities[i] += (float3)Physics.gravity * gravities[i - 1] * dt;

                    // integrate
                    positions[i] += velocities[i] * dt;

                    // solve constraints
                    var newPos = SetDistance(positions[i - 1], positions[i], math.length(localPositions[i]));
                    velocities[i] += (newPos - positions[i]) / dt;
                    positions[i] = newPos;

                    // make parent look at me
#if SEPARATEAPPLY
                    var pos = positions[i - 1];// bodies[i - 1].position;
                    var rot = math.mul(parentRotations[i - 1], localRotations[i - 1]);
                    var normalDirection = positions[i] - pos;

                    var swingRot = FromToRotation(math.mul(rot, localPositions[i]), normalDirection);
                    var worldRot = math.mul(swingRot, rot);
                    var localRot = math.mul(math.inverse(parentRotations[i - 1]), worldRot);
                    // Calculate the diff from the initial local rotation
                    // Clamp the diff values based on the constraint and reapply it back to get the final rotation
                    var diff = math.mul(math.inverse(localRotations[i - 1]), localRot);
                    var euler = ToEuler(diff);
                    var constraint = float3.zero;
                    if (constraints.Length > i - 1)
                    {
                        constraint = constraints[i - 1];
                    }

                    euler.x = Mathf.LerpAngle(euler.x, 0f, constraint.x);
                    euler.y = Mathf.LerpAngle(euler.y, 0f, constraint.y);
                    euler.z = Mathf.LerpAngle(euler.z, 0f, constraint.z);

                    localRot = math.mul(localRotations[i - 1], quaternion.Euler(euler));

                    parentRotations[i] = math.mul(parentRotations[i - 1], localRot);
#else
                var pos = positions[i - 1];// bodies[i - 1].position;
                var rot = parentRotations[i - 1] * localRotations[i - 1];
                var normalDirection = positions[i] - pos;

                var swingRot = Quaternion.FromToRotation(rot * localPositions[i], normalDirection);

                bodies[i - 1].rotation = swingRot * rot;
#endif


                }
            }
        }

        float3 SetDistance(float3 origin, float3 target, float dist)
        {
            return origin + math.normalize(target - origin) * dist;
        }

        quaternion FromToRotation(float3 from, float3 to)
        {
            var axis = math.normalize(math.cross(from, to));
            var angle = math.acos(math.clamp(math.dot(math.normalize(from), math.normalize(to)), -1f, 1f));
            return quaternion.AxisAngle(axis, angle);
        }

        float3 ToEuler(quaternion q)
        {
            const float epsilon = 1e-6f;

            //prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * new float4(2.0f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * new float4(2.0f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = new float3(0.0f);

            const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);

            var y1 = d2.z - d1.y;
            if (y1 * y1 < CUTOFF)
            {
                var x1 = d2.y + d1.x;
                var x2 = d3.z + d3.w - d3.y - d3.x;
                var z1 = d2.x + d1.z;
                var z2 = d3.x + d3.w - d3.y - d3.z;
                euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
            }
            else //xzx
            {
                y1 = math.clamp(y1, -1.0f, 1.0f);
                var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
            }
            return euler;
        }
    }
}
