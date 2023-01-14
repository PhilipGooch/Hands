using NBG.Core;
using Recoil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Experimental
{
    /// <summary>
    /// All purpose Trigger Script. Uses some methods that are not good for production but great for prototyping
    /// </summary>
    public class TriggerProximityBehaviour : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        public IEnumerable<Rigidbody> DetectedBodies => detectedBodies;
        public int Count => detectedBodies.Count;
        public bool IsEmpty => detectedBodies.Count == 0;
        public IEnumerable<GameObject> DetectedObjects => detectedBodies.Select(x => x.gameObject);

        public event Action<Rigidbody> OnBodyEnter;
        public event Action<Rigidbody> OnBodyExit;

        public event Action<GameObject> OnObjectEnter;
        public event Action<GameObject> OnObjectExit;

        public Func<Rigidbody, bool> AllowOnly;

        [Tooltip("If true, it will remove destroyed objects in FixedUpdate")]
        [SerializeField] protected bool ClearDestroyed;
        [Tooltip("If true, it will remove disabled objects in FixedUpdate AND call OnBodyExit/OnObjectExit for them")]
        [SerializeField] protected bool ClearInactive;
        [Tooltip("Set this to a value higher than 0 to skip Fixed Update Frames when clearing")]
        [Range(0, 1000)] [SerializeField] protected int ClearFrequency;

        [Tooltip("Only allow objects with these names")]
        [SerializeField] protected List<string> OnlyWithName = null;
        [Tooltip("Only allow objects with these Tags")]
        [SerializeField] protected List<string> OnlyWithTag = null; //TODO: Tag Names inspector?
        [Tooltip("Only allow objects in these layers")]
        [SerializeField] protected List<int> OnlyWithLayer = null; //TODO: Layer Names inspector?

        private readonly TriggerProximityList<Rigidbody> detectedBodies = new TriggerProximityList<Rigidbody>(ProximityListComponentSearch.OnRigidbody);
        private int clearCounter;
        private int inspectorLayerMask;

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        #region IManagedBehaviour
        void IManagedBehaviour.OnLevelLoaded()
        {
            if (ClearDestroyed || ClearInactive)
            {
                OnFixedUpdateSystem.Register(this);
                clearCounter = ClearFrequency;
            }
            if (OnlyWithLayer != null)
            {
                inspectorLayerMask = LayerMask.GetMask(OnlyWithName.ToArray());
            }
            if (OnlyWithLayer != null || OnlyWithTag != null || OnlyWithLayer != null)
            {
                AllowOnly = FilterInspectorSettings;
            }
        }
        void IManagedBehaviour.OnAfterLevelLoaded()
        {

        }
        void IManagedBehaviour.OnLevelUnloaded()
        {
            if (ClearDestroyed || ClearInactive)
            {
                OnFixedUpdateSystem.Unregister(this);
            }
        }
        #endregion

        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (--clearCounter > 0)
            {
                return;
            }
            clearCounter = ClearFrequency;

            if (ClearInactive)
            {
                if (OnBodyExit != null || OnObjectExit != null)
                {
                    detectedBodies.RemovedDisabledEntries(HandleDeactivated);
                }
                else
                {
                    detectedBodies.RemovedDisabledEntries();
                }
            }
            else if (ClearDestroyed)
            {
                detectedBodies.RemoveNullEntries();
            }
        }

        public IEnumerable<T> GetDetectedScripts<T>()
        {
            foreach (var rb in detectedBodies)
            {
                if (rb.TryGetComponent(out T script))
                {
                    yield return script;
                }
            }
        }
        protected void OnTriggerEnter(Collider other)
        {
            if (AllowOnly != null && !AllowOnly(other.attachedRigidbody))
            {
                return;
            }
            var rb = detectedBodies.OnTriggerEnter(other);
            if (rb != null)
            {
                OnBodyEnter?.Invoke(rb);
                OnObjectEnter?.Invoke(rb.gameObject);
            }
        }
        protected void OnTriggerExit(Collider other)
        {
            if (AllowOnly != null && !AllowOnly(other.attachedRigidbody))
            {
                return;
            }
            var rb = detectedBodies.OnTriggerLeave(other);
            if (rb != null)
            {
                OnBodyExit?.Invoke(rb);
                OnObjectExit?.Invoke(rb.gameObject);
            }
        }
        private void HandleDeactivated(Rigidbody deactivatedBody)
        {
            OnBodyExit?.Invoke(deactivatedBody);
            OnObjectExit?.Invoke(deactivatedBody.gameObject);
        }

        #region UsefulFilters
        public static Func<GameObject, bool> AllowOnlyLayer(int layerRequired)
        {
            return (go) => go.layer == layerRequired;
        }
        public static Func<GameObject, bool> AllowOnlyLayerMask(params string[] layerNames)
        {
            int layerMask = LayerMask.GetMask();
            return (go) => (go.layer & layerMask) > 0;
        }
        public static Func<GameObject, bool> AllowWithName(string name)
        {
            return (go) => go.name == name;
        }
        public static Func<GameObject, bool> AllowWithName(IEnumerable<string> name)
        {
            return (go) => name.Contains(go.name);
        }
        public static Func<GameObject, bool> AllowWithName(params string[] name)
        {
            return (go) => name.Contains(go.name);
        }
        public static Func<GameObject, bool> AllowWithTag(string tag)
        {
            return (go) => go.CompareTag(go.name);
        }
        public static Func<GameObject, bool> IgnoreTheseObjects(IEnumerable<GameObject> objectsToIgnore)
        {
            return (go) => !objectsToIgnore.Contains(go);
        }
        private bool FilterInspectorSettings(Rigidbody rb)
        {
            var go = rb.gameObject;
            if (OnlyWithName != null && OnlyWithName.Count > 0)
            {
                if (!OnlyWithName.Contains(go.name))
                {
                    return false;
                }
            }
            if (OnlyWithTag != null && OnlyWithTag.Count > 0)
            {
                bool foundAny = false;
                foreach (var filterTag in OnlyWithTag)
                {
                    if (go.CompareTag(filterTag))
                    {
                        foundAny = true;
                        break;
                    }
                }
                if (!foundAny) return false;
            }
            if (OnlyWithLayer != null && inspectorLayerMask > 0)
            {
                if ((inspectorLayerMask & go.layer) == 0)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}