using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public unsafe struct ArticulationJacobian
    {
        public int nBlocks;
        public int nBodyBlocks;

        [NativeDisableUnsafePtrRestriction] public bool* Jsparsity;
        [NativeDisableUnsafePtrRestriction] public float4x4* J;
        [NativeDisableUnsafePtrRestriction] public float4x4* WJT;
        [NativeDisableUnsafePtrRestriction] public float4x4* rawWJT; // non articulated

        public ArticulationJacobian(int nBlocks, int nBodyBlocks)
        {
            this.nBlocks = nBlocks;
            this.nBodyBlocks = nBodyBlocks;
            J = Unsafe.Malloc<float4x4>(nBlocks * nBodyBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            WJT = Unsafe.Malloc<float4x4>(nBlocks * nBodyBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            rawWJT = Unsafe.Malloc<float4x4>(nBlocks * nBodyBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Jsparsity = Unsafe.Malloc<bool>(nBlocks * nBodyBlocks, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        }
        public void Dispose()
        {
            Unsafe.Free(J, Allocator.Persistent);
            Unsafe.Free(WJT, Allocator.Persistent);
            Unsafe.Free(rawWJT, Allocator.Persistent);
            Unsafe.Free(Jsparsity, Allocator.Persistent);
        }

        // methods to fill JT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDiagonal(int r, int b, int off, float3 j)
        {
            var jtBlock = WJT + (b * 2 + off) * nBlocks + r / 4;
            var jCell = ((float4*)jtBlock + r % 4);
            *jCell++ = new float4(j.x, 0, 0, 0);
            *jCell++ = new float4(0, j.y, 0, 0);
            *jCell = new float4(0, 0, j.z, 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMatrix(int r, int b, int off, float3x3 j)
        {
            var jt = math.transpose(j);
            var jtBlock = WJT + (b * 2 + off) * nBlocks + r / 4;
            var jCell = ((float4*)jtBlock + r % 4);
            *jCell++ = new float4(jt.c0, 0);
            *jCell++ = new float4(jt.c1, 0);
            *jCell = new float4(jt.c2, 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteRow(int r, int b, int off, in float3 j)
        {
            var jtBlock = WJT + (b * 2 + off) * nBlocks + r / 4;
            var jCell = ((float4*)jtBlock + r % 4);
            *jCell = new float4(j, 0);
        }

        // WJT holds just JT after fill jacobian phase, calculate J and full WJT
        //public void Finalize<T>(in T bodies) where T: IGetBody
        //{
        //    //TODO: replace with CalculateWJT
        //    for (var r = 0; r < nBlocks; r++)
        //        for (var b = 0; b < nBodyBlocks / 2; b++)
        //        {
        //            ref var body = ref bodies.GetBody(b);
        //            if (Jsparsity[r * nBodyBlocks + b * 2 + 0])
        //            {
        //                J[b * 2 + 0 + r * nBodyBlocks] = math.transpose(WJT[(b * 2 + 0) * nBlocks + r]);
        //                WJT[(b * 2 + 0) * nBlocks + r] = math.mul(body.invI.ToFloat4x4(), WJT[(b * 2 + 0) * nBlocks + r]);
        //            }
        //            if (Jsparsity[r * nBodyBlocks + b * 2 + 1])
        //            {
        //                J[b * 2 + 1 + r * nBodyBlocks] = math.transpose(WJT[(b * 2 + 1) * nBlocks + r]);
        //                WJT[(b * 2 + 1) * nBlocks + r] = body.invM * WJT[(b * 2 + 1) * nBlocks + r];
        //            }
        //        }
        //}

        public void CalculateWJT<T>(in T context) where T : ISolverContext
        {
            for (var r = 0; r < nBlocks; r++)
                for (var b = 0; b < nBodyBlocks / 2; b++)
                {
                    context.GetInverseRigidInertia(b, out var invM, out var invI);
                    if (Jsparsity[r * nBodyBlocks + b * 2 + 0])
                    {
                        J[b * 2 + 0 + r * nBodyBlocks] = math.transpose(WJT[(b * 2 + 0) * nBlocks + r]);
                        rawWJT[(b * 2 + 0) * nBlocks + r] = math.mul(invI.ToFloat4x4(), WJT[(b * 2 + 0) * nBlocks + r]);
                    }
                    if (Jsparsity[r * nBodyBlocks + b * 2 + 1])
                    {
                        J[b * 2 + 1 + r * nBodyBlocks] = math.transpose(WJT[(b * 2 + 1) * nBlocks + r]);
                        rawWJT[(b * 2 + 1) * nBlocks + r] = invM * WJT[(b * 2 + 1) * nBlocks + r];
                    }
                    if (context.isArticulation(b))
                    {

                        ref var inv = ref context.GetInverseArticulatedInertia(b);
                        var invH = new float4x4(inv.H, float3.zero);
                        var JT0 = WJT[(b * 2 + 0) * nBlocks + r];
                        var JT1 = WJT[(b * 2 + 1) * nBlocks + r];
                        if (Jsparsity[r * nBodyBlocks + b * 2 + 0])
                        {
                            WJT[(b * 2 + 0) * nBlocks + r] = 
                                math.mul(inv.I.ToFloat4x4(), JT0)+
                                math.mul(invH, JT1);
                            
                        }
                        if (Jsparsity[r * nBodyBlocks + b * 2 + 1])
                        {
                            WJT[(b * 2 + 1) * nBlocks + r] =
                                math.mul(math.transpose(invH), JT0) +
                                math.mul(inv.M.ToFloat4x4(), JT1);
                        }
                    }
                    else
                    {
                        if (Jsparsity[r * nBodyBlocks + b * 2 + 0])
                        {
                            WJT[(b * 2 + 0) * nBlocks + r] = rawWJT[(b * 2 + 0) * nBlocks + r];
                        }
                        if (Jsparsity[r * nBodyBlocks + b * 2 + 1])
                        {
                            WJT[(b * 2 + 1) * nBlocks + r] = rawWJT[(b * 2 + 1) * nBlocks + r];
                        }

                    }
                }
        }
    }
    public unsafe struct JacobianSparsityWriter
    {
        int nBodyBlocks;
        bool* Jsparsity;
        public JacobianSparsityWriter(int nBodyBlocks, bool* Jsparsity)
        {
            this.nBodyBlocks = nBodyBlocks;
            this.Jsparsity = Jsparsity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int row, int nRows, int b, bool angular, bool linear)
        {
            var rBlock = row / 4;
            if (angular) Jsparsity[rBlock * nBodyBlocks + b * 2 + 0] = true;
            if (linear) Jsparsity[rBlock * nBodyBlocks + b * 2 + 1] = true;
            if (row % 4 + nRows > 4)
            {
                if (angular) Jsparsity[(rBlock + 1) * nBodyBlocks + b * 2 + 0] = true;
                if (linear) Jsparsity[(rBlock + 1) * nBodyBlocks + b * 2 + 1] = true;
            }
        }
    }

}