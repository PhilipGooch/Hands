using NBG.Unsafe;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NBG.Entities
{
    public struct ComponentType : IEquatable<ComponentType>
    {
        public int size;
        public int hash;

        public ComponentType(int hash, int size)
        {
            this.hash = hash;
            this.size = size;
        }

        public static unsafe ComponentType FromType<T>() where T:unmanaged => new ComponentType(BurstRuntime.GetHashCode32<T>(), sizeof(T));
        public override bool Equals(object obj) => obj is EntityArchetype other && this.Equals(other);

        public bool Equals(ComponentType p) => hash == p.hash;
        public override int GetHashCode() => hash.GetHashCode();

        public static bool operator ==(ComponentType lhs, ComponentType rhs) => lhs.Equals(rhs);

        public static bool operator !=(ComponentType lhs, ComponentType rhs) => !(lhs == rhs);
    }
    public struct ComponentTypeList : IDisposable
    {
        public int hash;
        public int Length => types.Length;
        
        UnsafeList<ComponentType> types;
        public ref ComponentType this[int index] => ref types.ElementAt(index);
        public static ComponentTypeList Create()
        {
            return new ComponentTypeList() { hash = 0, types = new UnsafeList<ComponentType>(16, Allocator.Persistent) };
        }

        public void AddType<T>() where T : unmanaged
        {
            var t = ComponentType.FromType<T>();
            AddType(t);
        }
        public void AddType(ComponentType t)
        {
            for (int i = 0; i < types.Length; i++)
                if (types[i] == t) return;
            types.Add(t);
            hash += t.hash;
        }

        public void Dispose()
        {
            types.Dispose();
        }

        public static ComponentTypeList Combine(ComponentTypeList list1, ComponentTypeList list2)
        {
            var resulting = new ComponentTypeList() { hash = list1.hash, types = new UnsafeList<ComponentType>(list1.Length + list2.Length, Allocator.Persistent) };
            resulting.types.AddRangeNoResize(list1.types);
            for (int i = 0; i < list2.Length; i++)
                resulting.AddType(list2.types[i]);
            return resulting;
        }

    }


    public unsafe struct EntityLayout :IDisposable
    {
        internal static readonly SharedStatic<EntityLayout> _emptyLayout = SharedStatic<EntityLayout>.GetOrCreate<EntityLayout>();
        public unsafe static EntityLayout* emptyLayout => _emptyLayout.Data.AsPointer();

        // offsets for type within stored struct
        public EntityArchetype archetype;
        public UnsafeParallelHashMap<int, int> componentDataOffsets;
        public ComponentTypeList componentTypes;
        public int size;

        public EntityLayout(ComponentTypeList componentTypes)
        {
            this.componentTypes = componentTypes;
            componentDataOffsets = new UnsafeParallelHashMap<int, int>(0, Allocator.Persistent);
            size = 0;
            var hash = 0;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var h = componentTypes[i].hash;
                var s = componentTypes[i].size;
                componentDataOffsets[h] = size;
                hash += h;
                size += s;
            }
            archetype = new EntityArchetype(hash);
        }

        public int GetComponentDataOffset<T>() where T : unmanaged
        {
            return GetComponentDataOffset(BurstRuntime.GetHashCode32<T>());
        }
        public int GetComponentDataOffset(int componentType)
        {
            if (componentDataOffsets.TryGetValue(componentType, out var offset))
                return offset;
            else
                throw new InvalidOperationException("Entity does not have component.");

        }
        public unsafe ref T GetComponentData<T>(IntPtr entity) where T : unmanaged
        {
            return ref *(T*)(entity + GetComponentDataOffset<T>());
        }
        public unsafe bool TryGetComponentData<T>(IntPtr entity, out T data) where T : unmanaged
        {
            var componentType = BurstRuntime.GetHashCode32<T>();
            if (componentDataOffsets.TryGetValue(componentType, out var offset))
            {
                data = *(T*)(entity + offset);
                return true;
            }
            data = default;
            return false;
        }
        public unsafe void* GetComponentPointer(IntPtr entity, ComponentType componentType)
        {
            return (void*)(entity + GetComponentDataOffset(componentType.hash));
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public void Dispose()
        {
            componentDataOffsets.Dispose();
            componentTypes.Dispose();
        }
        public static void Create()
        {
            var emptyTypeList = ComponentTypeList.Create();
            //emptyLayout = Unsafe.Unsafe.Malloc<EntityLayout>(Allocator.Persistent);
            *emptyLayout = new EntityLayout(emptyTypeList);
        }

        public static void Destroy()
        {
            emptyLayout->componentTypes.Dispose();
            // Unsafe.Unsafe.Free(emptyLayout, Allocator.Persistent);
        }

        public int CalculateCombinedHash(ComponentTypeList types)
        {
            var result = archetype.hash;
            for (int i = 0; i < types.Length; i++)
                if (!componentDataOffsets.ContainsKey(types[i].hash))
                    result += types[i].hash;
            return result;
        }      
    }


}
