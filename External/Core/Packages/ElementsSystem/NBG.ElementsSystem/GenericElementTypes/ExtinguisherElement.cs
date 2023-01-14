using NBG.Core;
using NBG.Core.GameSystems;
using NBG.Net;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Generic extinguisher object which can extinguish Flammable and coolDown Heatable
    /// </summary>
    public class ExtinguisherElement : ElementsSystemObject, IExtinguisher, IManagedBehaviour, INetBehavior
    {
        public static ExtinguisherElement AddExtinguisherElementComponent(GameObject targetGO, float extinguisherStrength)
        {
            ExtinguisherElement extinguishable = targetGO.AddComponent<ExtinguisherElement>();
            extinguishable.ExtinguisherStrength = extinguisherStrength;

            return extinguishable;
        }

        bool _isClient = false;
        protected ElementsGameSystem _elementsSystem;
        bool _isInitialized;

        #region IExtinguisher
        [SerializeField]
        protected ExtinguisherElementSettings _extinguisherSettings;
        public float ExtinguisherStrength { get; protected set; }
        #endregion

        #region IManagedBehaviour
        protected virtual void OnEnable()
        {
            if (_isInitialized)
            {
                _elementsSystem.RegisterForEvents(this);
                if (!_isClient)
                    _elementsSystem.RegisterForProcessing(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_isInitialized)
            {
                if (_elementsSystem != null)
                {
                    _elementsSystem.UnregisterFromEvents(this);
                    _elementsSystem.UnregisterFromProcessing(this);
                }
            }
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            Initialize();

            if (_elementsSystem == null)
                _elementsSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ElementsGameSystem>();

            if (ElementId == -1)
                ElementId = _elementsSystem.GenerateUniqueElementsId();

            if (gameObject.activeInHierarchy)
            {
                _elementsSystem.RegisterForEvents(this);
                if (!_isClient)
                    _elementsSystem.RegisterForProcessing(this);
            }

            ResetState();
        }

        protected virtual void Initialize()
        {
            if (_extinguisherSettings != null)
            {
                ExtinguisherStrength = _extinguisherSettings.ExtinguisherStrength;
            }

            ElementsGameSystem.FindCollidersRecursively(this, ElementSystemCollidersOnly, null, transform);
            EnvironmentInteractionColliders = ElementSystemCollidersOnly;
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            _isInitialized = true;
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            if (_elementsSystem != null)
            {
                _elementsSystem.UnregisterFromEvents(this);
                _elementsSystem.UnregisterFromProcessing(this);
            }
        }
        #endregion

        #region ElementsSystemObject
        protected override bool CanInteractWithOthers()
        {
            return true;
        }

        public override void ResetState()
        {
        }

        protected override void OnBeforeFixedUpdate(float fixedDeltaTime)
        {
        }

        protected override void OnOverlapWithElementsSystemObjectInternal(IElementsSystemObject overlapObject, float fixedDeltaTime)
        {
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
        }

        protected override void HandleEvent<T>(T data) where T : struct
        {
        }
        #endregion

        #region INetBehavior
        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Client:
                    _isClient = true;
                    _elementsSystem.UnregisterFromProcessing(this);
                    break;
                case NetworkAuthority.Server:
                    _isClient = false;
                    _elementsSystem.RegisterForProcessing(this);
                    break;
            }
        }
        #endregion
    }
}
