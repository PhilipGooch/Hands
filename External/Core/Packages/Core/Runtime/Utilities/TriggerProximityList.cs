using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core
{
    public enum ProximityListComponentSearch
    {
        InParents, // will look for desired component in parents of collider that entered trigger
        OnRigidbody, // will look for component in rigidbody containing the collider
        InRigidbodyHierarchy, // will look fo component in whole hierarchy of rigidbody
    }

    public class TriggerProximityList<T> : IReadOnlyList<T>
        where T : Component
    {
        List<T> items = new List<T>();
        List<Collider> collidersInRange = new List<Collider>();

        ProximityListComponentSearch search;

        #region IReadOnlyList<T> implementation
        public int Count => items.Count;
        public T this[int index] => items[index];
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        #endregion

        public TriggerProximityList(ProximityListComponentSearch search)
        {
            this.search = search;
        }

        T FindComponent(Collider other)
        {
            switch (search)
            {
                case ProximityListComponentSearch.OnRigidbody:
                    if (other.attachedRigidbody == null)
                        return null;
                    return other.attachedRigidbody.GetComponent<T>();
                case ProximityListComponentSearch.InRigidbodyHierarchy:
                    if (other.attachedRigidbody == null)
                        return null;
                    return other.attachedRigidbody.GetComponentInChildren<T>();
                case ProximityListComponentSearch.InParents:
                    return other.GetComponentInParent<T>();
                default: throw new InvalidOperationException();
            }
        }

        public T OnTriggerEnter(Collider other)
        {
            var item = FindComponent(other);
            if (item == null)
                return null;
            if (collidersInRange.Contains(other))
                return null;
            collidersInRange.Add(other);
            if (items.Contains(item))
                return null;

            // add and return item
            items.Add(item);
            return item;
        }

        public T OnTriggerLeave(Collider other)
        {
            var item = FindComponent(other);
            if (item == null)
                return null;
            if (!collidersInRange.Contains(other))
                return null;
            collidersInRange.Remove(other);

            // was not in list
            if (!items.Contains(item))
                return null;

            // still have colliders for this object
            for (int j = 0; j < collidersInRange.Count; j++)
            {
                if (FindComponent(collidersInRange[j]) == item)
                {
                    return null;
                }
            }

            // remove and return item
            items.Remove(item);
            return item;
        }

        public void RemoveNullEntries()
        {
            for (var index = collidersInRange.Count - 1; index >= 0; index--)
            {
                var collider = collidersInRange[index];
                if (collider == null)
                {
                    collidersInRange.RemoveAt(index);
                }
            }
            for (var index = items.Count - 1; index >= 0; index--)
            {
                var entry = items[index];
                if (entry == null)
                {
                    items.RemoveAt(index);
                }
            }
        }

        public void RemovedDisabledEntries(Action<T> OnRemove = null)
        {
            for (var index = collidersInRange.Count - 1; index >= 0; index--)
            {
                var collider = collidersInRange[index];
                if (!collider.gameObject.activeInHierarchy)
                {
                    collidersInRange.RemoveAt(index);
                }
            }
            for (var index = items.Count - 1; index >= 0; index--)
            {
                var entry = items[index];
                if (!entry.gameObject.activeInHierarchy)
                {
                    items.RemoveAt(index);
                    OnRemove?.Invoke(entry);
                }
            }
        }

        public void RemoveDisabledOrNullEntries()
        {
            for (var index = collidersInRange.Count - 1; index >= 0; index--)
            {
                var collider = collidersInRange[index];
                if (collider == null || !collider.gameObject.activeInHierarchy)
                {
                    collidersInRange.RemoveAt(index);
                }
            }
            for (var index = items.Count - 1; index >= 0; index--)
            {
                var entry = items[index];
                if (entry == null || !entry.gameObject.activeInHierarchy)
                {
                    items.RemoveAt(index);
                }
            }
        }

        public void ClearItemsAndProximityData()
        {
            items.Clear();
            collidersInRange.Clear();
        }
    }
}
