using System;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// Implements minimal ECS like storage for structs
// Needed to allow accessing data components embedded in structs by libraries having no knowledge of full struct containing the data

namespace NBG.Entities
{
    public struct EntityArchetype : IEquatable<EntityArchetype>
    {
        public int hash;
        // marks unsed element in archetype map
        public bool isDeleted => hash == -1;
        public static EntityArchetype deleted => new EntityArchetype(-1);
        // entity archetype having without components
        public bool isEmpty => hash == 0;
        public static EntityArchetype empty => new EntityArchetype(0);

        public EntityArchetype(int hash)
        {
            this.hash = hash;
        }

        public override bool Equals(object obj) => obj is EntityArchetype other && this.Equals(other);

        public bool Equals(EntityArchetype p) => hash == p.hash;
        public override int GetHashCode() => hash.GetHashCode();

        public static bool operator ==(EntityArchetype lhs, EntityArchetype rhs) => lhs.Equals(rhs);

        public static bool operator !=(EntityArchetype lhs, EntityArchetype rhs) => !(lhs == rhs);
    }

    public struct Entity : IEquatable<Entity>
    {
        public int id;

        public Entity(int id)
        {
            this.id = id;
        }
        
        public override bool Equals(object obj) => obj is Entity other && this.Equals(other);

        public bool Equals(Entity p) => id == p.id;
        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(Entity lhs, Entity rhs) => lhs.Equals(rhs);

        public static bool operator !=(Entity lhs, Entity rhs) => !(lhs == rhs);

        public bool isNull => id == 0;
        public static Entity Null => new Entity(0);
    }
    public struct EntityChunk : IDisposable
    {
        public unsafe EntityLayout layout;

        // storage
        public unsafe IntPtr data;
        public unsafe int* entityIds;
        //public unsafe bool* busyMap;
        public int itemSize;
        public int count;
        public int allocatedCount;

        // version to allow caching, incremented on entity add/remove
        public int version;

        public unsafe EntityChunk(EntityLayout layout, EntityArchetype archetype, int itemSize, int maxEntities)
        {
            this.layout = layout;

            data = (IntPtr)UnsafeUtility.Malloc(itemSize * maxEntities, 4, Allocator.Persistent);
            entityIds = Unsafe.Unsafe.Malloc<int>(maxEntities, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            UnsafeUtility.MemSet(entityIds, 0xFF, sizeof(int) * maxEntities);
            //busyMap = Unsafe.Malloc<bool>(maxEntities, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            this.itemSize = itemSize;
            count = 0;
            allocatedCount = maxEntities;
            version = 0;

        }

        public unsafe void Dispose()
        {
            UnsafeUtility.Free((void*)data, Allocator.Persistent);
            Unsafe.Unsafe.Free(entityIds, Allocator.Persistent);
            
        }
        public unsafe int AddEntity(int id)
        {
            for (int i = 0; i < allocatedCount; i++)
                if (entityIds[i] < 0)
                {
                    entityIds[i] = id;
                    UnsafeUtility.MemClear((void*)(data + i * itemSize), itemSize);
                    count++;
                    version++;
                    return i;
                }
            throw new InvalidOperationException("Chunk capacity exceeded while trying to add entity");
        }
        public unsafe int AddEntity(int id, IntPtr ptr)
        {
            
            UnsafeUtility.MemCpy((void*)(data + AddEntity(id) * itemSize), (void*)ptr, itemSize);
            
            throw new InvalidOperationException("Chunk capacity exceeded while trying to add entity");
        }

        public unsafe EntityReference GetEntityReference(int index)
        {
            if (entityIds[index]<0) return EntityReference.Null;
            return new EntityReference(new Entity(entityIds[index]), data + index * itemSize, layout.AsPointer());
        }
        public unsafe IntPtr GetEntityPtr(int index)
        {
            if (entityIds[index] < 0) return IntPtr.Zero;
            return data + index * itemSize;
        }

        public unsafe void RemoveEntity(int index)
        {
            if (entityIds[index] < 0)
                throw new InvalidOperationException("Trying to remove entity that does not exist");
            count--;
            entityIds[index] = -1;
            version++;
        }

        internal Entity GetEntity(int i)
        {
            throw new NotImplementedException();
        }
    }

