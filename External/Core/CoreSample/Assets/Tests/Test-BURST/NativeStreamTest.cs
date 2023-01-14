//using System.Collections;
//using System.Collections.Generic;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;

////[BurstCompile(CompileSynchronously = true)]
////public unsafe struct PointerJob : IJob
////{
////    public float4* pointer;
////    public void Execute()
////    {

////        *pointer = new float4(1, 2, 3, 4);
////    }
////}
//[BurstCompile(CompileSynchronously = true)]
//public struct StreamJob : IJobParallelFor
//{
//    public int start;
//    [NativeDisableParallelForRestriction]
//    public NativeStream.Writer Writer;
//    public unsafe void Execute(int index)
//    {
//        Writer.BeginForEachIndex(start+ index);
//        var ptr = (float4*)Writer.Allocate(sizeof(float4) * 10);
//        *ptr = new float4(1, 2, 3, 4);
//        Writer.EndForEachIndex();
//        //for (int j = 0; j < 2; j++)
//        //{

//        //    Writer.BeginForEachIndex(index * 2 + j);
//        //    for (int i = 0; i != index * 2 + j; i++)
//        //    {
//        //        Writer.Write(i);
//        //        var b = Writer.Allocate<SolverBody>();
//        //        b.m = i;

//        //    }
//        //    Writer.EndForEachIndex();
//        //}
//    }
//}

//public struct StreamBlock
//{
//}

//public class NativeStreamTest : MonoBehaviour
//{
//    // Start is called before the first frame update
//    unsafe void Start()
//    {

//        var stream = new NativeStream(10, Allocator.TempJob);
//        var fillInts = new StreamJob { Writer = stream.AsWriter(), start=2 };
//        var jobHandle = fillInts.Schedule(3, 1);
//        ////var jobHandle = IJobParallelForDeferExtensions.Schedule(fillInts,.Schedule(10, 1);
//        ////var compareInts = new ReadInts { Reader = stream.AsReader() };
//        ////var res0 = compareInts.Schedule(count, batchSize, jobHandle);
//        ////var res1 = compareInts.Schedule(count, batchSize, jobHandle);
//        ////res0.Complete();
//        ////res1.Complete();
//        jobHandle.Complete();
//        var writer = stream.AsWriter();
//        writer.BeginForEachIndex(0);
//        var ptr = (float4*)writer.Allocate(sizeof(float4) * 10);
//        *ptr = new float4(1, 2, 3, 4);
//        writer.EndForEachIndex();


//        var reader = stream.AsReader();
//        reader.BeginForEachIndex(2);
//        var ptr2 = (float4*)reader.ReadUnsafePtr(sizeof(float4) * 10);
//        var a = *ptr2;
//        reader.EndForEachIndex();
//        stream.Dispose();

//        //var array = new NativeArray<float4>(1, Allocator.Persistent);
//        //var job = new PointerJob();
//        //job.pointer = (float4*) Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(array);
//        //job.Schedule().Complete();
//        //array.Dispose();

//    }

//    // Update is called once per frame
//    void Update()
//    {
        
//    }
//}
