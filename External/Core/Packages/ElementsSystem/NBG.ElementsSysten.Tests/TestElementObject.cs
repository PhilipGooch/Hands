using NBG.Core.GameSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.ElementsSystem.Tests
{
    public struct CustomElementEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //public GameObject GameObjectReference { get; set; }//todo switch to this after GameObjectReference will suport dynamic objects
    }

    public class TestElementObject : MonoBehaviour, IElementsSystemObject
    {
        public int ElementId { get; set; }
        //public GameObject GameObjectReference { get => gameObject; }//todo switch to this after GameObjectReference will suport dynamic objects

        public List<Collider> ElementSystemCollidersOnly { get; set; } = new List<Collider>();

        public List<Collider> EnvironmentInteractionColliders { get; set; } = new List<Collider>();

        public event Action<IElementsSystemObject> OnOverlapWithElementsSystemObject;
        public event Action<CustomElementEvent> OnCustomElementsSystemEventReceived;

        void IElementsSystemObject.OnBeforeFixedUpdate(float fixedDeltaTime)
        {
        }

        void IElementsSystemObject.OnFixedUpdate(float fixedDeltaTime)
        {
        }

        void IElementsSystemObject.OnOverlapWithElementsSystemObject(IElementsSystemObject overlapElement, float fixedDeltaTime)
        {
            OnOverlapWithElementsSystemObject?.Invoke(overlapElement);
        }

        void IElementsSystemObject.HandleEvent<T>(T data)
        {
            if (data is CustomElementEvent eventData)
            {
                OnCustomElementsSystemEventReceived?.Invoke(eventData);
            }
        }

        bool IElementsSystemObject.CanInteractWithOthers()
        {
            return true;
        }

        void IElementsSystemObject.ResetState()
        {
        }
    }
}
