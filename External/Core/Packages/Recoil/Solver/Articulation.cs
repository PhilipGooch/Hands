//#define HEAVY_BALL
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Articulations are based on Roblox articulations and Erin Catto soft springs.
/// Roblox: GDC, Improving an Iterative Physics Solver using a Direct Method
/// https://www.youtube.com/watch?v=P-WP1yMOkc4
/// Erin Catto:
/// Soft Constraints, reinventing the spring, page 36
/// https://box2d.org/files/ErinCatto_SoftConstraints_GDC2011.pdf
/// 
/// Springs/dampers in joints are calculated according to Erin Catto - they are used to calculate gamma and beta to use in the differential equation.
/// K matrix (J*W*JT) is formed according to roblox (or featherstone).
/// Inverting is taken from Roblox but remade to work with 4x4 matrices to be burst friendly.
/// Iterative solver extended according to the recoil PDF by Tomas Sakalauskas. It allows interactions between articulations.
/// </summary>

namespace Recoil
{
    [BurstCompile(CompileSynchronously = true)]
    public struct ArticulationJacobianJob : IJobParallelFor
    {
        // references to world data

        public unsafe void Execute(int index)
        {
            ref var articulation = ref World.main.GetArticulation(index);
            if (articulation.destroyed) return;
            articulation.solver.CalculateBiasDeltaV(MotionVector.Linear(World.main.gravity));
            var v = articulation.ExtractWorldVelocityCopy();
            using (var context = articulation.GetContext(v))
            {
                articulation.solver.BuildJacobians(context);
            }
            articulation.WriteAndDisposeVelocityCopy(v);
        }

        public unsafe JobHandle Schedule(JobHandle dependsOn = default)
        {
            Profiler.BeginSample("Schedule");
            var jobHandle = this.Schedule(World.main.articulationCount, 1, dependsOn);
            Profiler.EndSample();
            return jobHandle;
        }

    }
    [BurstCompile(CompileSynchronously = true)]
    public struct ArticulationVelocityIterationJob : IJobParallelFor
    {

        public unsafe void Execute(int index)
        {
            ref var articulation = ref World.main.GetArticulation(index);
            if (articulation.destroyed) return;
            var v = articulation.ExtractWorldVelocityCopy();
            using (var context = articulation.GetContext(v))
            {
                //Solver.debug = true;
                articulation.solver.VelocityIterationBias(context);
                //Solver.debug = false;
            }
            articulation.WriteAndDisposeVelocityCopy(v);

        }



        public unsafe JobHandle Schedule(JobHandle dependsOn = default)
        {
            //if (iteration == 0) return dependsOn; // first iteration is handled in Jacobian

            Profiler.BeginSample("Schedule");

            var jobHandle = this.Schedule(World.main.articulationCount, 1, dependsOn);
            Profiler.EndSample();
            return jobHandle;
        }

    }


    [BurstCompile(CompileSynchronously =true)]
    public  unsafe partial struct Articulation
    {
        public Solver solver;
        int _destroyed;
        public bool destroyed { get => _destroyed > 0; set => _destroyed = value ? 1 : 0; }

        public unsafe void Allocate(int id, Rigidbody[] chain, ArticulationJoint[] joints)
        {
            var links = new int[chain.Length];
            for (int i = 0; i < chain.Length; i++)
            {
                links[i] = ManagedWorld.main.FindBody(chain[i]);
                if(id>=0)
                    World.main.GetBody(links[i]).linkRef=new ArticulationLinkReference( id, i);
            }
            solver.Allocate(links, joints);
        }
        public unsafe void Dispose()
        {
            solver.Dispose();
        }
        public ArticulationSolverContext GetContext(NativeArray<Velocity4> articulationV)
        {
            return new ArticulationSolverContext(solver.links,solver.nLinks, articulationV);
        }
        
        public static void DisposeContext(in ArticulationSolverContext context)
        {
        }

        public SolverBodies GetBodies() => solver.GetBodies();
        
        public NativeArray<Velocity4> ExtractWorldVelocityCopy()
        {
            return solver.ExtractWorldVelocityCopy();
        }
        public void WriteAndDisposeVelocityCopy(NativeArray<Velocity4> v)
        {
            solver.WriteAndDisposeVelocityCopy(v);
        }
     
