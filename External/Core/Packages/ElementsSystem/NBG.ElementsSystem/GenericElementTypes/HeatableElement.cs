using UnityEngine;
using System;
using NBG.Core;
using NBG.Core.GameSystems;
using System.Collections.Generic;
using NBG.Core.Events;
using NBG.Net;
using NBG.Core.Streams;
using NBG.Core.ObjectIdDatabase;
using UnityEngine.Scripting;

namespace NBG.ElementsSystem
{
    public struct HeatableHeatedEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        public bool withEffects;
    }

    [NetEventBusSerializer]
    public class HeatableHeatedEvent_NetSerializer : IEventSerializer<HeatableHeatedEvent>
    {
        const int BIT_SMALL = 4;
        const int BIT_LARGE = 32;

        public HeatableHeatedEvent Deserialize(IStreamReader reader)
        {
            var data = new HeatableHeatedEvent();
            //data.GameObjectReference = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);
            data.ElementId = reader.ReadInt32(BIT_SMALL, BIT_LARGE);
            data.withEffects = reader.ReadBool();
            return data;
        }
        public void Serialize(IStreamWriter writer, HeatableHeatedEvent data)
        {
            //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectReference);
            writer.Write(data.ElementId, BIT_SMALL, BIT_LARGE);
            writer.Write(data.withEffects);
        }
    }

    public struct HeatableCooledEvent : IElementsSystemEvents
    {
        public int ElementId { get; set; }
        //GameObject GameObjectReference { get; }//todo switch to this after GameObjectReference will suport dynamic objects
        public bool withEffects;
    }

    [NetEventBusSerializer]
    public class HeatableColledEvent_NetSerializer : IEventSerializer<HeatableCooledEvent>
    {
        const int BIT_SMALL = 4;
        const int BIT_LARGE = 32;

        public HeatableCooledEvent Deserialize(IStreamReader reader)
        {
            var data = new HeatableCooledEvent();
            //data.GameObjectReference = ObjectIdDatabaseResolver.instance.ReadGameObject(reader);
            data.ElementId = reader.ReadInt32(BIT_SMALL, BIT_LARGE);
            data.withEffects = reader.ReadBool();
            return data;
        }
        public void Serialize(IStreamWriter writer, HeatableCooledEvent data)
        {
            //ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.GameObjectReference);
            writer.Write(data.ElementId, BIT_SMALL, BIT_LARGE);
            writer.Write(data.withEffects);
        }
    }

    /// <summary>
    /// Generic heatable which can be heated up or cooled down
    /// </summary>
    public class HeatableElement : ElementsSystemObject, IHeatable, IManagedBehaviour, INetBehavior, INetStreamer
    {
        [Preserve]
        private static void PreserveEvents()
        {
            HeatableElement heatableElement = new GameObject("DummyObj").AddComponent<HeatableElement>();
            heatableElement.HandleEvent(new HeatableHeatedEvent());
            heatableElement.HandleEvent(new HeatableCooledEvent());

            throw new Exception("Shouldn't be called. This method is just for preserving events from stripping");
        }

        public static HeatableElement AddHeatableElementComponent(
            GameObject targetGO,
            float heatThreshold, float timeToHeat, float timeToCoolDown,
            bool canBeHeatedByFlammables, bool canBeHeatedByHeatables, float heatAbsorptionResistance,
            bool canBeCooledByExtinguishers, float extinguisherMultiplier,
            float heatAmountOnStart)
        {
            HeatableElement heatable = targetGO.AddComponent<HeatableElement>();
            heatable.HeatThreshold = heatThreshold;
            heatable.TimeToHeat = timeToHeat;
            heatable.TimeToCoolDown = timeToCoolDown;
            heatable.CanBeHeatedByFlammables = canBeHeatedByFlammables;
            heatable.CanBeHeatedByHeatables = canBeHeatedByHeatables;
            heatable.HeatAbsorptionResistance = heatAbsorptionResistance;
            heatable.CanBeCooledByExtinguishers = canBeCooledByExtinguishers;
            heatable.ExtinguisherMultiplier = extinguisherMultiplier;

            heatable._heatAmountOnStart = heatAmountOnStart;

            return heatable;
        }

        [SerializeField]
        protected List<Collider> _heatTransferCollidersOnly = new List<Collider>();

        bool _isClient = false;
        protected ElementsGameSystem _elementsSystem;
        bool _isInitialized = false;

        protected float _insideHeatSourceStrength = 0;
        protected float _extinguisherStrength = 0;

        IEventBus _eventBus;
        protected HeatableCooledEvent? _cooledEvent;
        protected HeatableHeatedEvent? _heatedEvent;

        #region IHeatable
        [SerializeField]
        protected HeatableElementSettings _heatableSettings;
        protected HeatableElementSettings HeatableSettings { get => _heatableSettings; }

        public float HeatThreshold { get; protected set; }
        public float TimeToHeat { get; protected set; }
        public float TimeToCoolDown { get; protected set; }
        public bool CanBeHeatedByFlammables { get; protected set; }
        public bool CanBeHeatedByHeatables { get; protected set; }
        public float HeatAbsorptionResistance { get; protected set; }
        public bool CanBeCooledByExtinguishers { get; protected set; }
        public float ExtinguisherMultiplier { get; protected set; }


        public event ElementsSystemEventWithEffectsHandler OnHeated;
        public event ElementsSystemEventWithEffectsHandler OnCooledDown;
        public event Action<float> OnHeatAmountChanged;

        [SerializeField, ReadOnlyInPlayModeField, Range(0f, 1f)]
        protected float _heatAmountOnStart;

        float _heatAmount;
        public float HeatAmount
        {
            get => _heatAmount;
            protected set
            {
                _heatAmount = value;
                OnHeatAmountChanged?.Invoke(_heatAmount);
            }
        }

        protected bool _isHeated;

        public void SetHeatChanged(float heatAmount)
        {
            var wasHeated = HeatAmount > HeatThreshold;
            
            if (HeatAmount != heatAmount)
            {
                HeatAmount = heatAmount;
            }

            bool isHeated = HeatAmount > HeatThreshold;

            if (isHeated && isHeated != wasHeated)
            {
                var evt = new HeatableHeatedEvent()
                {
                    //GameObjectReference = gameObject,
                    ElementId = ElementId,
                    withEffects = true,
                };
                _eventBus.Send(evt);
                _cooledEvent = null;
                _heatedEvent = evt;
            }
            else if (!isHeated && isHeated != wasHeated)
            {
                var evt = new HeatableCooledEvent()
                {
                    //GameObjectReference = gameObject,
                    ElementId = ElementId,
                    withEffects = true,
                };
                _eventBus.Send(evt);
                _cooledEvent = evt;
                _heatedEvent = null;
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

            _elementsSystem.RegisterEvent<HeatableHeatedEvent>();
            _elementsSystem.RegisterEvent<HeatableCooledEvent>();

            _eventBus = EventBus.Get();

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
            if (HeatableSettings != null)
            {
                HeatThreshold = HeatableSettings.HeatThreshold;
                TimeToHeat = HeatableSettings.TimeToHeat;
                TimeToCoolDown = HeatableSettings.TimeToCoolDown;
                CanBeHeatedByFlammables = HeatableSettings.CanBeHeatedByFlammables;
                CanBeHeatedByHeatables = HeatableSettings.CanBeHeatedByHeatables;
                HeatAbsorptionResistance = HeatableSettings.HeatAbsorptionResistance;
                CanBeCooledByExtinguishers = HeatableSettings.CanBeCooledByExtinguishers;
                ExtinguisherMultiplier = HeatableSettings.ExtinguisherMultiplier;
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
            return HeatAmount >= HeatThreshold;
        }

        public override void ResetState()
        {
            //TODO: But we can start already inside of something?
            _insideHeatSourceStrength = 0;
            _extinguisherStrength = 0;
            HeatAmount = _heatAmountOnStart;
            _cooledEvent = null;
            _heatedEvent = null;

            OnHeatAmountChanged?.Invoke(HeatAmount);
            if (_isHeated)
                OnHeated?.Invoke(false);
            else
                OnCooledDown?.Invoke(false);
        }

        protected override void OnBeforeFixedUpdate(float fixedDeltaTim)
        {
        }

        protected override void OnOverlapWithElementsSystemObjectInternal(IElementsSystemObject overlap, float fixedDeltaTime)
        {
            if (overlap is IExtinguisher extinguisher)
            {
                ManageExtinguisherInteraction(extinguisher, fixedDeltaTime);
            }

            if (overlap is IFlammable flammable)
            {
                ManageFlammableInteraction(flammable, fixedDeltaTime);
            }

            if (overlap is IHeatable heatable)
            {
                ManageHeatableInteraction(heatable, fixedDeltaTime);
            }
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            ManageEnvironmentInteraction(fixedDeltaTime);

            _insideHeatSourceStrength = 0;
            _extinguisherStrength = 0;
        }

        protected override void HandleEvent<T>(T data) where T : struct
        {
            if (data is HeatableHeatedEvent heatedEvent)
            {
                _isHeated = true;
                OnHeated?.Invoke(heatedEvent.withEffects);
            }
            else if (data is HeatableCooledEvent cooledEvent)
            {
                _isHeated = false;
                OnCooledDown?.Invoke(cooledEvent.withEffects);
            }
            else if(data is NewPeerIsReadyEvent newPeedEvent)
                OnNewPeerIsReady(newPeedEvent);
        }
        #endregion

        protected virtual void ManageEnvironmentInteraction(float fixedDeltaTime)
        {
            if (_extinguisherStrength > 0)
            {
                if (TimeToCoolDown > 0 && HeatAmount > 0)
                {
                    float coolDownTime = TimeToCoolDown - (TimeToCoolDown * ExtinguisherMultiplier * _extinguisherStrength);
                    if (coolDownTime > 0)
                    {
                        SetHeatChanged(Mathf.Clamp01(Mathf.MoveTowards(HeatAmount, 0, (1 / coolDownTime) * fixedDeltaTime)));
                    }
                    else
                    {
                        SetHeatChanged(0);
                    }
                }
            }
            else if (_insideHeatSourceStrength > 0 && HeatAmount < _insideHeatSourceStrength)
            {
                SetHeatChanged(Mathf.Clamp(Mathf.MoveTowards(HeatAmount, _insideHeatSourceStrength, (1 / TimeToHeat) * fixedDeltaTime), 0, _insideHeatSourceStrength));
            }    
            else
            {
                if (TimeToCoolDown > 0 && HeatAmount > 0)
                {
                    if (TimeToCoolDown > 0)
                    {
                        SetHeatChanged(Mathf.Clamp(Mathf.MoveTowards(HeatAmount, 0, (1 / TimeToCoolDown) * fixedDeltaTime), 0, HeatAmount));
                    }
                    else
                    {
                        SetHeatChanged(0);
                    }
                }
            }
        }

        protected virtual void ManageExtinguisherInteraction(IExtinguisher overlap, float fixedDeltaTime)
        {
            if (CanBeCooledByExtinguishers)
            {
                _extinguisherStrength += overlap.ExtinguisherStrength;
            }
        }

        protected virtual void ManageHeatableInteraction(IHeatable overlap, float fixedDeltaTime)
        {
            if (CanBeHeatedByHeatables && overlap.HeatAmount >= overlap.HeatThreshold)
            {
                if (_insideHeatSourceStrength < overlap.HeatAmount)
                    _insideHeatSourceStrength = Mathf.Pow(overlap.HeatAmount, HeatAbsorptionResistance);
            }
        }

        protected virtual void ManageFlammableInteraction(IFlammable overlap, float fixedDeltaTime)
        {
            if (CanBeHeatedByFlammables && overlap.IsBurning)
            {
                if (_insideHeatSourceStrength < overlap.FlameAmount)
                    _insideHeatSourceStrength = Mathf.Pow(overlap.FlameAmount, HeatAbsorptionResistance);
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
            if (_cooledEvent != null)
            {
                var eventWithoutEffects = _cooledEvent.Value;
                eventWithoutEffects.withEffects = false;
                e.CallOnPeer(eventWithoutEffects);
            }
            else if (_heatedEvent != null)
            {
                var eventWithoutEffects = _heatedEvent.Value;
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
            var val = HeatAmount.Quantize(MAX, BITS);
            stream.Write(val, BITS);
        }

        void INetStreamer.ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames)
        {
            var val0 = state0.ReadInt32(BITS).Dequantize(MAX, BITS);
            var val1 = state1.ReadInt32(BITS).Dequantize(MAX, BITS);
            HeatAmount = Mathf.Lerp(val0, val1, mix);
        }

        void INetStreamer.ApplyState(IStreamReader state)
        {
            HeatAmount = state.ReadInt32(BITS).Dequantize(MAX, BITS);
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
