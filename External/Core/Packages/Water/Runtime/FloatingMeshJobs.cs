//#define PERVERTEXCURRENT
using NBG.Core;
using Recoil;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Water
{
    struct FloatingVertex
    {
        public float3 pos;
        public float3 velocity;
        public float depth;
    }

    [BurstCompile]
    struct TransformVerticesJob : IJobParallelFor
    {
        [ReadOnly, NoAlias]
        NativeArray<float3> inVertices;
        [ReadOnly]
        public float4x4 localToWorldMatrix;
        [ReadOnly]
        public int reBodyId;

        [NoAlias]
        NativeArray<float3> outVertices;
        [NoAlias]
        NativeArray<float3> outVelocities;

        public TransformVerticesJob(
            NativeArray<float3> inVertices,
            float4x4 localToWorldMatrix,
            int reBodyId,
            NativeArray<float3> outVertices,
            NativeArray<float3> outVelocities)
        {
            this.inVertices = inVertices;
            this.localToWorldMatrix = localToWorldMatrix;
            this.reBodyId = reBodyId;
            this.outVertices = outVertices;
            this.outVelocities = outVelocities;
        }

        public void Execute(int index)
        {
            float3 transformed = math.mul(localToWorldMatrix, new float4(inVertices[index], 1.0f)).xyz;
            float3 velocity = float3.zero;
            
            if (reBodyId != World.environmentId)
            {
                velocity = World.main.GetWorldPointVelocity(reBodyId, transformed).linear;
            }

            outVertices[index] = transformed;
            outVelocities[index] = velocity;
        }

        public void Dispose()
        {
            outVertices.Dispose();
            outVelocities.Dispose();
        }
    }

    [BurstCompile]
    struct GenerateFloatingVerticesJob : IJobParallelFor
    {
        [ReadOnly, NoAlias]
        NativeArray<float3> inVertices;
        [ReadOnly, NoAlias]
        NativeArray<float3> inVelocities;

        [ReadOnly]
        public int bodiesOfWaterCount;
        [ReadOnly]
        public NativeArray<BoxBounds> bodiesOfWater;
        [ReadOnly]
        public NativeArray<float3> bodiesOfWaterGlobalFlow;

        [NoAlias]
        NativeArray<FloatingVertex> outVertices;

        public GenerateFloatingVerticesJob(
            NativeArray<float3> inVertices,
            NativeArray<float3> inVelocities,
            int bodiesOfWaterCount,
            NativeArray<BoxBounds> bodiesOfWater,
            NativeArray<float3> bodiesOfWaterGlobalFlow,
            NativeArray<FloatingVertex> outVertices)
        {
            this.inVertices = inVertices;
            this.inVelocities = inVelocities;
            this.bodiesOfWaterCount = bodiesOfWaterCount;
            this.bodiesOfWater = bodiesOfWater;
            this.bodiesOfWaterGlobalFlow = bodiesOfWaterGlobalFlow;
            this.outVertices = outVertices;
        }

        public unsafe void Execute(int index)
        {
#if !PERVERTEXCURRENT
            float currentWeight = 0.0f;
            float3 currentDir = float3.zero;// Current.Sample(body.worldCenterOfMass, out currentWeight);
#endif

            var transformed = inVertices[index];

            float depth = -1.0f;
            float3 waterVelocity = float3.zero;
            for (int i = 0; i < bodiesOfWaterCount; ++i)
            {
                var box = bodiesOfWater[i];
                SampleDepth(box, transformed, out float depthInBody);
                if (depthInBody > depth) // Find max depth
                {
                    depth = depthInBody;
                    waterVelocity = bodiesOfWaterGlobalFlow[i];
                }
            }

            if (depth != -1.0f)
            {
#if PERVERTEXCURRENT
                var currentWeight = 0f;
                var currentDir = Current.Sample(transformed, out currentWeight);
#endif
                waterVelocity = math.lerp(waterVelocity, currentDir, currentWeight); // Lerp the verts in line with the water    
            }

            FloatingVertex outVertex;
            outVertex.pos = transformed;
            outVertex.velocity = inVelocities[index] - waterVelocity;
            outVertex.depth = depth;

            outVertices[index] = outVertex;
        }

        static void SampleDepth(in BoxBounds box, in float3 worldPos, out float outDepth)
        {
            if (!box.Contains(worldPos))
            {
                outDepth = -1.0f;
                return;
            }

#if DEBUG_SAMPLES
            Draw.SolidBox(worldPos, new float3(0.1f, 0.1f, 0.1f), Color.green);
#endif

            float3 worldRayOrigin = worldPos;
            var intersections = box.IntersectRay(worldRayOrigin, new float3(0, 1, 0));
            outDepth = math.max(intersections.x, intersections.y);

#if DEBUG_SAMPLES
            Draw.SolidBox(worldRayOrigin + new float3(0, 1, 0) * depth, new float3(0.1f, 0.1f, 0.1f), Color.red);
#endif
        }
    }

    [BurstCompile]
    struct ApplyTriangleForcesJob : IJobParallelFor
    {
        [ReadOnly]
        [NoAlias]
        NativeArray<FloatingVertex> vertices;
        [ReadOnly]
        NativeArray<int> triangles;
        [ReadOnly]
        public float liquidDensity;

        // A vector for the centre of this thing
        [ReadOnly]
        public float3 center;
        [ReadOnly]
        public float bendWind;
        [ReadOnly]
        public float pressureLinear;
        [ReadOnly]
        public float pressureSquare;
        [ReadOnly]
        public float suctionLinear;
        [ReadOnly]
        public float suctionSquare;
        [ReadOnly]
        public float falloffPower;

        // A vector for the movement of this thing 
        public NativeArray<float3> resultantMoment;
        // A Vector for the force of this thing 
        public NativeArray<float3> resultantForce;
        // A Vector for the static force of this thing 
        public NativeArray<float3> resultantStaticForce;

        public ApplyTriangleForcesJob(NativeArray<FloatingVertex> vertices, NativeArray<int> triangles, float liquidDensity, float3 center, float bendWind, float pressureLinear,
            float pressureSquare, float suctionLinear, float suctionSquare, float falloffPower,
            NativeArray<float3> resultantMoment, NativeArray<float3> resultantForce, NativeArray<float3> resultantStaticForce)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.liquidDensity = liquidDensity;
            this.center = center;
            this.bendWind = bendWind;
            this.pressureLinear = pressureLinear;
            this.pressureSquare = pressureSquare;
            this.suctionLinear = suctionLinear;
            this.suctionSquare = suctionSquare;
            this.falloffPower = falloffPower;

            this.resultantMoment = resultantMoment;
            this.resultantForce = resultantForce;
            this.resultantStaticForce = resultantStaticForce;
        }

        public void UpdateSettings(float liquidDensity, float3 center, float bendWind, float pressureLinear,
            float pressureSquare, float suctionLinear, float suctionSquare, float falloffPower)
        {
            this.liquidDensity = liquidDensity;
            this.center = center;
            this.bendWind = bendWind;
            this.pressureLinear = pressureLinear;
            this.pressureSquare = pressureSquare;
            this.suctionLinear = suctionLinear;
            this.suctionSquare = suctionSquare;
            this.falloffPower = falloffPower;
        }

        public void Execute(int index)
        {
            FloatingVertex a = vertices[triangles[index * 3]];
            FloatingVertex b = vertices[triangles[index * 3 + 1]];
            FloatingVertex c = vertices[triangles[index * 3 + 2]];

            resultantMoment[index] = float3.zero;
            resultantForce[index] = float3.zero;
            resultantStaticForce[index] = float3.zero;
            // This is done every frame 

            //DrawTriangleGizmos(a, b, c);
            // triangle above surface
            if (a.depth <= 0 && b.depth <= 0 && c.depth <= 0)
            {
                return;
            }

            float3 areaNormal = -math.cross(a.pos - b.pos, c.pos - b.pos) * .5f;

            // sort by depth
            FloatingVertex h, m, l;
            if (a.depth <= b.depth && a.depth <= c.depth)
            {
                h = a;
                if (b.depth < c.depth) { m = b; l = c; }
                else { m = c; l = b; }
            }
            else if (b.depth <= a.depth && b.depth <= c.depth)
            {
                h = b;
                if (a.depth < c.depth) { m = a; l = c; }
                else { m = c; l = a; }
            }
            else
            {
                h = c;
                if (a.depth < b.depth) { m = a; l = b; }
                else { m = b; l = a; }
            }

            // triangle fully submerged
            if (a.depth >= 0 && b.depth >= 0 && c.depth >= 0)
                ApplySumbergedTriangleForces(h, m, l, areaNormal, index);      // add the force to the triangles 

            else if (m.depth <= 0) // 1 vertex below water - calculate intersection
            {
                var tm = -l.depth / (m.depth - l.depth);
                var th = -l.depth / (h.depth - l.depth);
                var ih = new FloatingVertex()
                {
                    pos = l.pos + th * (h.pos - l.pos),
                    velocity = l.velocity + th * (h.velocity - l.velocity),
                    depth = 0
                };
                var im = new FloatingVertex()
                {
                    pos = l.pos + tm * (m.pos - l.pos),
                    velocity = l.velocity + tm * (m.velocity - l.velocity),
                    depth = 0
                };
                ApplySumbergedTriangleForces(ih, im, l, areaNormal, index);
            }
            else // 2 verts below water - calculate intersection
            {
                var tm = -h.depth / (m.depth - h.depth);
                var tl = -h.depth / (l.depth - h.depth);
                var il = new FloatingVertex()
                {
                    pos = h.pos + tl * (l.pos - h.pos),
                    velocity = h.velocity + tl * (l.velocity - h.velocity),
                    depth = 0
                };
                var im = new FloatingVertex()
                {
                    pos = h.pos + tm * (m.pos - h.pos),
                    velocity = h.velocity + tm * (m.velocity - h.velocity),
                    depth = 0
                };
                ApplySumbergedTriangleForces(im, m, l, areaNormal, index);
                ApplySumbergedTriangleForces(il, im, l, areaNormal, index);
            }
        }

        private void ApplySumbergedTriangleForces(FloatingVertex t, FloatingVertex m, FloatingVertex l, float3 areaNormal, int index)
        {
            if (m.depth == t.depth || t.depth == l.depth)
            {
                TrianglePointingDown(t, m, l, areaNormal, index);
            }
            else if (m.depth == l.depth)
            {
                TrianglePointingUp(t, m, l, areaNormal, index);
            }
            else                                                            // will need to split the triangle at mid
            {
                var it = (m.depth - l.depth) / (t.depth - l.depth);
                var i = new FloatingVertex()
                {
                    pos = l.pos + it * (t.pos - l.pos),
                    velocity = l.velocity + it * (t.velocity - l.velocity),
                    depth = m.depth
                };

                TrianglePointingUp(t, m, i, areaNormal, index);
                TrianglePointingDown(i, m, l, areaNormal, index);
            }
        }

        private void TrianglePointingDown(FloatingVertex t, FloatingVertex m, FloatingVertex l, float3 areaNormal, int index)
        {
            Hydrostatic(t, m, l, areaNormal, index);
            Hydrodynamic(m, l, t, areaNormal, index);
        }

        private void TrianglePointingUp(FloatingVertex t, FloatingVertex m, FloatingVertex l, float3 areaNormal, int index)
        {
            Hydrostatic(m, l, t, areaNormal, index);
            Hydrodynamic(m, l, t, areaNormal, index);
        }

        private void Hydrostatic(FloatingVertex a, FloatingVertex b, FloatingVertex c, Vector3 areaNormal, int index)
        {
            //Profiler.BeginSample("Hydrostatic", this);
            //DrawTriangleGizmos(a, b, c);
            var O = c.pos; // tip
            var M = (a.pos + b.pos) / 2; // center point
            var B = ProjectPointOnLine(a.pos, math.normalize(b.pos - a.pos), c.pos); // base of height
            var H = math.length(B - O); // height of triangle
            var W = math.length(a.pos - b.pos);
            var dO = c.depth;
            var dM = a.depth;

            // depth @h = dO+(dM-dO)*h/H
            // width @h = W *h/H
            // point @h = O + (M-O)*h/H

            var force = liquidDensity * 9.81f * W * H * (dO / 2 + (dM - dO) / 3);
            var hintegral = liquidDensity * 9.81f * W * H * H * (dO / 3 + (dM - dO) / 4);
            var hForce = hintegral / force;

            if (W < 0.001f || H < 0.001f || force < 0.0001f)
            {
                //Profiler.EndSample();
                return;
            }

            var finalForce = -force * areaNormal.normalized;
            var forcePos = O + (M - O) * hForce / H;

            AddForceAtPosition(finalForce, forcePos, false, index);

            //Profiler.EndSample();

        }

        private void Hydrodynamic(FloatingVertex a, FloatingVertex b, FloatingVertex c, float3 areaNormal, int index)
        {
            //Profiler.BeginSample("Hydrodynamic", this);
            var surface = math.length(areaNormal);
            var normal = areaNormal / surface;

            //Hydrodynamic(a, surface/3, normal);
            //Hydrodynamic(b, surface/3, normal);
            //Hydrodynamic(c, surface/3, normal);

            // Expand above
            surface /= 3;
            {
                var v = a;
                var vi = v.velocity; // apparent wind (ui = vi.normailzed)
                var speed = math.length(vi);
                if (speed != 0)
                    vi /= speed;

                if (bendWind > 0)
                    normal = math.normalize(normal - vi * bendWind);

                float force;
                var cos = math.dot(vi, normal);
                if (cos > 0)
                    force = -(pressureLinear * speed + pressureSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(cos, falloffPower) : cos);
                else
                    force = (suctionLinear * speed + suctionSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(-cos, falloffPower) : -cos);


                AddForceAtPosition(force * normal, v.pos, true, index);
            }
            {
                var v = b;
                var vi = v.velocity; // apparent wind (ui = vi.normailzed)
                var speed = math.length(vi);
                if (speed != 0)
                    vi /= speed;

                if (bendWind > 0)
                    normal = math.normalize(normal - vi * bendWind);

                float force;
                var cos = math.dot(vi, normal);
                if (cos > 0)
                    force = -(pressureLinear * speed + pressureSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(cos, falloffPower) : cos);
                else
                    force = (suctionLinear * speed + suctionSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(-cos, falloffPower) : -cos);

                AddForceAtPosition(force * normal, v.pos, true, index);
            }
            {
                var v = c;
                var vi = v.velocity; // apparent wind (ui = vi.normailzed)
                var speed = math.length(vi);
                if (speed != 0)
                    vi /= speed;

                if (bendWind > 0)
                    normal = math.normalize(normal - vi * bendWind);

                float force;
                var cos = math.dot(vi, normal);
                if (cos > 0)
                    force = -(pressureLinear * speed + pressureSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(cos, falloffPower) : cos);
                else
                    force = (suctionLinear * speed + suctionSquare * speed * speed) * surface * (falloffPower != 1 ? Mathf.Pow(-cos, falloffPower) : -cos);

                AddForceAtPosition(force * normal, v.pos, true, index);
            }

            //Profiler.EndSample();
        }

        void AddForceAtPosition(float3 force, float3 pos, bool isDynamic, int index)
        {
            //if (showDebug)
            //    Debug.Log(name + " Add Force at position ");

            //body.AddForceAtPosition(force, pos);
            resultantMoment[index] += math.cross(pos - center, force);
            resultantForce[index] += force;

            var moment = resultantMoment[index];

            bool3 isNan = math.isnan(moment);
            if (math.any(isNan))
            {
                // Check whether the result is invalid for all the axis
                resultantMoment[index] = Vector3.zero;
            }
            else
            {
                // The result is valid so do something with it 
                if (!isDynamic)
                {
                    // Check whether the result is invalid
                    resultantStaticForce[index] += force;
                }
            }
        }

        private float3 ProjectPointOnLine(float3 linePoint, float3 lineVec, float3 point)
        {
            //get vector from point on line to point in space
            float3 linePointToPoint = point - linePoint;
            float t = math.dot(linePointToPoint, lineVec);
            return linePoint + lineVec * t;
        }

        public void Dispose()
        {
            resultantForce.Dispose();
            resultantMoment.Dispose();
            resultantStaticForce.Dispose();
        }
    }

    [BurstCompile]
    struct CalculateForcesAndMomentsJob : IJob
    {
        // A vector for the movement of this thing
        [ReadOnly]
        public NativeArray<float3> resultantMoments;
        // A Vector for the force of this thing
        [ReadOnly]
        public NativeArray<float3> resultantForces;
        // A Vector for the static force of this thing
        [ReadOnly]
        public NativeArray<float3> resultantStaticForces;

        public NativeArray<float3> results;

        public CalculateForcesAndMomentsJob(NativeArray<float3> resultantMoments, NativeArray<float3> resultantForces, NativeArray<float3> resultantStaticForces, NativeArray<float3> results)
        {
            this.resultantMoments = resultantMoments;
            this.resultantForces = resultantForces;
            this.resultantStaticForces = resultantStaticForces;
            this.results = results;
        }

        public void Execute()
        {
            results[0] = results[1] = results[2] = float3.zero;

            for (int i = 0; i < resultantMoments.Length; i++)
            {
                results[0] += resultantMoments[i];
                results[1] += resultantForces[i];
                results[2] += resultantStaticForces[i];
            }
        }

        public void Dispose()
        {
            results.Dispose();
        }
    }
}
