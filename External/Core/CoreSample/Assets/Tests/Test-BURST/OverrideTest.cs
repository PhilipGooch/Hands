using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile(CompileSynchronously = true)]
public class OverrideTest : MonoBehaviour
{
    const int opcount = 1000000;

    delegate void MatrixFunction(in float4x4 a, out float4x4 result);


    [BurstCompile(CompileSynchronously = true)]
    private struct FunctionPointerInverse : IJob
    {
        public FunctionPointer<MatrixFunction> fn;
        public NativeArray<float4x4> result;
        

        public void Execute()
        {
            result[0] = float4x4.zero;
            var m4 = new float4x4(
                .35f, .18f, .01f, 0.02f,
                .84f, .31f, .67f, 0.56f,
                .11f, .96f, .45f, 0.12f,
                .79f, .35f, .23f, 0.72f);
            for (int i = 0; i < opcount; i++)
            {
                fn.Invoke(m4, out var r);
                result[0] += r;
            }
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct DirectInverse : IJob
    {
        public NativeArray<float4x4> result; 


        public void Execute()
        {
            result[0] = float4x4.zero;
            var m4 = new float4x4(
                .35f, .18f, .01f, 0.02f,
                .84f, .31f, .67f, 0.56f,
                .11f, .96f, .45f, 0.12f,
                .79f, .35f, .23f, 0.72f);
            for (int i = 0; i < opcount; i++)
            {
                invert(m4, out var r);
                result[0] += r;
            }
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    static void invert(in float4x4 m, out float4x4 res) => res=math.inverse(m);

    IEnumerator Start()
    {
        //var input = new NativeArray<float>(10, Allocator.Persistent);
        var output = new NativeArray<float4x4>(1, Allocator.Persistent);
        //for (int i = 0; i < input.Length; i++)
        //    input[i] = 1.0f * i;

        for (int i = 0; i < 10; i++)
        {
            var job1 = new FunctionPointerInverse() { result = output, fn = BurstCompiler.CompileFunctionPointer<MatrixFunction>(invert) };
        
            var job2 = new DirectInverse() { result = output };
            var watch1 = System.Diagnostics.Stopwatch.StartNew();
            job1.Schedule().Complete();
            watch1.Stop();
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            job2.Schedule().Complete();
            watch2.Stop();

            Debug.Log($"Pointer: {1000000 * watch1.Elapsed.TotalMilliseconds / opcount} Direct: {1000000 * watch2.Elapsed.TotalMilliseconds / opcount}");
            yield return null;
        }
        output.Dispose();
    }
}