    public class EntityObjectStore
    {
        public static Dictionary<int, EntityObjectStore> all = new Dictionary<int, EntityObjectStore>();
        Dictionary<Type, object> registry = new Dictionary<Type, object>();
        public static void Create() { }
        public static void Destroy()
        {
            all.Clear();
        }

        public static EntityObjectStore GetOrCreate(Entity entity)
        {
            if(!all.TryGetValue(entity.id, out var store))
            {
                store = new EntityObjectStore();
                all[entity.id] = store;
            }
            return store;
        }
        public static void Remove(Entity entity)
        {
            all.Remove(entity.id);
        }

        public T Get<T>(bool optional)
        {
            var key = typeof(T);
            if (!registry.TryGetValue(key, out var result) && !optional)
                throw new InvalidOperationException($"Type {key} not found in container");
            return (T)result;
        }

        public void Add<T>(T item)
        {
            var key = typeof(T);
            if (registry.ContainsKey(key))
                Debug.LogWarning($"Replacing object {key}");
            registry[key] = item;
        }

        public void Remove<T>(T item)
        {
            var key = typeof(T);
            bool wasRemoved = registry.Remove(key);
            if (!wasRemoved)
            {
                Debug.LogWarning($"Trying to remove not registered object {key}");
            }
        }
    }


    public struct EntityStore
    {
        internal static readonly SharedStatic<EntityStore> _store = SharedStatic<EntityStore>.GetOrCreate<EntityStore>();
        // hashmap of type hashes to EntityChunk
        UnsafeParallelHashMap<EntityArchetype, IntPtr> _chunks;

        // data to locate entities
        unsafe EntityArchetype* archetypeByEntity;
        unsafe int* indexInChunkByEntity;

        // settings
        int defaultChunkSize ; // chunk size allocated by AddComponents when resulting acrhetype is not found
        

        // versions for caching
        int _archetypeVersion; // incremented on adding/removing archetypes
        int _entitiesVersion; // incremented on add/remove entity
        public static int entitiesVersion => _store.Data._entitiesVersion; // incremented on add/remove entity
        public static int archetypeVersion => _store.Data._archetypeVersion; // incremented on add/remove entity
        public static ref  UnsafeParallelHashMap<EntityArchetype, IntPtr> chunks => ref _store.Data._chunks; // incremented on add/remove entity

        //public static UnsafeHashMap<EntityArchetype, IntPtr> chunks => _store.Data._chunks;
        //public unsafe static EntityArchetype* archetypeByEntity => _store.Data._archetypeByEntity;
        //public unsafe static int* indexInChunkByEntity => _store.Data._indexInChunkByEntity;
        //public static int defaultChunkSize => _store.Data._defaultChunkSize;
        //public static int archetypeVersion => _store.Data._archetypeVersion;
        //public static int entitiesVersion => _store.Data._entitiesVersion;

        public unsafe static void Create(int maxTypes, int maxEntities)
        {
            _store.Data.defaultChunkSize = 512;
            chunks = new UnsafeParallelHashMap<EntityArchetype, IntPtr>(maxTypes,Allocator.Persistent);
            _store.Data.archetypeByEntity = Unsafe.Unsafe.Malloc<EntityArchetype>(maxEntities, Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < maxEntities; i++)
                _store.Data.archetypeByEntity[i] = EntityArchetype.deleted;
            _store.Data.indexInChunkByEntity = Unsafe.Unsafe.Malloc<int>(maxEntities, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            EntityQueryCache.Initialize();
            EntityLayout.Create();
            EntityObjectStore.Create();
        }
        public unsafe static void Destroy()
        {
            EntityObjectStore.Destroy();
            EntityLayout.Destroy();
            EntityQueryCache.Destroy();
            Unsafe.Unsafe.Free(_store.Data.archetypeByEntity, Allocator.Persistent);
            Unsafe.Unsafe.Free(_store.Data.indexInChunkByEntity, Allocator.Persistent);

            using (var keysValues = chunks.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keysValues.Length; i++)
                    FreeChunk((EntityChunk*)keysValues.Values[i]);
            }
            chunks.Dispose();
        }



