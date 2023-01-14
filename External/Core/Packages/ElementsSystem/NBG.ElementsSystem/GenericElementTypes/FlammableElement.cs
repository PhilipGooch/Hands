using NBG.Core;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Core.ObjectIdDatabase;
using NBG.Core.Streams;
using NBG.Net;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace NBG.ElementsSystem
{
    public struct FlameIgnitedEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        public bool withEffects;
    }

    [NetEventBusSerializer]
    public class FlameIgnitedEvent_NetSerializer : IEventSerializer<FlameIgnitedEvent>
    {
        const int BIT_SMALL = 4;
        const int BIT_LARGE = 32;

        public FlameIgnitedEvent Deserialize(IStreamReader reader)
        {
            var data = new FlameIgnitedEvent();
            //data.GameObjectReference = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);
            data.ElementId = reader.ReadInt32(BIT_SMALL, BIT_LARGE);
            data.withEffects = reader.ReadBool();
            return data;
        }
        public void Serialize(IStreamWriter writer, FlameIgnitedEvent data)
        {
            //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectReference);
            writer.Write(data.ElementId, BIT_SMALL, BIT_LARGE);
            writer.Write(data.withEffects);
        }
    }

    public struct FlameExtinguisherEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        public bool withEffects;
    }

    [NetEventBusSerializer]
    public class FlameExtinguisherEvent_NetSerializer : IEventSerializer<FlameExtinguisherEvent>
    {
        const int BIT_SMALL = 4;
        const int BIT_LARGE = 32;

        public FlameExtinguisherEvent Deserialize(IStreamReader reader)
        {
            var data = new FlameExtinguisherEvent();
            //data.GameObjectReference = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);
            data.ElementId = reader.ReadInt32(BIT_SMALL, BIT_LARGE);
            data.withEffects = reader.ReadBool();
            return data;
        }
        public void Serialize(IStreamWriter writer, FlameExtinguisherEvent data)
        {
            //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectReference);
            writer.Write(data.ElementId, BIT_SMALL, BIT_LARGE);
            writer.Write(data.withEffects);
        }
    }

    public struct FlameBurnedOutEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        public bool withEffects;
    }

    [NetEventBusSerializer]
    public class FlameBurnedOutEvent_NetSerializer : IEventSerializer<FlameBurnedOutEvent>
    {
        const int BIT_SMALL = 4;
        const int BIT_LARGE = 32;

        public FlameBurnedOutEvent Deserialize(IStreamReader reader)
        {
            var data = new FlameBurnedOutEvent();
            //data.GameObjectReference = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);
            data.ElementId = reader.ReadInt32(BIT_SMALL, BIT_LARGE);
            data.withEffects = reader.ReadBool();
            return data;
        }
        public void Serialize(IStreamWriter writer, FlameBurnedOutEvent data)
        {
            //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectReference);
            writer.Write(data.ElementId, BIT_SMALL, BIT_LARGE);
            writer.Write(data.withEffects);
        }
    }

    /// <summary>
    /// Generic Flammable object which can be ignited based on settings and extinjguished by all extinguishers
    /// </summary>
    public class FlammableElement : ElementsSystemObject, IFlammable, IManagedBehaviour, INetBehavior, INetStreamer
    {
        [Preserve]
        private static void PreserveEvents()
        {
            FlammableElement flammableElement = new GameObject("DummyObj").AddComponent<FlammableElement>();
            flammableElement.HandleEvent(new FlameIgnitedEvent());
            flammableElement.HandleEvent(new FlameExtinguisherEvent());
            flammableElement.HandleEvent(new FlameBurnedOutEvent());

            throw new Exception("Shouldn't be called. This method is just for preserving events from stripping");
        }

        public static FlammableElement AddFlammableElementComponent(GameObject targetGO,
            float burnTime, float timeToIgnite, float timeBeforeSelfExtinguish, float timeBeforeSelfIgnite, float timeForFireIncrease, bool canBeIgnitedByFlammables,
            bool canBeIgnitedByHeatable, float heatableIgniteThreshold, bool canBeExtinguishedByExtinguishers, float minimumExtinguisherStrengthNeeded,
            bool isBurningOnStart, bool isBurnedOutOnStart)
        {
            FlammableElement flammable = targetGO.AddComponent<FlammableElement>();
            flammable.BurnTime = burnTime;
            flammable.TimeToIgnite = timeToIgnite;
            flammable.TimeBeforeSelfExtinguish = timeBeforeSelfExtinguish;
            flammable.TimeBeforeSelfIgnite = timeBeforeSelfIgnite;
            flammable.TimeForFireIncrease = timeForFireIncrease;
            flammable.CanBeIgnitedByFlammables = canBeIgnitedByFlammables;
            flammable.CanBeIgnitedByHeatable = canBeIgnitedByHeatable;
            flammable.HeatableIgniteThreshold = heatableIgniteThreshold;
            flammable.CanBeExtinguishedByExtinguishers = canBeExtinguishedByExtinguishers;
            flammable.MinimumExtinguisherStrengthNeeded = minimumExtinguisherStrengthNeeded;

            flammable._isBurningOnStart = isBurningOnStart;
            flammable._isBurnedOutOnStart = isBurnedOutOnStart;

            return flammable;
        }

        [SerializeField]
        protected List<Collider> _heatTransferCollidersOnly = new List<Collider>();

        bool _isClient = false;
        protected ElementsGameSystem _elementsSystem;
        bool _isInitialized;

        protected bool _insideExtinguishSource = false;
        protected bool _insideHeatSource = false;
        float _lifetimeTimer = 0;
        float _timerInsideFlames = 0;
        float _timerBeforeSelfExtinguish = 0;
        float _timerBeforeSelfIgnite = 0;
        float _targetBurningAmount;

        IEventBus _eventBus;
        protected FlameIgnitedEvent? _igniteEvent;
        protected FlameExtinguisherEvent? _extinguishEvent;
        protected FlameBurnedOutEvent? _burnedOutEvent;

        #region IFlammable
        [SerializeField]
        protected FlammableElementSettings _flammableSettings;
        protected FlammableElementSettings FlammableSettings { get => _flammableSettings; }
        public float BurnTime { get; protected set; }
        public float TimeToIgnite { get; protected set; }
        public float TimeBeforeSelfExtinguish { get; protected set; }
        public float TimeBeforeSelfIgnite { get; protected set; }
        public float TimeForFireIncrease { get; protected set; }
        public bool CanBeIgnitedByFlammables { get; protected set; }
        public bool CanBeIgnitedByHeatable { get; protected set; }
        public float HeatableIgniteThreshold { get; protected set; }
        public bool CanBeExtinguishedByExtinguishers { get; protected set; }
        public float MinimumExtinguisherStrengthNeeded { get; protected set; }

        float _flameAmount;
        public float FlameAmount {
            get => _flameAmount;
            protected set
            {
                _flameAmount = value;
                OnBurningAmountChanged?.Invoke(_flameAmount);
            }
        }

        [SerializeField, ReadOnlyInPlayModeField]
        protected bool _isBurnedOutOnStart;

        [SerializeField, ReadOnlyInPlayModeField]
        protected bool _isBurningOnStart;
        protected bool _isBurning;
        public bool IsBurning => _isBurning;
        protected bool _isBurnedOut;
        public bool IsBurnedOut => _isBurnedOut;


        public event ElementsSystemEventWithEffectsHandler OnExtinguished;
        public event ElementsSystemEventWithEffectsHandler OnIgnited;
        public event Action<float> OnBurningAmountChanged;
        public event ElementsSystemEventWithEffectsHandler OnBurnedOut;

        void IFlammable.Ignite(bool withEffects)
        {
            if (CanBeIgnited())
            {
                _timerInsideFlames = 0;
                _timerBeforeSelfExtinguish = 0;
                _timerBeforeSelfIgnite = 0;
                _targetBurningAmount = 1;

                var evt = new FlameIgnitedEvent()
                {
                    ElementId = ElementId,
                    //GameObjectReference = gameObject,
                    withEffects = withEffects,
                };
                _eventBus.Send(evt);
                _igniteEvent = evt;
                _extinguishEvent = null;
                _burnedOutEvent = null;
            }
        }

        void IFlammable.Extinguish(bool withEffects)
        {
            _timerInsideFlames = 0;
            _timerBeforeSelfIgnite = 0;
            _timerBeforeSelfExtinguish = 0;
            if (IsBurning)
            {
                var evt = new FlameExtinguisherEvent()
                {
                    ElementId = ElementId,
                    //GameObjectReference = gameObject,
                    withEffects = withEffects,
                };
                _eventBus.Send(evt);
                _extinguishEvent = evt;
                _igniteEvent = null;

                _targetBurningAmount = FlameAmount = 0;
                
            }
        }

        public void SetFlameAmount(float flameAmount)
        {
            if (!IsBurning)
            {
                return;
            }

            if (FlameAmount != flameAmount)
            {
                _targetBurningAmount = FlameAmount = flameAmount;
            }
        }
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

            _elementsSystem.RegisterEvent<FlameIgnitedEvent>();
            _elementsSystem.RegisterEvent<FlameExtinguisherEvent>();
            _elementsSystem.RegisterEvent<FlameBurnedOutEvent>();

            if (gameObject.activeInHierarchy)
            {
                _elementsSystem.RegisterForEvents(this);
                if (!_isClient)
                    _elementsSystem.RegisterForProcessing(this);
            }

            _eventBus = EventBus.Get();

            ResetState();
        }

        protected virtual void Initialize()
        {
            if (FlammableSettings != null)
            {
                BurnTime = FlammableSettings.BurnTime;
                TimeToIgnite = FlammableSettings.TimeToIgnite;
                TimeBeforeSelfExtinguish = FlammableSettings.TimeBeforeSelfExtinguish;
                TimeBeforeSelfIgnite = FlammableSettings.TimeBeforeSelfIgnite;
                TimeForFireIncrease = FlammableSettings.TimeForFireIncrease;
                CanBeIgnitedByHeatable = FlammableSettings.CanBeIgnitedByHeatable;
                CanBeIgnitedByFlammables = FlammableSettings.CanBeIgnitedByFlammables;
                HeatableIgniteThreshold = FlammableSettings.HeatableIgniteThreshold;
                CanBeExtinguishedByExtinguishers = FlammableSettings.CanBeExtinguishedByExtinguishers;
                MinimumExtinguisherStrengthNeeded = FlammableSettings.MinimumExtinguisherStrengthNeeded;
            }

            EnvironmentInteractionColliders = _heatTransferCollidersOnly;
            foreach (var environmentCollider in EnvironmentInteractionColliders)
            {
                if (!LayerMask.LayerToName(environmentCollider.gameObject.layer).Equals("Ignore Raycast") || environmentCollider.gameObject == gameObject)
                {
                    Debug.LogWarning($"{gameObject.name}: Make _heatTransferCollidersOnly as child object and make it mark it with IgnoreRaycast layer");
                }
            }
            ElementsGameSystem.FindCollidersRecursively(this, ElementSystemCollidersOnly, EnvironmentInteractionColliders, transform);
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
            return IsBurning;
        }

        public override void ResetState()
        {
            //TODO: But we can start already inside of something?
            _insideExtinguishSource = false;
            _insideHeatSource = false;
            _lifetimeTimer = 0;
            _timerInsideFlames = 0;
            _timerBeforeSelfIgnite = 0;
            _timerBeforeSelfExtinguish = 0;
            _isBurning = _isBurningOnStart;
            FlameAmount = _targetBurningAmount = _isBurningOnStart ? 1 : 0;
            _isBurnedOut = _isBurnedOutOnStart;

            if (_isBurning)
            {
                OnIgnited?.Invoke(false);
            }
            else
            {
                if (_isBurnedOut)
                {
                    OnBurnedOut?.Invoke(false);
                }
                else
                {
                    OnExtinguished?.Invoke(false);
                }
            }
        }

        protected override void OnBeforeFixedUpdate(float fixedDeltaTime)
        {
        }

        protected override void OnOverlapWithElementsSystemObjectInternal(IElementsSystemObject overlap, float fixedDeltaTime)
        {
            if (overlap is IExtinguisher extinguisher)
            {
                if (CanBeExtinguishedByExtinguishers && MinimumExtinguisherStrengthNeeded <= extinguisher.ExtinguisherStrength)
                {
                    ManageExtinguisherInteraction(extinguisher, fixedDeltaTime);
                }
            }
            else if (overlap is IFlammable flammable)
            {
                ManageFlammableInteraction(flammable, fixedDeltaTime);
            }
            else if (overlap is IHeatable heatable)
            {
                ManageHeatableInteraction(heatable, fixedDeltaTime);
            }
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            ManageEnvironment(fixedDeltaTime);
            CalculateBurnAmount(fixedDeltaTime);

            _insideExtinguishSource = false;
            _insideHeatSource = false;
        }

        protected override void HandleEvent<T>(T data) where T : struct
        {
            if (data is FlameIgnitedEvent flameIgnited)
            {
                _isBurning = true;
                OnIgnited?.Invoke(flameIgnited.withEffects);
            }
            else if (data is FlameExtinguisherEvent flameExtinguished)
            {
                _isBurning = false;
                OnExtinguished?.Invoke(flameExtinguished.withEffects);
            }
            else if (data is FlameBurnedOutEvent flameBurnedOut)
            {
                _isBurnedOut = true;
                OnBurnedOut?.Invoke(flameBurnedOut.withEffects);
            }
            else if (data is NewPeerIsReadyEvent newPeedEvent)
                OnNewPeerIsReady(newPeedEvent);
        }
        #endregion

        bool CanBeIgnited()
        {
            return !IsBurning && !IsBurnedOut && !_insideExtinguishSource;
        }

        protected virtual void CalculateBurnAmount(float fixedDeltaTime)
        {
            if (FlameAmount != _targetBurningAmount)
            {
                if (TimeForFireIncrease == 0)
                {
                    FlameAmount = _targetBurningAmount;
                }
                else
                {
                    FlameAmount = Mathf.Clamp01(Mathf.MoveTowards(FlameAmount, _targetBurningAmount, fixedDeltaTime * (1f / TimeForFireIncrease)));
                }
            }
        }

        protected virtual void ManageEnvironment(float fixedDeltaTime)
        {
            if (IsBurning)
            {
                if (BurnTime > 0)
                {
                    _lifetimeTimer += fixedDeltaTime;
                    if (_lifetimeTimer >= BurnTime)
                    {
                        _lifetimeTimer = 0;

                        ((IFlammable)this).Extinguish(true);

                        var evt2 = new FlameBurnedOutEvent()
                        {
                            ElementId = ElementId,
                            //GameObjectReference = gameObject,
                            withEffects = true,
                        };
                        _eventBus.Send(evt2);
                        _burnedOutEvent = evt2;
                    }
                }

                if (_insideExtinguishSource)
                {
                    ((IFlammable)this).Extinguish(true);
                }
                else if (!_insideHeatSource)
                {
                    if (TimeBeforeSelfExtinguish > 0)
                    {
                        _timerBeforeSelfExtinguish += fixedDeltaTime;
                        if (_timerBeforeSelfExtinguish >= TimeBeforeSelfExtinguish)
                        {
                            var evt = new FlameExtinguisherEvent()
                            {
                                ElementId = ElementId,
                                //GameObjectReference = gameObject,
                                withEffects = true,
                            };
                            _eventBus.Send(evt);
                            _extinguishEvent = evt;
                            _igniteEvent = null;
                        }
                    }
                }
            }
            else
            {
                if (!_insideExtinguishSource)
                {
                    if (_insideHeatSource)
                    {
                        if (CanBeIgnited())
                        {
                            _timerInsideFlames += fixedDeltaTime;
                            if (_timerInsideFlames > TimeToIgnite)
                            {
                                ((IFlammable)this).Ignite(true);
                            }
                        }
                    }
                    else if (TimeBeforeSelfIgnite > 0)
                    {
                        if (CanBeIgnited())
                        {
                            _timerBeforeSelfIgnite += fixedDeltaTime;
                            if (_timerBeforeSelfIgnite >= TimeBeforeSelfIgnite)
                            {
                                ((IFlammable)this).Ignite(true);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void ManageExtinguisherInteraction(IExtinguisher overlap, float fixedDeltaTime)
        {
            _insideExtinguishSource = true;
        }

        protected virtual void ManageFlammableInteraction(IFlammable overlap, float fixedDeltaTime)
        {
            if (CanBeIgnitedByFlammables && overlap.IsBurning)
            {
                _insideHeatSource = true;
            }
        }

        protected virtual void ManageHeatableInteraction(IHeatable overlap, float fixedDeltaTime)
        {
            if (CanBeIgnitedByHeatable && overlap.HeatAmount >= HeatableIgniteThreshold)
            {
                _insideHeatSource = true;
            }
        }

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

        void OnNewPeerIsReady(NewPeerIsReadyEvent e)
        {
            if (_igniteEvent != null)
            {
                var eventWithoutEffects = _igniteEvent.Value;
                eventWithoutEffects.withEffects = false;
                e.CallOnPeer(eventWithoutEffects);
            }
            else if (_extinguishEvent != null)
            {
                var eventWithoutEffects = _extinguishEvent.Value;
                eventWithoutEffects.withEffects = false;
                e.CallOnPeer(eventWithoutEffects);
            }

            if (_burnedOutEvent != null)
            {
                var eventWithoutEffects = _burnedOutEvent.Value;
                eventWithoutEffects.withEffects = false;
                e.CallOnPeer(eventWithoutEffects);
            }
        }
        #endregion

        #region INetStreamer
        const float MAX = 1f;
        const int BITS = 8;

        void INetStreamer.CollectState(IStreamWriter stream)
        {
            var val = FlameAmount.Quantize(MAX, BITS);
            stream.Write(val, BITS);
        }

        void INetStreamer.ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames)
        {
            var val0 = state0.ReadInt32(BITS).Dequantize(MAX, BITS);
            var val1 = state1.ReadInt32(BITS).Dequantize(MAX, BITS);
            FlameAmount = Mathf.Lerp(val0, val1, mix);
        }

        void INetStreamer.ApplyState(IStreamReader state)
        {
            FlameAmount = state.ReadInt32(BITS).Dequantize(MAX, BITS);
        }

        void INetStreamer.CalculateDelta(IStreamReader state0, IStreamReader state1, IStreamWriter delta)
        {
            var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
            var q1 = state1.ReadInt32(BITS);
            if (q0 == q1) //Same Value -> single bool
            {
                delta.Write(false);
            }
            else
            {
                delta.Write(true);
                delta.Write(q1, BITS);
            }
        }

        void INetStreamer.AddDelta(IStreamReader state0, IStreamReader delta, IStreamWriter result)
        {
            var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
            if (!delta.ReadBool())
            {
                result.Write(q0, BITS); // not changed
            }
            else
            {
                //changed. We write absolute, because we don't really gain a lot by writing relative, with BITS=4
                //Make sure, if you increase bits to consider writing relative here
                int q1 = delta.ReadInt32(BITS);
                result.Write(q1, BITS);
            }
        }
        #endregion
    }
}
