using NBG.Core;
using NBG.LogicGraph;
using NBG.Net;
using Recoil;
using System;
using UnityEditor;
using UnityEngine;

namespace NBG.Wind
{
    public enum WindmakerMode
    {
        Suck,
        Blow
    }

    public enum WindZoneVolumeType
    {
        Cylinder,
        Box
    }

    [System.Serializable]
    public class WindZone : MonoBehaviour, IOnFixedUpdate, IManagedBehaviour, INetBehavior
    {
        [SerializeField]
        private WindmakerMode mode;
        [SerializeField]
        private WindZoneVolumeType volumeType;
        [SerializeField]
        private LayerMask affectedLayers;

        [SerializeField]
        protected Vector3 airZoneOffset;

        [SerializeField]
        protected float airZoneLength;
        [SerializeField]
        protected Vector2 airZoneDimensions = Vector2.one;

        [NodeAPI("AirZoneLength")]
        public float AirZoneLength
        {
            get => airZoneLength;
            set => airZoneLength = value;
        }

        [SerializeField]
        protected float airZoneRadius;
        [NodeAPI("AirZoneRadius")]
        public float AirZoneRadius
        {
            get => airZoneRadius;
            set => airZoneRadius = value;
        }

        [SerializeField]
        public bool oscillatingPower = true;
        [NodeAPI("OscillatePower")]
        public bool OscillatingPower
        {
            get => oscillatingPower;
            set => oscillatingPower = value;
        }

        [SerializeField]
        protected float oscillatorFrequency = 1;

        [NodeAPI("PowerMulti")]
        public float PowerMulti { get; set; } = 1;

        [NodeAPI("OnLastBlockerObjectDistanceChange")]
        public event Action<float> onLastBlockerObjectDistanceChange;