        public static unsafe EntityArchetype RegisterArchetype(int maxEntities, ComponentTypeList types) 
        {
            var layout = new EntityLayout(types);
            var archetype = layout.archetype;
            if (chunks.ContainsKey(archetype))
                throw new InvalidOperationException("Trying to register already registered entity type");

            var pChunk = Unsafe.Unsafe.Malloc<EntityChunk>(Allocator.Persistent);
            *pChunk = new EntityChunk(layout, archetype, layout.size, maxEntities);
            chunks[archetype] = (IntPtr)pChunk;
            _store.Data._archetypeVersion++;
            return archetype;
        }
        
        public unsafe static void UnregisterArchetype(EntityArchetype archetype) 
        {
            //var archetype = GetArchetype<T>();
            if (!chunks.ContainsKey(archetype))
                throw new InvalidOperationException("Trying to unregister not registered entity type");
            FreeChunk((EntityChunk*)chunks[archetype]);
            chunks.Remove(archetype);
            _store.Data._entitiesVersion++;
            _store.Data._archetypeVersion++;
        }

        private static unsafe void FreeChunk(EntityChunk* pChunk)
        {
            pChunk->layout.Dispose();
            pChunk->Dispose();
            Unsafe.Unsafe.Free(pChunk, Allocator.Persistent);
        }

        public static unsafe ref EntityChunk GetChunk(EntityArchetype archetype)
        {
            if (chunks.TryGetValue(archetype, out var chunkPtr))
                return ref *(EntityChunk*)chunkPtr;
            throw new InvalidOperationException("Must register entity type before adding entities");
        }

        public static unsafe Entity AddEntity()
        {
            var index = 1;
            while (!_store.Data.archetypeByEntity[index].isDeleted)
                index++;
            _store.Data.archetypeByEntity[index] = EntityArchetype.empty;
            _store.Data._entitiesVersion++;
            return new Entity(index);
        }

        public static unsafe Entity AddEntity(EntityArchetype archetype) 
        {
            var entity = AddEntity();
            if (!archetype.isEmpty)
            {
                ref var chunk = ref GetChunk(archetype);
                _store.Data.archetypeByEntity[entity.id] = archetype;
                _store.Data.indexInChunkByEntity[entity.id] = chunk.AddEntity(entity.id);
            }
            return entity;
        }

        public static unsafe EntityReference GetEntityReference(Entity entity)
        {
            var archetype = _store.Data.archetypeByEntity[entity.id];
            if (archetype.isDeleted)
                throw new InvalidOperationException($"Enity with id {entity.id} does not exist");
            if (archetype.isEmpty)
                return new EntityReference(entity, (IntPtr)0, EntityLayout.emptyLayout);
            ref var chunk = ref GetChunk(archetype);
            return chunk.GetEntityReference(_store.Data.indexInChunkByEntity[entity.id]);
        }

        public static unsafe bool TryGetEntityReference(Entity entity, out EntityReference entityRef)
        {
            if(!entity.isNull)
            {
                var archetype = _store.Data.archetypeByEntity[entity.id];
                if (!archetype.isDeleted && !archetype.isEmpty)
                {
                    ref var chunk = ref GetChunk(archetype);
                    entityRef = chunk.GetEntityReference(_store.Data.indexInChunkByEntity[entity.id]);
                    return true;
                }
            }
            entityRef = EntityReference.Null;
            return false;

        }

        public unsafe static void RemoveEntity(Entity entity)
        {
            var archetype = _store.Data.archetypeByEntity[entity.id];
            if (archetype.isDeleted)
                throw new InvalidOperationException($"Enity with id {entity.id} does not exist");
            if (!archetype.isEmpty)
            {
                ref var chunk = ref GetChunk(archetype);
                chunk.RemoveEntity(_store.Data.indexInChunkByEntity[entity.id]);
            }
            _store.Data.archetypeByEntity[entity.id] = EntityArchetype.deleted; // mark deleted
            _store.Data._entitiesVersion++;
            EntityObjectStore.Remove(entity);
        }

