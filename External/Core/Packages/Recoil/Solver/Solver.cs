using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public unsafe partial struct Solver
    { 
        // definition
        public int nLinks;
        public int nJoints;
        [NativeDisableUnsafePtrRestriction] public int* links;
        //[NativeDisableUnsafePtrRestriction] public ArticulationJoint* joints;
        public ArticulationJointArray joints;
        // dimensions of J
        public int nRows;
        int nColumns;

        // dimensions of block J
        public int nBlocks;
        public int nBodyBlocks;

        [NativeDisableUnsafePtrRestriction] public float4* impulse4;
        [NativeDisableUnsafePtrRestriction] public float4* gamma;
        [NativeDisableUnsafePtrRestriction] public float4* bias;
        public float h;
        public ArticulationJacobian jacobian;
        public SparseLDL sparseLDL;
        [NativeDisableUnsafePtrRestriction] public Velocity4* biasDV; // body velocities


        public unsafe void Allocate(int[] links, ArticulationJoint[] joints)
        {
            h = Time.fixedDeltaTime;
            // count jacobian rows
            nRows = 0;
            for (int j = 0; j < joints.Length; j++)
            {
                var joint = joints[j];
                joint.row = nRows;
                joints[j] = joint;
                nRows += joint.rowCount;
            }

            nJoints = joints.Length;
            nBlocks = (nRows + 3) / 4; // pad
            nLinks = links.Length;
            nColumns = nLinks * 8;
            nBodyBlocks = (nColumns + 3) / 4;


            this.links = Unsafe.Malloc<int>(links, Allocator.Persistent);

            this.joints = new ArticulationJointArray(joints, Allocator.Persistent);
            impulse4 = Unsafe.Malloc<float4>(nBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            gamma = Unsafe.Malloc<float4>(nBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            bias = Unsafe.Malloc<float4>(nBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            biasDV = Unsafe.Malloc<Velocity4>(nLinks, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            jacobian = new ArticulationJacobian(nBlocks, nBodyBlocks);
            sparcityWriter = new JacobianSparsityWriter(nBodyBlocks, jacobian.Jsparsity);
            sparseLDL = new SparseLDL();
            ProfilerMarkers.EnsureCreated();
        }
      
        public unsafe void Dispose()
        {
            Unsafe.Free(links, Allocator.Persistent);
            joints.Dispose();
            Unsafe.Free(impulse4, Allocator.Persistent);
            Unsafe.Free(gamma, Allocator.Persistent);
            Unsafe.Free(bias, Allocator.Persistent);
            Unsafe.Free(biasDV, Allocator.Persistent);

            sparseLDL.Dispose();
            jacobian.Dispose();
        }


        public SolverBodies GetBodies()
        {
            return new SolverBodies(links, nLinks) ;
        }

        public int GetBodyId(int idx)
        {
            if (idx < 0) return -1;
            Unsafe.CheckIndex(idx, nLinks);
            return links[idx];
        }
        public ref T GetJoint<T>(int idx) where T : unmanaged => ref joints.GetJoint<T>(idx);

        public JacobianSparsityWriter sparcityWriter;
        public void MarkJacobian(int startRow, int nRows, int link, bool angular, bool linear)
        {                
            
                         
            sparcityWriter.Set(startRow, nRows, link, angular, linear);
        }

        public void WriteDiagonal(int r, int b, int off, float3 j) => jacobian.WriteDiagonal(r, b, off, j);
        public void WriteMatrix(int r, int b, int off, float3x3 j) => jacobian.WriteMatrix(r, b, off, j);
        public void WriteRow(int r, int b, int off, float3 j) => jacobian.WriteRow(r, b, off, j);

    
        public unsafe void WriteError(int startRow, Spring spring, float err, float velErr)
        {
            var pGamma = (float*)this.gamma;
            var pBias = (float*)this.bias;
            spring.CalculateSpring(err, velErr, h, out pGamma[startRow], out pBias[startRow]); startRow++;
        }
        public unsafe void WriteError(int startRow, Spring spring, float2 err, float2 velErr)
        {
            var pGamma = (float*)this.gamma;
            var pBias = (float*)this.bias;
            spring.CalculateSpring(err.x, velErr.x, h, out pGamma[startRow], out pBias[startRow]); startRow++;
            spring.CalculateSpring(err.y, velErr.y, h, out pGamma[startRow], out pBias[startRow]); startRow++;
        }
        public unsafe void WriteError(int startRow, Spring spring, float3 err, float3 velErr)
        {
            var pGamma = (float*)this.gamma;
            var pBias = (float*)this.bias;
            spring.CalculateSpring(err.x, velErr.x, h, out pGamma[startRow], out pBias[startRow]); startRow++;
            spring.CalculateSpring(err.y, velErr.y, h, out pGamma[startRow], out pBias[startRow]); startRow++;
            spring.CalculateSpring(err.z, velErr.z, h, out pGamma[startRow], out pBias[startRow]); startRow++;
        }



        public unsafe void BuildJacobians<T>(in T context) where T:ISolverContext
        {

            // copy velocities from world

            // heavier ball - more stable (1- very dynamic, 4 - more or less on rails) 
#if HEAVY_BALL
            ref var body0 = ref nodeReader.GetNode(0);
            body0.m *= 1.5f; node0.invM /= 1.5f;
#endif
            var profiler = ProfilerMarkers.instance;
            profiler.profileBuildJ.Begin();
            BuildJ(context);

            profiler.profileBuildJ.End();

            if (!sparseLDL.isValid)
            {
                profiler.profileBuildSparse.Begin();
                SparseLDL.BuildPlan(jacobian, out sparseLDL);
                profiler.profileBuildSparse.End();
            }

            profiler.profileSparseK.Begin();

            SparseLDL.BuildK(sparseLDL, jacobian, nRows);
            SparseLDL.AddGamma(sparseLDL, gamma);

            //if (debug)
            //{


            //    var K = new NativeArray<float4x4>(nBlocks * nBlocks, Allocator.Temp);
            //    sparseLDL.Unpack(K);
            //    Articulation.Print(K, nBlocks, nBlocks * 4, nBlocks * 4);
            //    K.Dispose();
            //}

            profiler.profileSparseK.End();

            profiler.profileSparseLDL.Begin();
            SparseLDL.LDL(ref sparseLDL);
            profiler.profileSparseLDL.End();

            for (var r = 0; r < nBlocks; r++)
                impulse4[r] = float4.zero;
        }

        public unsafe void RecalculateErrors<T>(in T context) where T : ISolverContext
        {
            var r = 0;

            for (int j = 0; j < nJoints; j++)
            {
                ref var joint = ref joints.GetJoint(j);
                joint.CalculateErrors(this, context, r);
                r += joint.rowCount;
            }

        }

        private unsafe int BuildJ<T>(in T context) where T : ISolverContext
        {
            var r = 0;

            for (int j = 0; j < nJoints; j++)
            {
                ref var joint = ref joints.GetJoint(j);
                joint.FillJacobianSparcity(this, r);
                joint.FillJacobian(this, context, r);
                joint.CalculateErrors(this, context, r);
                r += joint.rowCount;
            }

            //if(debug)
            //    Articulation.Print(jacobian.WJT, nBlocks, nBodyBlocks * 4,r);

            // transpose J
            //jacobian.Finalize(context);
            jacobian.CalculateWJT(context);

            //if (debug)
            //    Articulation.Print(jacobian.WJT, nBlocks, nBodyBlocks * 4, r);
            //Debug.Log($"{nBodyBlocks} {nRows}");

            return r;
        }
        public void VelocityIterationBias<T>(in T context) where T : ISolverContext
        {
            for (int b = 0; b < nLinks; b++)
                context.AddVelocity(b, -biasDV[b]);
            VelocityIterationNoBias(context);
            for (int b = 0; b < nLinks; b++)
                context.AddVelocity(b, biasDV[b]);
        }
        public void VelocityIterationNoBias<T>(in T context) where T : ISolverContext
        {
            VelocityIteration(context,true);
        }

        //public static bool debug;

        public unsafe void VelocityIteration<T>(in T context, bool accumulateImpulse) where T : ISolverContext
        {
            var profiler = ProfilerMarkers.instance;
            var x = new NativeArray<float4>(nBlocks, Allocator.Temp);

            RecalculateErrors(context);

            profiler.profileBuildB.Begin();
            CalculateB(nBlocks, gamma, bias, impulse4, x);
            var pv = (float4*)context.GetVelocity4(0).AsPointer(); // access pointer from reference to 0 element
            AddJv(jacobian, pv, x);
            profiler.profileBuildB.End();

            profiler.profileSparseSolve.Begin();
            SparseLDL.MulInvDecomposed(sparseLDL, x);
            profiler.profileSparseSolve.End();


            profiler.profileApplyImpulse.Begin();
            ApplyNegativeDeltaImpulse(context, jacobian, x);

            if (accumulateImpulse)
                for (var r = 0; r < nBlocks; r++)
                    impulse4[r] -= x[r];

            //if (debug) 
            //    DebugImpulses(context, (float*)impulse4);

            x.Dispose();
            profiler.profileApplyImpulse.End();
        }

        private readonly void DebugImpulses<T>(T context, float* pimp) where T : ISolverContext
        {
            var row = 0;
            for (int j = 0; j < nJoints; j++)
            {
                ref var joint = ref joints.GetJoint(j);
                switch (joint.jointType)
                {
                    case ArticulationJointType.Angular:

                        Debug.DrawRay(context.GetBody(joint.angular.link).x.pos,
                            new float3(pimp[row + 0], pimp[row + 1], pimp[row + 2]), Color.red);


                        break;
                    case ArticulationJointType.Linear:
                        {

                            var l = new float3(pimp[row + 0], pimp[row + 1], pimp[row + 2]);
                            float3 pos = context.TransformPoint(joint.linear.link, joint.linear.anchor);
                            var nextJoint = joints.GetJoint(j + 1);
                            if (nextJoint.jointType == ArticulationJointType.Angular3)
                            {
                                var angular3 = joints.GetJoint<Angular3ArticulationJoint>(j + 1);
                                var a = math.mul(context.GetBody(joint.linear.link).x.rot, angular3.axisX * pimp[row + 3]) +
                                    math.mul(context.GetBody(joint.linear.link).x.rot, angular3.axisY * pimp[row + 4]) +
                                    math.mul(context.GetBody(joint.linear.link).x.rot, angular3.axisZ * pimp[row + 5]);
                                Debug.DrawRay(pos, a, Color.red);
                            }
                            else
                            {
                                //var a = new float3(pimp[row + 3], pimp[row + 4], pimp[row + 5]);
                            }
                            Debug.DrawRay(pos, l, Color.blue);

                            //Articulation.CalculateFulcrum(new ForceVector(a, l), pos);
                            break;
                        }
                }
                row += joint.rowCount;
            }
        }

        static void CalculateB(int nBlocks, float4* gamma, float4* bias, 
            float4* impulse, NativeArray<float4> x)
        {
            for (int r = 0; r < nBlocks; r++)
                x[r] =  bias[r] + gamma[r] * impulse[r];
        }
        static void AddJv(in ArticulationJacobian jacobian, float4* v,
            NativeArray<float4> x)
        {
            var nBlocks = jacobian.nBlocks;
            var nBodyBlocks = jacobian.nBodyBlocks;
            var J = jacobian.J;
            var WJT = jacobian.WJT;
            var Jsparsity = jacobian.Jsparsity;

            for (int r = 0; r < nBlocks; r++)
            {
                var b = x[r];
                // Add Jv
                for (int c = 0; c < nBodyBlocks; c++)
                    if (Jsparsity[r * nBodyBlocks + c])
                        b += math.mul(J[r * nBodyBlocks + c], v[c]);
                x[r] = b;
            }
        }


        public static void ApplyNegativeDeltaImpulse<T>(in T context, in ArticulationJacobian jacobian,
                    NativeArray<float4> deltaImpulse) where T:ISolverContext
        {
            var nBlocks = jacobian.nBlocks;
            var nBodyBlocks = jacobian.nBodyBlocks;
            var J = jacobian.J;
            var rawWJT = jacobian.rawWJT;
            var Jsparsity = jacobian.Jsparsity;

            for (int b = 0; b < nBodyBlocks / 2; b++)
            {
                // rigidbody uses WJT*lambda as deltaV
                for (int r = 0; r < nBlocks; r++)
                {

                    var sa = Jsparsity[r * nBodyBlocks + b * 2 + 0];
                    var sl = Jsparsity[r * nBodyBlocks + b * 2 + 1];
                    if (sa || sl)
                    {
#if NBG_RECOIL_DEBUG
                        re.AssertFinite(deltaImpulse[r]); // NOTE: this causes Burst BC1371 warnings
#endif
                        ref var v = ref context.GetVelocity4(b);
                        if (sa) v.angular -= math.mul(rawWJT[(b * 2 + 0) * nBlocks + r], deltaImpulse[r]);
                        if (sl) v.linear -= math.mul(rawWJT[(b * 2 + 1) * nBlocks + r], deltaImpulse[r]);
                    }
                }

            }
        }

        public NativeArray<Velocity4> ExtractWorldVelocityCopy()
        {
            var world = World.main;
            var v = new NativeArray<Velocity4>(nLinks, Allocator.Temp);
            for (int i = 0; i < nLinks; i++)
                v[i] = world.GetVelocity4(links[i]);
            return v;
        }
        public void WriteAndDisposeVelocityCopy(NativeArray<Velocity4> v)
        {
            var world = World.main;
            for (int i = 0; i < nLinks; i++)
                world.SetVelocity4(links[i], v[i]);
            v.Dispose();
        }
    }
}
