using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// This class is making sure that interface specific methods is not public, but explicid, but they still can be extended in all inherited classes.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ElementsSystemObject : MonoBehaviour, IElementsSystemObject
    {
        public int ElementId { get; protected set; } = -1;
        //public GameObject GameObjectReference { get => gameObject; }//todo switch to this after GameObjectReference will suport dynamic objects

        public List<Collider> ElementSystemCollidersOnly { get; protected set; } = new List<Collider>();
        public List<Collider> EnvironmentInteractionColliders { get; protected set; } = new List<Collider>();

        bool IElementsSystemObject.CanInteractWithOthers()
        {
            return CanInteractWithOthers();
        }

        protected abstract bool CanInteractWithOthers();

        public abstract void ResetState();

        void IElementsSystemObject.OnBeforeFixedUpdate(float fixedDeltaTime)
        {
            OnBeforeFixedUpdate(fixedDeltaTime);
        }

        protected abstract void OnBeforeFixedUpdate(float fixedDeltaTime);

        void IElementsSystemObject.OnFixedUpdate(float fixedDeltaTime)
        {
            OnFixedUpdate(fixedDeltaTime);
        }

        protected abstract void OnFixedUpdate(float fixedDeltaTime);

        void IElementsSystemObject.OnOverlapWithElementsSystemObject(IElementsSystemObject overlapObject, float fixedDeltaTime)
        {
            OnOverlapWithElementsSystemObjectInternal(overlapObject, fixedDeltaTime);
        }

        protected abstract void OnOverlapWithElementsSystemObjectInternal(IElementsSystemObject overlapObject, float fixedDeltaTime);

        void IElementsSystemObject.HandleEvent<T>(T data) where T : struct
        {
            HandleEvent(data);
        }

        protected abstract void HandleEvent<T>(T data) where T : struct;
    }
}