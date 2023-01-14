using System.Collections.Generic;
using UnityEngine;

namespace NBG.ElementsSystem
{
    public interface IElementsSystemObject
    {
        int ElementId { get; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        bool CanInteractWithOthers();

        /// <summary>
        /// These objects is interacted with
        /// </summary>
        List<Collider> ElementSystemCollidersOnly { get; }
        /// <summary>
        /// These objects interact with another objects
        /// </summary>
        List<Collider> EnvironmentInteractionColliders { get; }

        void ResetState();

        void OnBeforeFixedUpdate(float fixedDeltaTime);
        void OnOverlapWithElementsSystemObject(IElementsSystemObject overlapObject, float fixedDeltaTime);
        void OnFixedUpdate(float fixedDeltaTime);
        void HandleEvent<T>(T data) where T : struct;
    }
}
