using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public interface ICameraOverride
    {
        public int priority { get; }
    }

    public interface ICameraShakeOverride : ICameraOverride
    {
        float cameraShake { get; }
    }
    public interface ICameraVelocityOverride : ICameraOverride
    {
        float3 cameraVelocity { get; }
    }


    public class CameraOverrideList<T> : List<T> where T : ICameraOverride
    {
        public T GetOverride()
        {
            while (Count > 0 && this[0] == null) // remove destroyed items from start
                RemoveAt(0);
            if (Count > 0)
                return this[0];
            return default;
        }
        public void AddOverride(T item)
        {
            var priority = item.priority;
            RemoveDestroyed(); // remove destroyed items here to conserve list size
            var insertIndex = 0;
            for (int i = 0; i < Count; i++)
                if (this[i].priority > priority) // item has higher priority, insert after it
                    insertIndex++;
            Insert(insertIndex, item);
        }
        public void RemoveOverride(T item)
        {
            Remove(item);
        }
        void RemoveDestroyed()
        {
            for (int i = Count - 1; i >= 0; i--)
                if (this[i] == null)
                    RemoveAt(i);
        }

        public static T GetOverride(Entity trackedEntity)
        {
            var list = EntityStore.GetComponentObject<CameraOverrideList<T>>(trackedEntity, optional: true);
            if (list == null)
                return default;
            return list.GetOverride();
        }

        public static void AddOverride(Entity trackedEntity, T item)
        {
            var list = EntityStore.GetComponentObject<CameraOverrideList<T>>(trackedEntity, optional: true);
            if (list == null)
                throw new System.InvalidOperationException($"Camerra override for {typeof(T)} not supported by tracked entity");
            list.AddOverride(item);
        }
        public static void RemoveOverride(Entity trackedEntity, T item)
        {
            var list = EntityStore.GetComponentObject<CameraOverrideList<T>>(trackedEntity, optional: true);
            if (list == null)
                throw new System.InvalidOperationException($"Camerra override for {typeof(T)} not supported by tracked entity");
            list.RemoveOverride(item);
        }

        public static void EnableOverride(Entity trackedEntity)
        {
            var list = EntityStore.GetComponentObject<CameraOverrideList<T>>(trackedEntity, optional: true);
            if (list != null)
                throw new System.InvalidOperationException($"Camerra override already enabled {typeof(T)}");
            EntityStore.AddComponentObject(trackedEntity, new CameraOverrideList<T>());

        }
    }
}