        public void ApplyImpulse(in ArticulationSolverContext context,  int b, ForceVector impulse, bool accumulateImpulse)
        {
            context.ApplyImpulse(b, impulse);
            //ApplyImpulseSingleBody(context, b, context.v, impulse);
            solver.VelocityIteration(context,  accumulateImpulse);
        }
        public void ApplyImpulse(in ArticulationSolverContext context, int b, float3 anchor, float3 impulse, bool accumulateImpulse)
        {
            context.ApplyImpulseAtLocalPoint(b, impulse, anchor);
            //ApplyImpulseSingleBody(context, b, anchor, context.v, impulse);
            solver.VelocityIteration(context,  accumulateImpulse);
        }

       
        //public float3 CalculateAngularMomentumAroundPoint(in NodeReader nodes, float3 pos, int start, int count)
        //{
        //    var total = float3.zero;
        //    for (var b = start; b < start + count; b++)
        //    {
        //        ref var node = ref nodes.GetNode(b);
        //        var f = new ForceVector(re.mul(node.I, GetAngularVelocity(v, b)), node.m * GetVelocity(v, b));
        //        total += f.TranslateBy(pos - node.x.pos).angular;

        //    }
        //    return total;
        //}

        #region Debug
#if UNITY_EDITOR
        public static void Print(float4x4* array, int stride, int rows, int cols, bool triangle = false)
        {
            //string result = ToStr(array, stride, 0 rows, cols);
            //MonoBehaviour.print(result);
            var a = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float4x4>(array, (rows + 3) / 4 * stride, Allocator.Temp);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref a, AtomicSafetyHandle.Create());

            for (int i = 0; i < rows; i += 20)
                MonoBehaviour.print(ToStr(a, stride, i, Mathf.Min(i + 20, rows) - i, cols, triangle));
            a.Dispose();
        }
#endif
        public static void Print(NativeArray<float4x4> array, int stride, int rows, int cols, bool triangle = false)
        {
            //string result = ToStr(array, stride, 0 rows, cols);
            //MonoBehaviour.print(result);
            for (int i = 0; i < rows; i += 20)
                MonoBehaviour.print(ToStr(array, stride, i, Mathf.Min(i + 20, rows) - i, cols, triangle));
        }

        public static string ToStr(NativeArray<float4x4> array, int stride, int start, int rows, int cols, bool triangle = false)
        {
            string result = "";
            for (var i = 0; i < rows; i++)
            {
                var str = $"{array[(start + i) / 4 * stride][0][(start + i) % 4]:F3}";
                for (var j = 1; j < (triangle ? start + i + 1 : cols); j++)
                    str += $"\t{array[(start + i) / 4 * stride + j / 4][j % 4][(start + i) % 4]:F3}";
                if (i == 0)
                    result = str;
                else
                    result += "\n" + str;
            }

            return result;
        }



        public static void Print(NativeArray<float4> array)
        {
            var str = ToStr(array);
            MonoBehaviour.print(str);
        }

        public static string ToStr(NativeArray<float4> array)
        {
            var str = $"{array[0][0]:F3}";
            for (var j = 1; j < array.Length * 4; j++)
                str += $"\t{array[j / 4][j % 4]:F3}";
            return str;
        }
        #endregion

        //public void DrawJointErrors()
        //{
        //    var v = ExtractWorldVelocityCopy(World.current.v);
        //    using (var context = GetContext(World.current.bodies, v))
        //    {

        //        for (int j = 0; j < solver.nJoints; j++)
        //        {
        //            ref var joint = ref solver.joints[j];
        //            if (joint.jointType == ArticulationJointType.Linear)
        //            {
        //                var l = joint.linear;
        //                var pos = math.transform(context.GetBody(l.linkB).x, l.anchorB);
        //                JointErrors.CalculateLinearError(context, l.linkA, l.anchorA, l.linkB, l.anchorB, float3.zero, float3.zero, out var err, out var velErr);
        //                //if (math.lengthsq(err) > .1f) Debug.Break();
        //                Debug.DrawRay(pos, err, Color.red);
        //                Debug.DrawRay(pos, velErr, Color.blue);
                        
        //            }
        //        }
        //        v.Dispose();
        //    }
        //}
        public static float3 CalculateFulcrum(ForceVector f, float3 pos)
        {
            var tProj = f.angular.SetY(0); // clear torque rotation around Y
            var tLen = math.length(tProj);
            if (tLen < .000001f) return pos;
            var tDir = tProj / tLen;

            var fProj = re.ProjectOnPlane(f.linear, tDir); // project force to torque plane
            var fLen = math.length(fProj);
            if (fLen < .000001f) return pos;
            var fDir = fProj / fLen;

            // offset position to eliminate torque
            var rDir = math.normalize(math.cross(fDir, tDir));
            var rLen = tLen / fLen;
            //var r=rLen *rDir; // offset perpendicular to force direction

            // calculate offset by projecting r to ground plane along the force verctor direction
            var rDirProj = rDir.SetY(0);
            var rDirProjLen = math.length(rDirProj);
            if (rDirProjLen < .000001f) return pos; // force is nearly parallel to ground
            var rProjDir = rDirProj / rDirProjLen;
            var rFinalLen = rLen / rDirProjLen;

            var r = rFinalLen * rProjDir;

            Debug.DrawRay(pos, f.angular, Color.red);
            Debug.DrawRay(pos, f.linear, Color.green);
            var pos2 = pos + r;
            var f2 = f.TranslateBy(pos2 - pos);
            Debug.DrawRay(pos2, f2.angular, Color.yellow);
            Debug.DrawRay(pos2, f2.linear, Color.magenta);

            Debug.DrawLine(pos, pos2, Color.blue);
            return pos2;
        }

