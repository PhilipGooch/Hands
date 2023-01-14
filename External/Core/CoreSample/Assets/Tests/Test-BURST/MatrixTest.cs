using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MatrixTest : MonoBehaviour
{
    const int opcount = 10000000;
    [BurstCompile(CompileSynchronously = true)]
    private struct MeasureInverse4 : IJob
    {
        public float4x4 result;

        public void Execute()
        {
            var res = float4x4.zero;
            var m4 = new float4x4(
                .35f, .18f, .01f, 0.02f,
                .84f, .31f, .67f, 0.56f,
                .11f, .96f, .45f, 0.12f,
                .79f, .35f, .23f, 0.72f);
            for (int i = 0; i < opcount; i++)
                res+=math.inverse(m4);
            result = res;
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct MeasureInverse3 : IJob
    {
        public float3x3 result;

        public void Execute()
        {
            var res = float3x3.zero;
            var m3 = new float3x3(
                .35f, .18f, .01f,
                .84f, .31f, .67f,
                .11f, .96f, .45f);
            for (int i = 0; i < opcount; i++)
                res += math.inverse(m3);
            result = res;
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct MeasureInverse2 : IJob
    {
        public float2x2 result;

        public void Execute()
        {
            var res = float2x2.zero;
            var m3 = new float2x2(
                .84f, .67f,
                .11f, .45f);
            for (int i = 0; i < opcount; i++)
                res += math.inverse(m3);
            result = res;
        }
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        var input = new NativeArray<float>(10, Allocator.Persistent);
        var output = new NativeArray<float>(1, Allocator.Persistent);
        for (int i = 0; i < input.Length; i++)
            input[i] = 1.0f * i;

        for (int i = 0; i < 10; i++)
        {
            var job4 = new MeasureInverse4();
            var job3 = new MeasureInverse3();
            var job2 = new MeasureInverse2();
            var watch4 = System.Diagnostics.Stopwatch.StartNew();
            job4.Schedule().Complete();
            watch4.Stop();
            var watch3 = System.Diagnostics.Stopwatch.StartNew();
            job3.Schedule().Complete();
            watch3.Stop();
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            job2.Schedule().Complete();
            watch2.Stop();

            Debug.Log($"4x4 1M: {1000000* watch4.Elapsed.TotalMilliseconds/opcount} 3x3 1M: {1000000 * watch3.Elapsed.TotalMilliseconds / opcount} 2x2 1M: {1000000 * watch2.Elapsed.TotalMilliseconds / opcount}");
            yield return null;
        }
        input.Dispose();
        output.Dispose();
    }

   
}

