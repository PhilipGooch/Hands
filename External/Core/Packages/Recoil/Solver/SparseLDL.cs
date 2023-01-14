using NBG.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{

    public unsafe struct SparseLDL
    {
        int nBlocks;
        int nK;
        [NativeDisableUnsafePtrRestriction] float4x4* K; // packed K
        [NativeDisableUnsafePtrRestriction] int* rows; // unpacking info
        [NativeDisableUnsafePtrRestriction] int* heights; // heights of packed columns
        [NativeDisableUnsafePtrRestriction] int* jwjtPlan; // a list of JWJT multiplications for each cell of packed K
        [NativeDisableUnsafePtrRestriction] int* rsi; // reduction scattering index

        public bool isValid => K != null;


        public void Dispose()
        {
            if (K != null)
            {
                Unsafe.Free(K, Allocator.Persistent);
                Unsafe.Free(rows, Allocator.Persistent);
                Unsafe.Free(heights, Allocator.Persistent);
                Unsafe.Free(jwjtPlan, Allocator.Persistent);
                Unsafe.Free(rsi, Allocator.Persistent);
            }
        }

        // constructs J*W*J^-1 multiplication plan consisting based on nonempty jacobian elements, marks non empty cells of K
        public static void BuildPlan(in ArticulationJacobian jacobian,
            out SparseLDL sparseLDL)
        {
            sparseLDL = new SparseLDL();
            sparseLDL.nBlocks = jacobian.nBlocks;
            BuildPlan(jacobian.Jsparsity, jacobian.nBlocks, jacobian.nBodyBlocks, out sparseLDL.nK, out sparseLDL.K, out sparseLDL.heights, out sparseLDL.rows, out sparseLDL.jwjtPlan, out sparseLDL.rsi);
        }
        public static void BuildPlan(bool* Jsparsity, int nBlocks, int nBodyBlocks,
            out int _nK, out float4x4* _K,
            out int* heights, out int* _rows, out int* _jwjtPlan, out int* _rsi)
        {
            heights = Unsafe.Malloc<int>(nBlocks, Allocator.Persistent);
            var rows = new NativeList<int>(nBlocks, Allocator.Temp);
            var jwjtPlan = new NativeList<int>(nBlocks, Allocator.Temp);
            var rsi = new NativeList<int>(nBlocks, Allocator.Temp);


            var map = new NativeArray<int>(nBlocks * nBlocks, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            // clear map - packed columns
            for (int c = 0; c < nBlocks; c++)
                for (int r = c; r < nBlocks; r++)
                    map[c * nBlocks + r] = -1;

            var nK = 0;
            //var nRsi = 0;
            //var nKplan = 0;
            for (int c = 0; c < nBlocks; c++)
            {
                var h = 0;

                for (int r = c; r < nBlocks; r++)
                {
                    // J * W * JT
                    bool filled = map[c * nBlocks + r] >= 0;
                    for (int k = 0; k < nBodyBlocks; k++)
                        if (Jsparsity[r * nBodyBlocks + k] && Jsparsity[c * nBodyBlocks + k])
                        {
                            filled = true;
                            jwjtPlan.Add(r * nBodyBlocks + k);
                            jwjtPlan.Add(k * nBlocks + c);
                        }
                    if (filled)
                    {
                        jwjtPlan.Add(-1); // add terminator to WJWT plan
                        rows.Add(r);
                        map[c * nBlocks + r] = nK++; // add map
                        h++;

                        // for each filled row above us mark r,r2 as to be filled in by LDL
                        for (int r2 = c + 1; r2 <= r; r2++)
                            if (map[c * nBlocks + r2] >= 0)
                            {
                                map[r2 * nBlocks + r] = 0; // include in packing
                                rsi.Add(r2 * nBlocks + r);
                            }
                    }
                }
                heights[c] = h;
            }

            var rsiLen = rsi.Length;
            for (int i = 0; i < rsiLen; i++)
                rsi[i] = map[rsi[i]];

            //state[0] = new SparseLDLState() { nK = nK};
            map.Dispose();

            _nK = nK;
            _rows = Unsafe.Malloc<int>(rows, Allocator.Persistent);
            _jwjtPlan = Unsafe.Malloc<int>(jwjtPlan, Allocator.Persistent);
            _rsi = Unsafe.Malloc<int>(rsi, Allocator.Persistent);
            _K = Unsafe.Malloc<float4x4>(nK, Allocator.Persistent);

            rows.Dispose();
            jwjtPlan.Dispose();
            rsi.Dispose();
        }

        public static void BuildK(in SparseLDL sparseLDL, in ArticulationJacobian jacobian, int usedRows)
        {
            BuildK(sparseLDL.K, sparseLDL.nK, sparseLDL.nBlocks, sparseLDL.heights, sparseLDL.jwjtPlan, jacobian.J, jacobian.WJT, usedRows);
        }

        // creates packed contraint matrix based on previously calculated plan
        public static void BuildK(float4x4* K, int nK, int nBlocks, int* heights, int* jwjtPlan, float4x4* J, float4x4* WJT, int usedRows)
        {
            var k = 0;
            var p = 0;
            while (k < nK)
            {
                var cell = float4x4.zero;
                while (jwjtPlan[p] >= 0)
                    cell += math.mul(J[jwjtPlan[p++]], WJT[jwjtPlan[p++]]);
                K[k++] = cell;
                p++;
            }

            // pad with diagonal 1 where there are no actual rows (J will be empty there and K won't be invertible)
            var pad = nBlocks * 4 - usedRows;
            ref var lastK = ref K[k - 1];
            for (var r = 3; r > 3 - pad; r--)
                lastK[r % 4][r % 4] = 1;
        }
        public static unsafe void AddGamma(in SparseLDL sparseLDL, float4* gamma)
        {
            AddGamma(sparseLDL.K, sparseLDL.nBlocks, sparseLDL.heights, gamma);
        }
        public static unsafe void AddGamma(float4x4* K, int nBlocks, int* heights, float4* gamma)
        {
            var a = K;
            for (int c = 0; c < nBlocks; c++)
            {
                var g = gamma[c];
                *((float*)a + 0 * 5) += g[0];
                *((float*)a + 1 * 5) += g[1];
                *((float*)a + 2 * 5) += g[2];
                *((float*)a + 3 * 5) += g[3];
                a += heights[c];
            }
        }

        public static unsafe void LDL(ref SparseLDL sparseLDL)//NativeArray<float4x4> K, NativeArray<float4x4> LTemp, NativeArray<int> heights, NativeArray<int> rsi, int nBlocks)
        {
            LDL(sparseLDL.K, sparseLDL.nBlocks, sparseLDL.heights, sparseLDL.rsi);
        }

        public static unsafe void LDL(float4x4* K, int nBlocks, int* heights, int* rsi)//NativeArray<float4x4> K, NativeArray<float4x4> LTemp, NativeArray<int> heights, NativeArray<int> rsi, int nBlocks)
        {
            var LTemp = new NativeArray<float4x4>(nBlocks - 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var idxRsi = 0;
            var N = K;
            for (int c = 0; c < nBlocks; c++)
            {
                var colHeight = heights[c];
                var E = N + 1;
                var eHeight = colHeight - 1;

                var invN = re.safeinverse(*N);

                // L*,i
                for (var i = 0; i < eHeight; i++)
                    LTemp[i] = math.mul(*(E + i), invN);

                for (var i = 0; i < eHeight; i++)
                    for (var j = 0; j <= i; j++)
                        K[rsi[idxRsi++]] -= math.mul(*(E + i), math.transpose(LTemp[j]));

                for (var i = 0; i < eHeight; i++)
                    *(E + i) = LTemp[i];

                N += colHeight;
            }
            LTemp.Dispose();
        }

        public static void MulInvDecomposed(in SparseLDL sparseLDL, NativeArray<float4> x)
        {
            MulInvDecomposed(sparseLDL.K, sparseLDL.nBlocks, sparseLDL.heights, sparseLDL.rows, x);
        }
        public static void MulInvDecomposed(float4x4* K, int nBlocks, int* heights, int* rows, NativeArray<float4> x)
        {
            // L^-1
            var k = 0;
            for (int c = 0; c <= nBlocks - 2; c++)
            {
                var height = heights[c];
                for (var i = 1; i < height; i++)
                {
                    var r = rows[k + i];
                    x[r] -= math.mul(K[k + i], x[c]);
                }
                k += height;
            }
            // invD
            k = 0;
            for (int i = 0; i < nBlocks; i++)
            {
                var height = heights[i];
                x[i] = math.mul(re.safeinverse(K[k]), x[i]);
                k += height;
            }

            // L^-t
            //k = state[0].nK - 1; // second to last column of K
            k--; // skip last column
            for (int c = nBlocks - 2; c >= 0; c--)
            {
                var height = heights[c];
                k -= height;
                for (var i = 1; i < height; i++)
                {
                    var r = rows[k + i];
                    x[c] -= math.mul(math.transpose(K[k + i]), x[r]);
                }
            }
        }

        public void Unpack(NativeArray<float4x4> target, bool triangle = true)
        {
            var k = 0;
            for (int c = 0; c < nBlocks; c++)
            {
                var height = heights[c];
                for (var i = 0; i < height; i++)
                {
                    var r = rows[k + i];
                    target[r * nBlocks + c] = K[k + i];
                    if (!triangle)
                        target[c * nBlocks + r] = K[k + i];
                }
                k += height;
            }
        }

    }
}
