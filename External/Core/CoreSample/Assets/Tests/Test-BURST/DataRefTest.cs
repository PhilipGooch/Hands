using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Recoil;
using System;
using NBG.Entities;
using NBG.Unsafe;

// Looking for a way to access a component embedded in unknown structure (offset)
// - Direct - is a baseline where structure is known
// - RefAccess is a cluttered, interface based implementation 
// - Offset uses readonly static to store offsets
// - Shared uses SharedStatic to store offset 
// - Cached uses reads offsets from job 

// Best candidate: Shared - is the cleanest, most versatile code without noticeable performance loss


public class DataRefTest : MonoBehaviour
{

    const int opcount = 1000000;
    public struct CompA
    {
        public int a;
    }
    public struct CompB
    {
        public int b;
    }

    public struct EntityAB
    {
        public CompA a;
        public CompB b;
    }


        internal sealed class SharedComponentOffset<TContainer, TComponent>
        {
            public static readonly SharedStatic<int> Ref = SharedStatic<int>.GetOrCreate<TContainer, TComponent>();
        }

    internal sealed class ComponentOffset<TContainer, TComponent>
    {
        public static readonly int offset;
        static ComponentOffset()
        {
            offset = CalculateOffset();
        }
        //= CalculateOffset();

        private static int CalculateOffset()
        {
            
            if(BurstRuntime.GetHashCode64<TContainer>() == BurstRuntime.GetHashCode64<EntityAB>())
            {
                if (BurstRuntime.GetHashCode64<TComponent>() == BurstRuntime.GetHashCode64<CompA>())
                    return 0;
                if (BurstRuntime.GetHashCode64<TComponent>() == BurstRuntime.GetHashCode64<CompB>())
                    return 4;

            }
            return 0;
        }
    }



