using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NBG.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine.Profiling;
using System.Runtime.InteropServices;

namespace Recoil
{
    [BurstCompile(CompileSynchronously = true)]
    public struct ConstraintJacobianJob : IJobParallelFor
    {

        public unsafe void Execute(int index)
        {
            ref var c = ref World.main.GetConstraint(index);
            if (c.destroyed) return;
            c.BuildJacobian();
        }

        public unsafe JobHandle Schedule(JobHandle dependsOn = default)
        {
            ProfilerMarkers.EnsureCreated();
            Profiler.BeginSample("ScheduleArticulationVsBody");

            var jobHandle = this.Schedule(World.main.constraintCount, 1, dependsOn);
            Profiler.EndSample();
            return jobHandle;
        }

    }
    [BurstCompile(CompileSynchronously = true)]
    public struct ConstraintVelocityIterationJob : IJobParallelFor
    {
        public unsafe void Execute(int index)
        {
            ref var c = ref World.main.GetConstraint(index);
            if (c.destroyed) return;
            c.VelocityIterationDRY();
        }

        public unsafe JobHandle Schedule(JobHandle dependsOn = default)
        {
            var jobHandle = this.Schedule(World.main.constraintCount, World.main.constraintCount, dependsOn);
            return jobHandle;
        }

    }


    [BurstCompile(CompileSynchronously = true)]
    public unsafe struct ConstraintBlock
    {
        int _destroyed;
        public bool destroyed { get => _destroyed > 0; set => _destroyed = value ? 1 : 0; }
        public Solver solver;
        [NativeDisableUnsafePtrRestriction] public ArticulationLinkReference* bodiesToLinkReferences;
        [NativeDisableUnsafePtrRestriction] public ArticulatedBodyInertia* articulatedInertias;

        //public unsafe void Allocate(in World world, IList<Rigidbody> chain, ArticulationJoint[] joints)
        //{
        //    var links = new int[chain.Count];
        //    for (int i = 0; i < chain.Count; i++)
        //        links[i] = world.FindOrAddBody(chain[i]);

        //    solver.Allocate(world, links, joints);
        //    bodiesToLinkReferences = Unsafe.Malloc<int>(solver.nLinks, Allocator.Persistent);
        //    articulatedInertias = Unsafe.Malloc<ArticulatedBodyInertia>(solver.nLinks, Allocator.Persistent);
        //    for (int i = 0; i < solver.nLinks; i++)
        //        bodiesToLinkReferences[i] = world.bodiesToLinkReferences[solver.links[i]];
        //}
        public unsafe void Allocate(int[] links, ArticulationJoint[] joints)
        {
            solver.Allocate( links, joints);
            bodiesToLinkReferences = Unsafe.Malloc<ArticulationLinkReference>(solver.nLinks, Allocator.Persistent);
            articulatedInertias = Unsafe.Malloc<ArticulatedBodyInertia>(solver.nLinks, Allocator.Persistent);
            for (int i = 0; i < solver.nLinks; i++)
                bodiesToLinkReferences[i] = World.main.GetBody(solver.links[i]).linkRef;
        }
        public unsafe void Dispose()
        {
            //for (int i = 0; i < solver.nLinks; i++)
            //    world.ReleaseBody(solver.links[i]);
            Unsafe.Free(bodiesToLinkReferences, Allocator.Persistent);
            Unsafe.Free(articulatedInertias, Allocator.Persistent);
            solver.Dispose();
        }
        public unsafe BlockSolverContext GetContext(NativeArray<Velocity4> blockV)
        {
            return new BlockSolverContext(
                solver.links, solver.nLinks, bodiesToLinkReferences, articulatedInertias, blockV);
        }
        public SolverBodies GetBodies() => solver.GetBodies();
        public static void DisposeContext(in BlockSolverContext context)
        {
        }

        public void RecalculateArticulatedInertias()
        {
            for (int i = 0; i < solver.nLinks; i++)
            {
                if (!bodiesToLinkReferences[i].isEmpty)
                {
                    var articulation = World.main.GetArticulation(bodiesToLinkReferences[i].articulationId);
                    articulatedInertias[i] = Articulation.ComputeImpulseResponseFast(articulation, bodiesToLinkReferences[i].linkId);
                }
            }
        }
        public unsafe void BuildJacobian()
        {
            
            RecalculateArticulatedInertias();
            var v = solver.ExtractWorldVelocityCopy();
            using (var context = GetContext( v))
            {

                solver.BuildJacobians(context);
                //solver.VelocityIterationNoBias(context);
            }
            solver.WriteAndDisposeVelocityCopy(v);
        }
        public unsafe void VelocityIteration()
        {
            var v = solver.ExtractWorldVelocityCopy();
            using (var context = GetContext( v))
            {
                
                solver.VelocityIterationNoBias(context);
            }


            solver.WriteAndDisposeVelocityCopy( v);

            for (int link = 0; link < solver.nLinks; link++)
            {
                if (!bodiesToLinkReferences[link].isEmpty)
                {

                    // do a velocity pass
                    ref var articulation = ref World.main.GetArticulation(bodiesToLinkReferences[link].articulationId);
                    var v2 = articulation.ExtractWorldVelocityCopy();
                    using (var context = articulation.GetContext( v2))
                        articulation.solver.VelocityIterationBias(context);
                    articulation.WriteAndDisposeVelocityCopy( v2);
                }
            }

            //referencedArticulations.Dispose();
        }
        public unsafe void VelocityIterationDRY()
        {
            var v = solver.ExtractWorldVelocityCopy();
            using (var context = GetContext(v))
            {
                //Solver.debug = true;
                solver.VelocityIterationNoBias(context);
                //Solver.debug = false;
            }


            solver.WriteAndDisposeVelocityCopy( v);


            NativeList<int> referencedArticulations = new NativeList<int>(solver.nLinks, Allocator.Temp);
            for (int link = 0; link < solver.nLinks; link++)
            {
                if (!bodiesToLinkReferences[link].isEmpty)
                {
                    var articulationId = bodiesToLinkReferences[link].articulationId;
                    var idx = referencedArticulations.IndexOf(articulationId);
                    if (idx < 0)
                    {
                        referencedArticulations.Add(articulationId);

                        // do a velocity pass
                        ref var articulation = ref World.main.GetArticulation(articulationId);
                        var v2 = articulation.ExtractWorldVelocityCopy();
                        using (var context = articulation.GetContext(v2))
                            articulation.solver.VelocityIterationBias(context);
                        articulation.WriteAndDisposeVelocityCopy(v2);

                    }
                }
            }

            referencedArticulations.Dispose();
        }
    }

}