        public static ref T GetComponentData<T>(Entity entity) where T:unmanaged
        {
            return ref GetEntityReference(entity).GetComponentData<T>();
        }
        public static bool TryGetComponentData<T>(Entity entity, out T data) where T : unmanaged
        {
            if(TryGetEntityReference(entity, out var entityRef))
                return entityRef.TryGetComponentData<T>(out data);
            data = default;
            return false;
        }
        public static EntityObjectStore GetObjectStore(Entity entity)
        {
            return EntityObjectStore.GetOrCreate(entity);
        }
        public static T GetComponentObject<T>(Entity entity, bool optional=false)
        {
            return EntityObjectStore.GetOrCreate(entity).Get<T>(optional);
        }
        public static void AddComponentObject<T>(Entity entity, T obj)
        {
            EntityObjectStore.GetOrCreate(entity).Add(obj);
        }
        public static void RemoveComponentObject<T>(Entity entity, T obj)
        {
            EntityObjectStore.GetOrCreate(entity).Remove(obj);
        }

      

        // expand entity adding new components and move to new chunk if needed
        public unsafe static void AddComponents(Entity entity, ComponentTypeList types)
        {
            // determine target archetype
            var srcIndex = _store.Data.indexInChunkByEntity[entity.id]; // remember for later removal
            var srcArchetype = _store.Data.archetypeByEntity[entity.id];
            var srcRef = GetEntityReference(entity);
            var srcLayout = srcRef.layout;
            var dstArchetype = new EntityArchetype(srcLayout->CalculateCombinedHash(types));
            if (srcLayout->archetype == dstArchetype) return; // nothing to do

            // create target chunk if needed
            if (!chunks.ContainsKey(dstArchetype))
                RegisterArchetype(_store.Data.defaultChunkSize, ComponentTypeList.Combine(srcLayout->componentTypes, types));
            
            // allocate in target chunk
            ref var dstChunk = ref GetChunk(dstArchetype);
            _store.Data.archetypeByEntity[entity.id] = dstArchetype;
            _store.Data.indexInChunkByEntity[entity.id] = dstChunk.AddEntity(entity.id);
            var dstRef = dstChunk.GetEntityReference(_store.Data.indexInChunkByEntity[entity.id]);

            // copy entity data
            if (!srcArchetype.isEmpty)
            {
                int nComponents = srcRef.layout->componentTypes.Length;
                for (int i = 0; i < nComponents; i++)
                {
                    var type = srcRef.layout->componentTypes[i];
                    UnsafeUtility.MemCpy(dstRef.GetComponentDataPtr(type), srcRef.GetComponentDataPtr(type), type.size);
                }

                // remove from source chunk
                ref var srcChunk = ref GetChunk(srcArchetype);
                srcChunk.RemoveEntity(srcIndex);
            }

            _store.Data._entitiesVersion++;
        }

     
        public static EntityQuery Query<T1>() => EntityQuery.QueryComponents(BurstRuntime.GetHashCode32<T1>());

        public static EntityQuery Query<T1, T2>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>());

        public static EntityQuery Query<T1, T2, T3>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>());

        public static EntityQuery Query<T1, T2, T3, T4>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>(), BurstRuntime.GetHashCode32<T4>());

        public static EntityQuery Query<T1, T2, T3, T4, T5>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>(), BurstRuntime.GetHashCode32<T4>(),
            BurstRuntime.GetHashCode32<T5>());

        public static EntityQuery Query<T1, T2, T3, T4, T5, T6>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>(), BurstRuntime.GetHashCode32<T4>(),
            BurstRuntime.GetHashCode32<T5>(), BurstRuntime.GetHashCode32<T6>());

        public static EntityQuery Query<T1, T2, T3, T4, T5, T6, T7>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>(), BurstRuntime.GetHashCode32<T4>(),
            BurstRuntime.GetHashCode32<T5>(), BurstRuntime.GetHashCode32<T6>(), BurstRuntime.GetHashCode32<T7>());

        public static EntityQuery Query<T1, T2, T3, T4, T5, T6, T7, T8>() => EntityQuery.QueryComponents(
            BurstRuntime.GetHashCode32<T1>(), BurstRuntime.GetHashCode32<T2>(), BurstRuntime.GetHashCode32<T3>(), BurstRuntime.GetHashCode32<T4>(),
            BurstRuntime.GetHashCode32<T5>(), BurstRuntime.GetHashCode32<T6>(), BurstRuntime.GetHashCode32<T7>(), BurstRuntime.GetHashCode32<T8>());
    }
}