        public static void MoveToFulcrum(ref ForceVector f, ref float3 pos)
        {
            var tProj = f.angular.SetY(0); // clear torque rotation around Y
            var tLen = math.length(tProj);
            if (tLen < .000001f) return;
            var tDir = tProj / tLen;

            var fProj = re.ProjectOnPlane(f.linear, tDir); // project force to torque plane
            var fLen = math.length(fProj);
            if (fLen < .000001f) return;
            var fDir = fProj / fLen;

            // offset position to eliminate torque
            var rDir = math.normalize(math.cross(fDir, tDir));
            var rLen = tLen / fLen;
            //var r=rLen *rDir; // offset perpendicular to force direction

            // calculate offset by projecting r to ground plane along the force verctor direction
            var rDirProj = rDir.SetY(0);
            var rDirProjLen = math.length(rDirProj);
            if (rDirProjLen < .000001f) return; // force is nearly parallel to ground
            var rProjDir = rDirProj / rDirProjLen;
            var rFinalLen = rLen / rDirProjLen;
            if (rFinalLen > 1) rFinalLen = 1;// clamp

            var r = rFinalLen * rProjDir;

            //Debug.DrawRay(pos, f.angular, Color.red);
            //Debug.DrawRay(pos, f.linear, Color.green);
            //Debug.DrawRay(pos, r, Color.blue);
            pos += r;
            f = f.TranslateBy(r);

            //Debug.DrawRay(pos, f.angular, Color.yellow);
            //Debug.DrawRay(pos, f.linear, Color.magenta);
            //Debug.DrawRay(pos, re.up*f.linear.y, Color.blue);
        }
    }
}
public struct ProfilerMarkers
{
    public static ref ProfilerMarkers instance => ref sharedStatic.Data;
    public static readonly SharedStatic<ProfilerMarkers> sharedStatic = SharedStatic<ProfilerMarkers>.GetOrCreate<ProfilerMarkers>();
    bool created;
    public ProfilerMarker profileBuildBodies;
    public ProfilerMarker profileBuildJ;
    public ProfilerMarker profileBuildSparse;
    public ProfilerMarker profileSparseK;
    public ProfilerMarker profileSparseLDL;
    public ProfilerMarker profileSparseSolve;
    public ProfilerMarker profileBuildK;
    public ProfilerMarker profileLDL;
    public ProfilerMarker profileErrors;
    public ProfilerMarker profileBuildB;
    public ProfilerMarker profileSolve;
    public ProfilerMarker profileApplyImpulse;
    public ProfilerMarker profileIK;
    public ProfilerMarker profileStateMachine;
    public ProfilerMarker profileAnimator;

    public unsafe static ProfilerMarkers EnsureCreated() 
    {
        if(!instance.created )
        instance = new ProfilerMarkers()
        {
            created=true,
            profileBuildBodies = new ProfilerMarker("BuildBodies"),
            profileBuildJ = new ProfilerMarker("BuildJ"),
            profileBuildSparse = new ProfilerMarker("BuildSparsePlan"),
            profileSparseK = new ProfilerMarker("SparseK"),
            profileSparseLDL = new ProfilerMarker("SparseLDL"),
            profileSparseSolve = new ProfilerMarker("SparseSolve"),

            profileBuildK = new ProfilerMarker("BuildK"),
            profileLDL = new ProfilerMarker("LDL"),
            profileErrors = new ProfilerMarker("Errors"),
            profileBuildB = new ProfilerMarker("BuildB"),
            profileSolve = new ProfilerMarker("Solve"),
            profileApplyImpulse = new ProfilerMarker("ApplyImpulse"),
            profileIK = new ProfilerMarker("IK"),
            profileStateMachine = new ProfilerMarker("StateMachine"),
            profileAnimator = new ProfilerMarker("Animator"),
        };
        return instance;
    }
}