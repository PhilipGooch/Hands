using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Entities
{
    public unsafe struct EntityReference 
    {
        public Entity entity;
        public IntPtr entityPtr;
        public EntityLayout* layout;

        public static EntityReference Null => new EntityReference(new Entity(), IntPtr.Zero, null);
        public bool isNull => entityPtr == IntPtr.Zero;

        public EntityReference(Entity entity, IntPtr entityPtr, EntityLayout* layout)
        {
            this.entity = entity;
            this.entityPtr = entityPtr;
            this.layout = layout;
        }

        public ref T GetComponentData<T>() where T : unmanaged
        {
            return ref layout->GetComponentData<T>(entityPtr);
        }
        public bool TryGetComponentData<T>(out T data) where T : unmanaged
        {
            return layout->TryGetComponentData<T>(entityPtr, out data);
        }
        public void * GetComponentDataPtr(ComponentType componentType)
        {
            return layout->GetComponentPointer(entityPtr, componentType);
        }
        public T GetComponentObject<T>(bool optional=false) 
        {
            return EntityStore.GetComponentObject<T>(entity, optional);
        }
    }
}
