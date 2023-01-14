using NBG.Core;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Net;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.ElementsSystem
{
    public interface IElementsSystemEvents
    {
        public int ElementId { get; }
        //public GameObject GameObjectReference {get; }//todo switch to this after GameObjectReference will suport dynamic objects
    }

    public delegate void ElementsSystemEventWithEffectsHandler(bool withEffects);

    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class ElementsGameSystem : GameSystem
    {
        public readonly LayerMask DEFAULT_LAYER_MASK = Physics.AllLayers & ~(1 << LayerMask.NameToLayer("Ignore Raycast") | 1 << LayerMask.NameToLayer("TransparentFX") | 1 << LayerMask.NameToLayer("UI"));
        public LayerMask layerMask;

        public const int MAXIMUM_COLLISION_COUNT_PER_OBJECT = 10;
        int _maximumObjectCountThatCanBeAffectedByOneObject = MAXIMUM_COLLISION_COUNT_PER_OBJECT;
        public int maximumObjectCountThatCanBeAffectedByOneObject
        {
            get => _maximumObjectCountThatCanBeAffectedByOneObject;
            set
            {
                _maximumObjectCountThatCanBeAffectedByOneObject = value;
                RecreateArrays();
            }
        }

        IEventBus _eventBus;

        Dictionary<int, IElementsSystemObject> _elementsSystemObjectsDict = new Dictionary<int, IElementsSystemObject>();
        List<IElementsSystemObject> _elementsSystemObjects = new List<IElementsSystemObject>();
        Dictionary<int, IElementsSystemObject> _collidersMatObjectsMap = new Dictionary<int, IElementsSystemObject>();

        IElementsSystemObject[] affectingElementsSystemObjects;
        IElementsSystemObject[] affectedElementsSystemObjects;

        List<Type> _registeredEventTypes = new List<Type>();

        static int _instanceCounter = 0;

        //This is BAD and temporary workaroud. Noticed bug, that if client and server will have different scene history, matches, then id will be bad.
        //For now just adding clearing, so testing in the same scene would be possible, but we can't depend on those generated id's. Best to migrate to objectIdDatabase asap, but we cant do that either now
        //as unit tests will not pass as objectiddatabase doesn't support dynamic objects yet
        public void ResetUniqueElementID()
        {
            _instanceCounter = 0;
        }

        public int GenerateUniqueElementsId()
        {
            _instanceCounter++;
            return _instanceCounter;
        }

        public void RegisterForEvents(IElementsSystemObject elementsSystemObject)
        {
            Debug.Assert(elementsSystemObject.ElementId > 0, $"ElementID is not set yet");

            if (!_elementsSystemObjectsDict.ContainsKey(elementsSystemObject.ElementId))
                _elementsSystemObjectsDict.Add(elementsSystemObject.ElementId, elementsSystemObject);
        }

        public void UnregisterFromEvents(IElementsSystemObject elementsSystemObject)
        {
            if (_elementsSystemObjectsDict.ContainsKey(elementsSystemObject.ElementId))
                _elementsSystemObjectsDict.Remove(elementsSystemObject.ElementId);
        }


        public void RegisterForProcessing(IElementsSystemObject elementsSystemObject)
        {
            bool needToRecreateArrays = false;
            if (!_elementsSystemObjects.Contains(elementsSystemObject))
            {
                _elementsSystemObjects.Add(elementsSystemObject);
                needToRecreateArrays = true;
            }
            
            for (var i = 0; i < elementsSystemObject.ElementSystemCollidersOnly.Count; i++)
            {
                if (elementsSystemObject.ElementSystemCollidersOnly[i] == null)
                    continue;

                if (!_collidersMatObjectsMap.ContainsKey(elementsSystemObject.ElementSystemCollidersOnly[i].GetInstanceID()))
                {
                    _collidersMatObjectsMap.Add(elementsSystemObject.ElementSystemCollidersOnly[i].GetInstanceID(), elementsSystemObject);
                }
            }

            if (needToRecreateArrays)
                RecreateArrays();
        }

        public void UnregisterFromProcessing(IElementsSystemObject elementsSystemObject)
        {
            bool needToRecreateArrays = false;
            if (_elementsSystemObjects.Contains(elementsSystemObject))
            {
                _elementsSystemObjects.Remove(elementsSystemObject);
                needToRecreateArrays = true;
            }

            for (var i = 0; i < elementsSystemObject.ElementSystemCollidersOnly.Count; i++)
            {
                if (elementsSystemObject.ElementSystemCollidersOnly[i] == null)
                    continue;

                if (_collidersMatObjectsMap.ContainsKey(elementsSystemObject.ElementSystemCollidersOnly[i].GetInstanceID()))
                {
                    _collidersMatObjectsMap.Remove(elementsSystemObject.ElementSystemCollidersOnly[i].GetInstanceID());
                }
            }

            if (needToRecreateArrays)
                RecreateArrays();

            CheckForDestroyedColliders();
        }

        public List<IElementsSystemObject> GetAllRegisteredObjects()
        {
            return _elementsSystemObjects;
        }

        public void RegisterEvent<T>() where T : struct
        {
            var eventType = typeof(T);
            if (!_registeredEventTypes.Contains(eventType))
            {
                _registeredEventTypes.Add(eventType);
                _eventBus.Register<T>(HandleEvent);
            }
        }
        
        protected override void OnCreate()
        {
            layerMask = DEFAULT_LAYER_MASK;
            maximumObjectCountThatCanBeAffectedByOneObject = 10;
            _eventBus = EventBus.Get();
            _eventBus.Register<NewPeerIsReadyEvent>(HandleEvent);
            _instanceCounter = 0;
        }

        protected override void OnDestroy()
        {
            _instanceCounter = 0;
        }


        protected override void OnUpdate()
        {
            for (var i = 0; i < _elementsSystemObjects.Count; i++)
            {
                _elementsSystemObjects[i].OnBeforeFixedUpdate(Time.fixedDeltaTime);
                CollisionCheck(i, _elementsSystemObjects[i]);
            }

            for (var i = 0; i < affectedElementsSystemObjects.Length; i++)
            {
                if (affectingElementsSystemObjects[i] == null)
                {
                    continue;
                }

                if (affectingElementsSystemObjects[i] == affectedElementsSystemObjects[i])
                {
                    continue;
                }

                affectedElementsSystemObjects[i].OnOverlapWithElementsSystemObject(affectingElementsSystemObjects[i], Time.fixedDeltaTime);
            }

            for (var i = 0; i < _elementsSystemObjects.Count; i++)
            {
                _elementsSystemObjects[i].OnFixedUpdate(Time.fixedDeltaTime);
            }
        }

        void RecreateArrays()
        {
            affectingElementsSystemObjects = new IElementsSystemObject[_elementsSystemObjects.Count * _maximumObjectCountThatCanBeAffectedByOneObject];
            affectedElementsSystemObjects = new IElementsSystemObject[_elementsSystemObjects.Count * _maximumObjectCountThatCanBeAffectedByOneObject];
        }

        void CheckForDestroyedColliders()
        {
            //Not performant, but shouldn't be called often
            List<int> toDestroy = new List<int>();
            foreach(var colliderMatKeyVal in _collidersMatObjectsMap)
            {
                if (colliderMatKeyVal.Value == null)
                {
                    toDestroy.Add(colliderMatKeyVal.Key);
                }
            }

            for (var i = 0; i < toDestroy.Count; i++)
            {
                _collidersMatObjectsMap.Remove(toDestroy[i]);
            }
        }

        BoxBounds _bounds;
        float3 _castSize;
        float _castLength;
        float3 _castOrigin;
        int _hitCount;
        bool _componentPresent;
        IElementsSystemObject affectedElementsSystemObject;
        int currentHitCounts;
        RaycastHit[] _hits = new RaycastHit[64];
        Collider _checkedCollider;
        void CollisionCheck(int whichElement, IElementsSystemObject elementsSystemObject)
        {
            if (!elementsSystemObject.CanInteractWithOthers())
            {
                return;
            }

            currentHitCounts = 0;
            var tmpBackfaceValue = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            for (var i = 0; i < elementsSystemObject.EnvironmentInteractionColliders.Count; i++)
            {
                _checkedCollider = elementsSystemObject.EnvironmentInteractionColliders[i];
                for (int j = 0; j < maximumObjectCountThatCanBeAffectedByOneObject; j++)
                {
                    affectingElementsSystemObjects[whichElement * maximumObjectCountThatCanBeAffectedByOneObject + j] = null;
                }

                if (_checkedCollider == null)
                    continue;

                _bounds = new BoxBounds(_checkedCollider);
                _castSize = (_bounds.size / 2);
                _castLength = Vector3.Project(_castSize, Vector3.up).magnitude;
                _castOrigin = _bounds.center;
                _castOrigin.y -= _castSize.y;

                _hitCount = Physics.BoxCastNonAlloc(_castOrigin, _castSize, Vector3.up, _hits, _checkedCollider.transform.rotation, _castLength, layerMask);
                for (int j = 0; j < _hitCount; j++)
                {
                    if (_hits[j].collider == _checkedCollider)
                        continue;

                    _componentPresent = _collidersMatObjectsMap.TryGetValue(_hits[j].collider.GetInstanceID(), out affectedElementsSystemObject);
                    if (!_componentPresent || !affectedElementsSystemObject.ElementSystemCollidersOnly.Contains(_hits[j].collider))
                    {
                        continue;
                    }

                    currentHitCounts++;
                    if (currentHitCounts >= maximumObjectCountThatCanBeAffectedByOneObject)
                    {
                        continue;
                    }

                    // Check if not blocked by other objects or walls
                    affectingElementsSystemObjects[whichElement * maximumObjectCountThatCanBeAffectedByOneObject + currentHitCounts] = elementsSystemObject;
                    affectedElementsSystemObjects[whichElement * maximumObjectCountThatCanBeAffectedByOneObject + currentHitCounts] = affectedElementsSystemObject;
                }
            }
            Physics.queriesHitBackfaces = tmpBackfaceValue;
        }

        public static void FindCollidersRecursively(IElementsSystemObject elementsSystemObject, List<Collider> colliderListToFill, List<Collider> notIncludedColliders, Transform parent)
        {
            var tmpElementsSystemObject = parent.GetComponent<IElementsSystemObject>();
            if (tmpElementsSystemObject != null && elementsSystemObject != tmpElementsSystemObject)
            {
                return;
            }

            var colliders = parent.GetComponents<Collider>();
            if (colliders != null)
            {
                if (notIncludedColliders != null)
                {
                    foreach (var collider in colliders)
                    {
                        if (!notIncludedColliders.Contains(collider))
                            colliderListToFill.Add(collider);
                    }
                }
                else
                {
                    colliderListToFill.AddRange(colliders);
                }
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                FindCollidersRecursively(elementsSystemObject, colliderListToFill, notIncludedColliders, parent.GetChild(i));
            }
        }

        void HandleEvent<T>(T data) where T : struct
        {
            if (data is IElementsSystemEvents elementsSystemEvent)
            {
                var elementsSystemObject = _elementsSystemObjectsDict[elementsSystemEvent.ElementId];
                elementsSystemObject.HandleEvent(data);
            }
            else if (data is NewPeerIsReadyEvent)
            {
                //slow but this should happen just once per player; trying to avoid additional list
                foreach (var elementsSystemObject in _elementsSystemObjectsDict)
                {
                    elementsSystemObject.Value.HandleEvent(data);
                }
            }
        }
    }
}