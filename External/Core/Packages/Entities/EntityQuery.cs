using NBG.Unsafe;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;

namespace NBG.Entities
{
    public unsafe struct EntityQueryCache
    {
        public static UnsafeParallelHashMap<int, EntityQuery> cache;
        public static UnsafeParallelHashMap<int, EntityQueryResults> resultCache;
        public static void Initialize()
        {
            cache = new UnsafeParallelHashMap<int, EntityQuery>(10, Allocator.Persistent);
            resultCache = new UnsafeParallelHashMap<int, EntityQueryResults>(10, Allocator.Persistent);
        }
        public static void Destroy()
        {
            using(var keysValues = cache.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keysValues.Length; i++)
                    keysValues.Values[i].Dispose();
            }
            cache.Dispose();
            using (var keysValues = resultCache.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keysValues.Length; i++)
                    keysValues.Values[i].Dispose();
            }
            resultCache.Dispose();

        }

        public static int GetHash(UnsafeArray<int> components)
        {
            var hash = 0;
            for (var c = 0; c < components.Length; c++)
            {
                var comp = components[c];
                hash += comp;
            }
            return hash;
        }

        public static bool GetCachedResults(int hash, out EntityQueryResults results)
        {
            return resultCache.TryGetValue(hash, out results);
        }
        public static void UpdateResults(int hash, EntityQueryResults results)
        {
            resultCache[hash] = results;
        }
        public static void CacheResults(int hash, EntityQueryResults results)
        {
            if (resultCache.TryGetValue(hash, out var oldResults))
                oldResults.Dispose();
            resultCache[hash] = results;
        }
        public static bool GetCachedQuery(int hash, out EntityQuery query)
        {
            return cache.TryGetValue(hash, out query);
        }

        public static void CacheQuery(int hash, EntityQuery query)
        {
            if (cache.TryGetValue(hash, out var oldQuery))
                oldQuery.Dispose();
            cache[hash] = query;
        }
    }

    public unsafe struct EntityQueryResults :IDisposable
    {
        // results
        //bool containsResults;
        [NativeDisableUnsafePtrRestriction] public EntityReference* entityReferences;
        [NativeDisableUnsafePtrRestriction] public EntityLayout** layouts;
        public int count;

        // versions for validity checks

        public int archetypeVersion;
        public int entitiesVersion;
        [NativeDisableUnsafePtrRestriction] UnsafeParallelHashMap<EntityArchetype, int> chunkVersions;

        public bool isValid
        {
            get
            {
                //if (!containsResults) return false;
                if (EntityStore.archetypeVersion != archetypeVersion) return false; // if archetypes added/removed invalid
                if (EntityStore.entitiesVersion == entitiesVersion) return true; // if no entities added/removed - valid
                Profiler.BeginSample("Validate chunks");
                // else check if entities added/removed to chunk we're tracking
                using (var c = chunkVersions.GetKeyValueArrays(Allocator.Temp))
                {
                    for (int i = 0; i < c.Length; i++)
                        if (EntityStore.GetChunk(c.Keys[i]).version != c.Values[i])
                        {
                            Profiler.EndSample();
                            return false;
                        }
                }
                Profiler.EndSample();

                return true;
            }
        }

        public void Dispose()
        {
            //if (!containsResults) return;
            Unsafe.Unsafe.Free(entityReferences, Allocator.Persistent);
            chunkVersions.Dispose();
            //containsResults = false;
        }

        public unsafe EntityReference GetEntity(int idx)
        {
            Unsafe.Unsafe.CheckIndex(idx, count);
            return entityReferences[idx];
        }
        public ref T GetComponentData<T>(int idx) where T : unmanaged
        {
            return ref entityReferences[idx].GetComponentData<T>();
        }
        public T GetComponentObject<T>(int idx, bool optional = false)
        {
            return entityReferences[idx].GetComponentObject<T>(optional);
        }

        public static EntityQueryResults QueryComponents(UnsafeArray<int> components)
        {
            using (var archetypes = ListArchetypes(Allocator.Temp, components))
            {
                int count = 0;
                foreach (var archetype in archetypes)
                    count += EntityStore.GetChunk(archetype).count;
                var res = new EntityQueryResults()
                {
                    count = count,
                    entityReferences = Unsafe.Unsafe.Malloc<EntityReference>(count, Allocator.Persistent),

                    archetypeVersion = EntityStore.archetypeVersion,
                    entitiesVersion = EntityStore.entitiesVersion,
                    chunkVersions = new UnsafeParallelHashMap<EntityArchetype, int>(archetypes.Length, Allocator.Persistent)
                };
                var elementIdx = 0;
                foreach (var archetype in archetypes)
                {
                    ref var chunk = ref EntityStore.GetChunk(archetype);
                    res.chunkVersions[archetype] = chunk.version;
                    var chunkCount = chunk.count;
                    for (int i = 0; i < chunk.allocatedCount; i++)
                    {
                        var entityRef = chunk.GetEntityReference(i);
                        if (!entityRef.isNull)
                        {
                            res.entityReferences[elementIdx++] = entityRef;
                            if (chunkCount-- == 0) break;
                        }
                    }
                }
                return res;
            }
        }
        public static NativeArray<EntityArchetype> ListArchetypes(Allocator allocator, UnsafeArray<int> components)
        {
            var archetypes = new NativeList<EntityArchetype>(allocator);
            // collect archetypes having components
            using (var keysValues = EntityStore.chunks.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keysValues.Length; i++)
                {
                    ref var chunk = ref keysValues.Values[i].AsRef<EntityChunk>();
                    var valid = true;
                    for (var c = 0; c < components.Length; c++)
                    {
                        var comp = components[c];
                        if (!chunk.layout.componentDataOffsets.ContainsKey(comp))
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                        archetypes.Add(keysValues.Keys[i]);
                }
            }
            return archetypes.AsArray();
        }
    }

    public unsafe struct EntityQuery// :IDisposable
    {
        // meta for requerying
        public int hash;
        UnsafeArray<int> components;

        public EntityQueryResults Execute()
        {
            Profiler.BeginSample("EntityQuery.Execute");
            if (EntityQueryCache.GetCachedResults(hash, out var results))
            {
                if (results.isValid)
                {
                    if (results.entitiesVersion != EntityStore.entitiesVersion)
                    {
                        results.entitiesVersion = EntityStore.entitiesVersion;
                        EntityQueryCache.UpdateResults(hash, results);
                    }
                    Profiler.EndSample();

                    return results;
                    // results.Dispose(); will dispose results on overwrite
                }
            }
            Profiler.BeginSample("EntityQuery.Execute - QueryComponents");
            results = EntityQueryResults.QueryComponents(components);
            EntityQueryCache.CacheResults(hash, results);
            Profiler.EndSample();
            Profiler.EndSample();
            return results;
        }
      
        public static EntityQuery QueryComponents(UnsafeArray<int> components)
        { 
            var hash = EntityQueryCache.GetHash(components);
            if (!EntityQueryCache.GetCachedQuery(hash, out var query))
            {
                query = new EntityQuery() { hash = hash, components = new UnsafeArray<int>(components, Allocator.Persistent) };
                EntityQueryCache.CacheQuery(hash, query);

            }
            return query;
        }
       
        public void Dispose()
        {

            components.Dispose();
        }
       
      


     
       
        public static EntityQuery QueryComponents(int comp1)
        {
            using (var arr = new UnsafeArray<int>(1, Allocator.Temp))
            {
                arr.ElementAt(0) = comp1;
                return QueryComponents(arr);
            }
        }
        public static EntityQuery QueryComponents(int comp1, int comp2)
        {
            using (var arr = new UnsafeArray<int>(2, Allocator.Temp))
            {
                arr.ElementAt(0) = comp1;
                arr.ElementAt(1) = comp2;
                return QueryComponents(arr);
            }
        }
        public static EntityQuery QueryComponents(int comp1, int comp2, int comp3)
        {
            using (var arr = new UnsafeArray<int>(3, Allocator.Temp))
            {
                arr.ElementAt(0) = comp1;
                arr.ElementAt(1) = comp2;
                arr.ElementAt(2) = comp3;
                return QueryComponents(arr);
            }
        }
        public static EntityQuery QueryComponents(int comp1, int comp2, int comp3, int comp4)
        {
            using (var arr = new UnsafeArray<int>(4, Allocator.Temp))
            {
                arr.ElementAt(0) = comp1;
                arr.ElementAt(1) = comp2;
                arr.ElementAt(2) = comp3;
                arr.ElementAt(3) = comp4;
                return QueryComponents(arr);
            }
        }
        // generic version for more types
        public static EntityQuery QueryComponents(params int[] components)
        {
            using (var arr = new UnsafeArray<int>(components, Allocator.Temp))
                return QueryComponents(arr);
        }
    }
    //public struct EntityQueryDisposeJob : IJob
    //{
    //    EntityQuery query;
    //    public static JobHandle Schedule(EntityQuery query, JobHandle dependsOn)
    //    {
    //        return new EntityQueryDisposeJob() { query = query }.Schedule(dependsOn);

    //    }
    //    public void Execute()
    //    {
    //        query.Dispose();
    //    }


    //}

}