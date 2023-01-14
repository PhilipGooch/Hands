#define DEBUGGING
using Drawing;
using NBG.Core;
using Recoil;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Water
{
    class FloatingMeshBackend : IFloatingMeshBackend
    {
        const int kMaxBodiesOfWater = 8;

        IFloatingMesh target;
        IFloatingMeshSettings targetSettings;

        Rigidbody body;
        ReBody reBody;

        NativeArray<float3> allMeshVertices;
        NativeArray<int> allMeshTriangles;
        NativeArray<BoxBounds> bodiesOfWater;
        NativeArray<float3> bodiesOfWaterGlobalFlow;

        TransformVerticesJob transformVerticesJob;
        GenerateFloatingVerticesJob generateFloatingVerticesJob;
        ApplyTriangleForcesJob applyForcesJob;
        CalculateForcesAndMomentsJob calculateForcesJob;
        NativeArray<FloatingVertex> allFloatingVertices;

        float calculatedMass;
        float calculatedBuoyancyMultiplier;

        public float CalculatedMass { get; }
        public float CalculatedVolume { get; }
        public float CalculatedBuoyancyMultiplier { get; private set; }
        public int OriginalVertexCount { get; }
        public int OptimizedVertexCount { get; }
        public int TriangleCount { get; }

        public FloatingMeshBackend(IFloatingMesh fm, IFloatingMeshSettings fmSettings)
        {
            Debug.Assert(fm != null);
            Debug.Assert(fm.FloatingGameObject != null);
            Debug.Assert(fm.Rigidbody != null);
            Debug.Assert(fm.HullGameObject != null);
            Debug.Assert(fm.HullMesh != null);
            Debug.Assert(fm.HullMesh.isReadable);
            Debug.Assert(fm.HullWaterSensor != null);
            Debug.Assert(fmSettings != null);

            target = fm;
            targetSettings = fmSettings;

            body = target.Rigidbody;
            reBody = new ReBody(body);
            if (body == null)
                throw new System.InvalidOperationException("Floater needs a Rigidbody");

            if (!target.HullMesh.isReadable)
            {
#if UNITY_EDITOR
                Debug.LogError($"FloatingMesh cannot read mesh vertices from {target.HullMesh.name}. You need to enable read/write for that mesh!", target.HullMesh);
#endif
                return;
            }

            // Extract geometry
            {
                List<int> triangleList = new List<int>();
                List<float3> vertexList = new List<float3>();

                GrabMeshTriangles(triangleList, vertexList, target.HullMesh, target.FloatingGameObject.transform, target.HullGameObject.transform);

                allMeshVertices = new NativeArray<float3>(vertexList.ToArray(), Allocator.Persistent);
                allMeshTriangles = new NativeArray<int>(triangleList.ToArray(), Allocator.Persistent);
                allFloatingVertices = new NativeArray<FloatingVertex>(allMeshVertices.Length, Allocator.Persistent);

                OriginalVertexCount = target.HullMesh.vertexCount;
                OptimizedVertexCount = vertexList.Count;
                TriangleCount = triangleList.Count;
            }

            // Diagnostics
            {
                calculatedMass = body.mass;

                CalculatedMass = calculatedMass;
                CalculatedVolume = GetVolume();
                if (Mathf.Approximately(CalculatedVolume, 0.0f))
                    throw new System.NotFiniteNumberException("Floating mesh volume is 0.");

                UpdateCalculatedBuoyancyMultiplier();
            }

            //TODO: remove 8 limit?
            //TODO: unify into a single structure?
            bodiesOfWater = new NativeArray<BoxBounds>(kMaxBodiesOfWater, Allocator.Persistent);
            bodiesOfWaterGlobalFlow = new NativeArray<float3>(kMaxBodiesOfWater, Allocator.Persistent);

            float liquidDensity = 1000.0f * calculatedBuoyancyMultiplier * target.InstanceData.buoyancyMultiplier;

            // TransformVerticesJob
            var transformedVertices = new NativeArray<float3>(allMeshVertices.Length, Allocator.Persistent);
            var transformedVelocities = new NativeArray<float3>(allMeshVertices.Length, Allocator.Persistent);
            transformVerticesJob = new TransformVerticesJob(
                allMeshVertices,
                target.FloatingGameObject.transform.localToWorldMatrix,
                reBody.Id,
                transformedVertices, transformedVelocities
                );

            // GenerateFloatingVerticesJob
            generateFloatingVerticesJob = new GenerateFloatingVerticesJob(
                transformedVertices, transformedVelocities,
                0, // Not initialized here
                bodiesOfWater,
                bodiesOfWaterGlobalFlow,
                allFloatingVertices
                );

            // ApplyTriangleForcesJob
            var triangleCount = allMeshTriangles.Length / 3;
            var resultantMoments = new NativeArray<float3>(triangleCount, Allocator.Persistent);
            var resultantForces = new NativeArray<float3>(triangleCount, Allocator.Persistent);
            var resultantStaticForces = new NativeArray<float3>(triangleCount, Allocator.Persistent);
            applyForcesJob = new ApplyTriangleForcesJob(
                allFloatingVertices, allMeshTriangles,
                liquidDensity,
                float3.zero,
                0.0f /*floatingData.bendWind*/, // bendWind not used currently
                targetSettings.SimulationData.pressureLinear,
                targetSettings.SimulationData.pressureSquare,
                targetSettings.SimulationData.suctionLinear,
                targetSettings.SimulationData.suctionSquare,
                targetSettings.SimulationData.falloffPower,
                resultantMoments, resultantForces, resultantStaticForces);

            // CalculateForcesAndMomentsJob
            var forceResults = new NativeArray<float3>(3, Allocator.Persistent);
            calculateForcesJob = new CalculateForcesAndMomentsJob(resultantMoments, resultantForces, resultantStaticForces, forceResults);
        }

        public void Dispose()
        {
            if (allFloatingVertices != null && allFloatingVertices.Length > 0)
            {
                bodiesOfWater.Dispose();
                bodiesOfWaterGlobalFlow.Dispose();
                allFloatingVertices.Dispose();
                allMeshTriangles.Dispose();
                allMeshVertices.Dispose();

                transformVerticesJob.Dispose();
                applyForcesJob.Dispose();
                calculateForcesJob.Dispose();
            }
        }

        float GetVolume() // m^3
        {
            var scale = target.FloatingGameObject.transform.lossyScale;
            float signedVolume = 0;
            for (int i = 0; i < allMeshTriangles.Length - 2; i += 3)
            {
                var firstId = allMeshTriangles[i];
                var secondId = allMeshTriangles[i + 1];
                var thirdId = allMeshTriangles[i + 2];
                signedVolume += Vector3.Dot(allMeshVertices[firstId], Vector3.Cross(allMeshVertices[secondId], allMeshVertices[thirdId])) / 6f;
            }
            signedVolume = signedVolume * scale.x * scale.y * scale.z;
            return Mathf.Abs(signedVolume);
        }

        static void GrabMeshTriangles(List<int> triangles, List<float3> vertices, Mesh targetMesh, Transform baseTransform, Transform hullTransform)
        {
            var meshTriangles = targetMesh.triangles;
            var meshVertices = targetMesh.vertices;

            for (int i = 0; i < meshTriangles.Length; i++)
            {
                var v = meshVertices[meshTriangles[i]];
                var transformedVertex = baseTransform.InverseTransformPoint(hullTransform.TransformPoint(v)); // Put into parent space
                var idx = vertices.IndexOf(transformedVertex);
                if (idx < 0)
                {
                    idx = vertices.Count;
                    vertices.Add(transformedVertex);
                }
                triangles.Add(idx);
            }

            List<float3> optimized = new List<float3>();
            for (int i = 0; i < triangles.Count; i++)
            {
                var v = vertices[triangles[i]];
                var idx = optimized.IndexOf(v);
                if (idx < 0)
                {
                    idx = optimized.Count;
                    optimized.Add(v);
                }
                triangles[i] = idx;
            }
            vertices.Clear();
            vertices.AddRange(optimized);
        }

        bool GetShouldPerformCalculations()
        {
            if (target == null)
                return false;

            if (Mathf.Approximately(target.InstanceData.buoyancyMultiplier, 0))
                return false;

            if (allMeshTriangles == null || allMeshTriangles.Length == 0)
            {
                Debug.LogError($"Floating mesh not initialized for {body.name}!", body.gameObject);
                return false;
            }

            if (body.IsSleeping())
                return false;

            if (!target.HullWaterSensor.Submerged)
                return false;

            return true;
        }

        const int kNumVerticesPerJob = 128;
        const int kNumTrianglesPerJob = 64;

        public unsafe JobHandle ScheduleJobs(JobHandle dependsOn)
        {
            if (!GetShouldPerformCalculations())
                return dependsOn;

            // TransformVerticesJob
            transformVerticesJob.localToWorldMatrix = target.FloatingGameObject.transform.localToWorldMatrix;
            dependsOn = transformVerticesJob.Schedule(allMeshVertices.Length, kNumVerticesPerJob, dependsOn);

            // GenerateFloatingVerticesJob
            var bodies = target.HullWaterSensor.BodiesOfWater;
            var numBodiesOfWater = Mathf.Min(bodies.Count, kMaxBodiesOfWater);
            generateFloatingVerticesJob.bodiesOfWaterCount = numBodiesOfWater;
            for (int i = 0; i < numBodiesOfWater; ++i)
            {
                var bodyOfWater = bodies[i];
                bodiesOfWater[i] = bodyOfWater.GlobalBox;
                bodiesOfWaterGlobalFlow[i] = bodyOfWater.GlobalFlow;
            }
            dependsOn = generateFloatingVerticesJob.Schedule(allMeshVertices.Length, kNumVerticesPerJob, dependsOn);

            // ApplyTriangleForcesJob
            UpdateCalculatedBuoyancyMultiplier();
            var liquidDensity = 1000.0f * calculatedBuoyancyMultiplier * target.InstanceData.buoyancyMultiplier;
            applyForcesJob.UpdateSettings(
                liquidDensity,
                reBody.worldCenterOfMass,
                0.0f /*floatingData.bendWind*/, // bendWind not used currently
                targetSettings.SimulationData.pressureLinear,
                targetSettings.SimulationData.pressureSquare,
                targetSettings.SimulationData.suctionLinear,
                targetSettings.SimulationData.suctionSquare,
                targetSettings.SimulationData.falloffPower
                );
            resultantMoment = resultantForce = resultantStaticForce = Vector3.zero; // Set these things to 0 
            dependsOn = applyForcesJob.Schedule(allMeshTriangles.Length / 3, kNumTrianglesPerJob, dependsOn);

            // CalculateForcesAndMomentsJob
            dependsOn = calculateForcesJob.Schedule(dependsOn);

            return dependsOn;
        }

        public void ApplyForces()
        {
            if (!GetShouldPerformCalculations())
                return;

            resultantMoment = calculateForcesJob.results[0];
            resultantForce = calculateForcesJob.results[1];
            resultantStaticForce = calculateForcesJob.results[2];

            if (horizontalHydrostaticTreshold > 0)
            {
                var h = new float3(resultantStaticForce.x, 0, resultantStaticForce.z);
                if (math.length(h) < horizontalHydrostaticTreshold)
                    resultantForce -= h;
            }

            //// clamp force
            //var velocity = body.velocity;
            //var forceAlongVelocity = Vector3.Dot(velocity.normalized, resultantForce);
            //if (forceAlongVelocity > 0)
            //{
            //    var maxSpeed = 5;
            //    var normalForce = resultantForce - forceAlongVelocity*resultantForce.normalized;
            //    forceAlongVelocity = Mathf.Min(forceAlongVelocity, (maxSpeed - velocity.magnitude) / Time.fixedDeltaTime * FloatingMeshSync.skipFrames);
            //    resultantForce = normalForce + forceAlongVelocity * velocity.normalized;
            //}

            if (targetSettings.SimulationData.ignoreHydrodynamicForce != Vector3.zero)
            {
                var ignoreDir = target.FloatingGameObject.transform.TransformVector(targetSettings.SimulationData.ignoreHydrodynamicForce);
                var project = math.dot(resultantForce, ignoreDir.normalized);
                if (project < 0)
                    resultantForce -= project * (float3)ignoreDir;
            }

            // clamp forces
            resultantForce = Vector3.ClampMagnitude(resultantForce, reBody.mass * 100);// normalForce + forceAlongVelocity * velocity.normalized;
            resultantMoment = Vector3.ClampMagnitude(resultantMoment, reBody.mass * 100);

            if (float.IsNaN(resultantForce.x) || float.IsNaN(resultantForce.y) || float.IsNaN(resultantForce.z))
            {
#if DEBUGGING
                Debug.LogWarning($"FloatingMesh '{target.FloatingGameObject.name}' resultantForce is NaN.");
#endif
            }
            else
            {
                reBody.AddForce(resultantForce);          // Add this force to the body
            }

            if (float.IsNaN(resultantMoment.x) || float.IsNaN(resultantMoment.y) || float.IsNaN(resultantMoment.z))
            {
#if DEBUGGING
                Debug.LogWarning($"FloatingMesh '{target.FloatingGameObject.name}' resultantMoment is NaN.");
#endif
            }
            else
            {
                reBody.AddTorque(resultantMoment);      // Add this torque to the body 
            }
        }

        float3 resultantMoment;        // A vector for the movement of this thing 
        float3 resultantForce;         // A Vector for the force of this thing 
        float3 resultantStaticForce;   // A Vector for the static force of this thing 

        const float horizontalHydrostaticTreshold = 0;         // might be needed for objects with bigger triangles

        public void DrawDebugGizmos()
        {
            if (target == null)
                return;
            if (target.HullWaterSensor == null || !target.HullWaterSensor.Submerged)
                return;

            for (int i = 0; i < allFloatingVertices.Length; i++)
            {
                var v = allFloatingVertices[i];
                var color = (v.depth <= 0.0f) ? Color.yellow : Color.red;
                Draw.SphereOutline(v.pos, 0.1f, color);
            }

            Draw.PushColor(Color.blue);
            Draw.Ray(body.position, resultantForce / 10);
            Draw.Ray(body.position, resultantStaticForce / 10);
            Draw.Ray(body.position, new Vector3(resultantStaticForce.x, 0, resultantStaticForce.z) / 10);
            Draw.PopColor();
        }

        void UpdateCalculatedBuoyancyMultiplier()
        {
            if (target.InstanceData.mode == FloatingMeshMode.Normal)
            {
                calculatedBuoyancyMultiplier = 1.0f;
            }
            else if (target.InstanceData.mode == FloatingMeshMode.NormalizeMass)
            {
                if (Mathf.Approximately(target.InstanceData.buoyancyNormalizedMass, 0.0f))
                    throw new System.Exception("buoyancyNormalizedMass must be non-zero");
                calculatedBuoyancyMultiplier = calculatedMass / target.InstanceData.buoyancyNormalizedMass;
            }
            else
            {
                throw new System.NotImplementedException();
            }

            CalculatedBuoyancyMultiplier = calculatedBuoyancyMultiplier;
        }
    }
}