    public interface IRefAccess<TContainer,TComponent>
    {
        ref TComponent Get(ref TContainer entity);
    }
    public struct GetCompA : IRefAccess<EntityAB, CompA>
    {
        public ref CompA Get(ref EntityAB entity) => ref entity.a;
    }
    public struct GetCompB : IRefAccess<EntityAB, CompB>
    {
        public ref CompB Get(ref EntityAB entity) => ref entity.b;
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataAccess<T, TAccessA, TAccessB> : IJob where T : unmanaged
       where TAccessA : unmanaged, IRefAccess<T, CompA>
        where TAccessB : unmanaged, IRefAccess<T, CompB>
    {
        public NativeArray<T> data;


        public unsafe void Execute()
        {
//            if (T is EntityAB) return;
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);
                //ref var ab = ref *(EntityAB*)element.AsPointer();

                ref var a = ref default(TAccessA).Get(ref element);
                ref var b = ref default(TAccessB).Get(ref element);
                a.a++;
                b.b++;


            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataShared<T> : IJob where T:unmanaged
    {
        public NativeArray<T> data;
        public unsafe static ref TComponent GetComponentData<TEntity, TComponent>( ref TEntity entity)
    where TEntity : unmanaged
    where TComponent : unmanaged
        {
            var pElement = (IntPtr)entity.AsPointer();
            var offset = SharedComponentOffset<TEntity, TComponent>.Ref.Data;
            var pComponent = pElement + offset;
            return ref *(TComponent*)pComponent;

        }

        public unsafe void Execute()
        {
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);
                ref var a = ref GetComponentData<T,CompA>(ref element);
                ref var b = ref GetComponentData<T, CompB>(ref element);

                a.a++;
                b.b++;

            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataMixed<T> : IJob where T : unmanaged
    {
        public NativeArray<T> data;

        public unsafe void Execute()
        {
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);
                var pElement = (byte*)element.AsPointer();
                ref var e = ref *(EntityAB*)(pElement + SharedComponentOffset<T, EntityAB>.Ref.Data);
                ref var a = ref e.a;
                ref var b = ref e.b;
                //ref var a = ref *(CompA*)(pElement + 0);
                //ref var b = ref *(CompB*)(pElement + 4);
                a.a++;
                b.b++;


            }
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataOffset<T> : IJob where T : unmanaged
    {
        public NativeArray<T> data;

        public unsafe ref TComponent GetComponentData<TEntity, TComponent>(ref TEntity entity) where TEntity:unmanaged where  TComponent:unmanaged
        {
            var pEntity = (byte*)entity.AsPointer();
            return ref *(TComponent*)(pEntity + ComponentOffset<TEntity, TComponent>.offset);
        }

        public unsafe void Execute()
        {
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);
                ref var a = ref GetComponentData<T, CompA>(ref element);
                ref var b = ref GetComponentData<T, CompB>(ref element);
                a.a++;
                b.b++;


            }
        }
    }
    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataDirect: IJob 
    {
        public NativeArray<EntityAB> data;

        public unsafe void Execute()
        {
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);

                ref var a = ref element.a;
                ref var b = ref element.b;
                a.a++;
                b.b++;


            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct TestDataCached<T> : IJob where T : unmanaged
    {
        public NativeArray<T> data;
        public int offsetCompA;
        public int offsetCompB;

        public unsafe ref TComponent GetComponentData<TEntity, TComponent>(ref TEntity entity, int offst) where TEntity : unmanaged where TComponent : unmanaged
        {
            var pEntity = (byte*)entity.AsPointer();
            return ref *(TComponent*)(pEntity + offst);
        }

        public unsafe void Execute()
        {
            for (int i = 0; i < opcount; i++)
            {
                ref var element = ref data.ItemAsRef(i);
                ref var a = ref GetComponentData<T, CompA>(ref element, offsetCompA);
                ref var b = ref GetComponentData<T, CompB>(ref element, offsetCompB);
                a.a++;
                b.b++;


            }
        }
    }

    IEnumerator Start()
    {
        SharedComponentOffset<EntityAB, EntityAB>.Ref.Data = 0;
        SharedComponentOffset<EntityAB, CompA>.Ref.Data = 0;
        SharedComponentOffset<EntityAB, CompB>.Ref.Data = 4;

        var data = new NativeArray<EntityAB>(opcount, Allocator.Persistent);
        var jobAccess = new TestDataAccess<EntityAB, GetCompA, GetCompB>() { data=data };
        var jobDirect = new TestDataDirect { data = data };
        var jobShared = new TestDataShared<EntityAB> { data = data };
        var jobOffset = new TestDataOffset<EntityAB> { data = data };
        var jobCached= new TestDataCached<EntityAB> { data = data, offsetCompA= SharedComponentOffset<EntityAB, CompA>.Ref.Data, offsetCompB = SharedComponentOffset<EntityAB, CompB>.Ref.Data };
        var jobMixed = new TestDataMixed<EntityAB> { data = data };
        for (int i = 0; i < 10; i++)
        {
            var watch1 = System.Diagnostics.Stopwatch.StartNew();
            jobDirect.Schedule().Complete();
            watch1.Stop();
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            jobAccess.Schedule().Complete();
            watch2.Stop();
            var watch3 = System.Diagnostics.Stopwatch.StartNew();
            jobShared.Schedule().Complete();
            watch3.Stop();
            var watch4 = System.Diagnostics.Stopwatch.StartNew();
            jobOffset.Schedule().Complete();
            watch4.Stop();
            var watch5 = System.Diagnostics.Stopwatch.StartNew();
            jobCached.Schedule().Complete();
            watch5.Stop();
            var watch6 = System.Diagnostics.Stopwatch.StartNew();
            jobMixed.Schedule().Complete();
            watch6.Stop();
            Debug.Log($"direct: {1000000 * watch1.Elapsed.TotalMilliseconds / opcount},"+
                $"access: {1000000 * watch2.Elapsed.TotalMilliseconds / opcount},"+
                $"shared: {1000000 * watch3.Elapsed.TotalMilliseconds / opcount}"+
                $"offset: {1000000 * watch4.Elapsed.TotalMilliseconds / opcount}"+
                $"cached: {1000000 * watch5.Elapsed.TotalMilliseconds / opcount}"+
                $"mixed: {1000000 * watch6.Elapsed.TotalMilliseconds / opcount}"); 
            yield return null;
        }


        print($"{data[0].a.a} {data[0].b.b}");
        data.Dispose();
    }
}