        [SerializeField]
        protected float forceMin = 10f;
        [SerializeField]
        protected float forceMax = 20f;
        [SerializeField]
        private AnimationCurve forceFalloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0) { inTangent = -2.5f, outTangent = -2.5f });
        private float oscillatorTimer;
        private float oscillatorState;

        private RaycastHit[] hitBodies = new RaycastHit[128];
        private RaycastHit[] reachabilityChecks = new RaycastHit[32];
        private RaycastHit[] auxCasts = new RaycastHit[16];

        private ReBody reBody;

        float lastfarthestViableReceiver = float.MinValue;
        public const float powerOffAccuracy = 0.01f;

        protected Vector3 windAxis = Vector3.forward;
        public Vector3 WindDirection { get; private set; }
        protected Quaternion BoxCastRoation { get; set; }
        protected Vector3 BoxCastSize { get; set; }
        protected Vector3 CastOrigin { get; set; }
        protected float CastOffset { get; set; }
        public Vector3 AirZoneStart { get; private set; }
        public WindmakerMode Mode => mode;

        public bool Enabled => isActiveAndEnabled;

        /// <summary>
        /// Does this object block wind
        /// </summary>
        /// <param name="hitCollider"></param>
        /// <returns></returns>
        protected virtual bool IsBlockerObject(Collider hitCollider)
        {
            return false;
        }

        /// <summary>
        /// Can this object be ignored by wind (it wont recieve any forces)
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        protected virtual bool IgnoreObject(Collider collider)
        {
            return false;
        }

        public void OnLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        public void OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        public void OnFixedUpdate()
        {
            BlowWind(PowerMulti);
        }

        public void OnAfterLevelLoaded()
        {
            var rig = GetComponent<Rigidbody>();
            if (rig != null)
            {
                new ReBody(rig);
            }
        }

        public void SetWindmakerMode(WindmakerMode mode)
        {
            this.mode = mode;
        }

        void UpdateWindParameters()
        {
            WindDirection = transform.TransformDirection(windAxis);
            AirZoneStart = transform.position + transform.TransformVector(airZoneOffset);

            if (volumeType == WindZoneVolumeType.Box)
            {
                BoxCastRoation = (reBody.BodyExists ? reBody.rotation : transform.rotation) * (Quaternion.LookRotation(windAxis, windAxis.y == 0 ? Vector3.up : Vector3.right));
                BoxCastSize = new Vector3(airZoneDimensions.x / 2, airZoneDimensions.y / 2, Mathf.Min(airZoneDimensions.y / 2, airZoneLength / 2));
                CastOffset = BoxCastSize.z * 1.1f;
            }
            else
            {
                CastOffset = airZoneRadius * 1.1f;
            }

            CastOrigin = AirZoneStart - WindDirection * CastOffset;

        }

        /// <summary>
        /// Blows wind
        /// </summary>
        /// <param name="power">Initial power multiplier, goes 0 - 1, 0 being no wind, 1 - maximum wind</param>
        void BlowWind(float power)
        {
            UpdateWindParameters();

            power = Mathf.Clamp01(power);

            var airzoneCenter = AirZoneStart;
            float farthestViableReceiver = 0;

            if (power > powerOffAccuracy)
            {
                int hitCount;
                if (volumeType == WindZoneVolumeType.Cylinder)
                {
                    hitCount = Physics.SphereCastNonAlloc(CastOrigin, airZoneRadius, WindDirection, hitBodies, AirZoneLength, affectedLayers, QueryTriggerInteraction.Collide);
                }
                else
                {
                    hitCount = Physics.BoxCastNonAlloc(CastOrigin, BoxCastSize, WindDirection, hitBodies, BoxCastRoation, AirZoneLength, affectedLayers, QueryTriggerInteraction.Collide);
                }

                if (oscillatingPower)
                    UpdateOscillatorState();

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = hitBodies[i];

                    if (IgnoreObject(hit.collider))
                        continue;

                    if (reBody.BodyExists || hit.rigidbody != reBody.rigidbody)
                    {
                        hit = FixUnityCastingOverlapIssues(hit);

                        if (AirCanReachBody(hit))
                        {
                            if (hit.distance > farthestViableReceiver && IsBlockerObject(hit.collider))
                            {
                                farthestViableReceiver = hit.distance;
                            }

                            var rig = hit.rigidbody;
                            int bodyCount = 1;
                            if (rig != null)
                            {
                                bodyCount = rig.GetBodyRigidbodyCount();
                            }

                            AddForce(power / bodyCount, airzoneCenter, hit);
                        }
                    }
                }
            }

            farthestViableReceiver -= CastOffset;
            farthestViableReceiver = farthestViableReceiver > 0 ? farthestViableReceiver : airZoneLength;
            farthestViableReceiver = Mathf.Min(farthestViableReceiver, airZoneLength);

            if (lastfarthestViableReceiver != farthestViableReceiver)
                onLastBlockerObjectDistanceChange?.Invoke(farthestViableReceiver);

            lastfarthestViableReceiver = farthestViableReceiver;
        }

        void AddForce(float power, Vector3 airzoneCenter, RaycastHit hit)
        {
            var rig = hit.rigidbody;

            //Sucking objects pulls them towards wind maker center - so they would stick and follow its movement.
            //Blowing objects pushes them straight, so they could be moved in predictable manner.
            var windDirection = mode == WindmakerMode.Blow ? WindDirection : (airzoneCenter - hit.point).normalized;
            var windReceiverMultiplier = GetWindMultiplier(hit, windDirection);
            var forceFalloffMulti = forceFalloff.Evaluate(hit.distance / airZoneLength);
            var force = oscillatingPower ? Mathf.Lerp(forceMin, forceMax, oscillatorState) : forceMax;

            var squaredStrength = force * forceFalloffMulti;
            var finalForce = windDirection * squaredStrength * power * windReceiverMultiplier;

            if (hit.collider.TryGetComponent<IWindReceiver>(out var windReceiver))
            {
                windReceiver.OnReceiveWind(finalForce);
            }
            // Add rigidbody forces, but only for non-triggers.
            // Triggers can still use OnReceiveWind for handling wind
            else if (rig != null && !hit.collider.isTrigger)
            {
                var forcePosition = hit.point;
                var rigReBody = new ReBody(rig);
                rigReBody.AddForceAtPosition(finalForce, forcePosition, ForceMode.Force);
            }
        }

        private void UpdateOscillatorState()
        {
            oscillatorTimer += Time.fixedDeltaTime;
            if (oscillatorTimer > oscillatorFrequency)
                oscillatorTimer = 0f;

            var progress = oscillatorTimer / oscillatorFrequency;
            oscillatorState = 1 - (Mathf.Cos(Mathf.PI * 2 * progress) + 1f) / 2f;
        }

        private RaycastHit FixUnityCastingOverlapIssues(RaycastHit hit)
        {
            //hit.distance from sphere cast if not the one we need, so need to fix that for all cases

            var hitCollider = hit.collider;

            if (hit.point != Vector3.zero)
            {
                hit.distance = (hit.point - CastOrigin).magnitude;
                return hit;
            }
            else // When spherecasting to a nearby object the point and distance will be zero. We need to find the actual point and distance
            {
                var toBody = hitCollider.transform.position - AirZoneStart;

                var hits = Physics.RaycastNonAlloc(CastOrigin, WindDirection, auxCasts, toBody.magnitude + CastOffset, affectedLayers, QueryTriggerInteraction.Collide);

                for (int i = 0; i < hits; i++)
                {
                    if (auxCasts[i].collider == hitCollider)
                    {
                        return auxCasts[i];
                    }
                }
            }
            // even worse approximation
            hit.point = hitCollider.transform.position;
            hit.distance = (hit.point - CastOrigin).magnitude;

            return hit;
        }

        private float GetWindMultiplier(RaycastHit hit, Vector3 windDirection)
        {
            var windMultiplier = hit.collider.GetComponentInChildren<IWindMultiplier>();
            if (windMultiplier == null && hit.rigidbody != null)
            {
                windMultiplier = hit.rigidbody.GetComponentInChildren<IWindMultiplier>();
            }
            if (windMultiplier != null)
                return windMultiplier.GetWindMultiplier(windDirection);
            return 1f;
        }

        private bool AirCanReachBody(RaycastHit hit)
        {
            var hitCount = Physics.RaycastNonAlloc(CastOrigin, (hit.point - CastOrigin).normalized, reachabilityChecks, hit.distance + CastOffset, affectedLayers);

            //sort hits by distance, since RaycastNonAlloc does not guarantee order
            ArrayUtilities.InsertionSort(reachabilityChecks, hitCount, WindUtils.Comparer);

            var originHitCollider = hit.collider;
            var originHitRigidbody = originHitCollider.attachedRigidbody;

            //loops until finds the target object or a blocker object in its path
            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = reachabilityChecks[i].collider;
                var hitRigidbody = hitCollider.attachedRigidbody;
                //target object
                if ((originHitRigidbody != null && hitRigidbody != null && hitRigidbody == originHitRigidbody) ||
                    hitCollider == originHitCollider)
                {
                    return true;
                }
                //not the target object AND a blocker
                else if (IsBlockerObject(hitCollider))
                {
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR

        #region Gizmos

        public void OnDrawGizmos()
        {
            var height = Application.isPlaying ? Mathf.Min(lastfarthestViableReceiver, airZoneLength) : airZoneLength;

            if (!Application.isPlaying)
                UpdateWindParameters();

            if (volumeType == WindZoneVolumeType.Cylinder)
            {
                DrawCapsule(height, Color.red);
                DrawCapsule(airZoneLength, Color.green);

                void DrawCapsule(float height, Color color)
                {
                    var castCenter = (AirZoneStart + (AirZoneStart + WindDirection * height)) / 2;
                    DrawWireCapsule(
                        castCenter,
                        transform.rotation * Quaternion.Euler(new Vector3(90, 0, 0)),
                        airZoneRadius,
                        height,
                        color);
                }
            }
            else
            {
                DrawBox(height, Color.red);
                DrawBox(airZoneLength, Color.green);

                void DrawBox(float height, Color color)
                {
                    Gizmos.color = color;
                    var prevMatrix = Gizmos.matrix;
                    var castCenter = (AirZoneStart + (AirZoneStart + WindDirection * height)) / 2;
                    var scale = new Vector3(airZoneDimensions.x, airZoneDimensions.y, height);
                    Gizmos.matrix = Matrix4x4.TRS(castCenter, BoxCastRoation, scale);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = prevMatrix;
                }
            }
        }

        public void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color = default(Color))
        {
            if (color != default(Color))
                Handles.color = color;

            Matrix4x4 angleMatrix = Matrix4x4.TRS(position, rotation, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = height / 2;

                Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));

                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);
            }
        }

        #endregion
#endif

        #region Network

        public void OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Server:
                    OnFixedUpdateSystem.Register(this);
                    break;
                case NetworkAuthority.Client:
                    OnFixedUpdateSystem.Unregister(this);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        #endregion
    }
}
