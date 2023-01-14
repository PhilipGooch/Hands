using NBG.Actor;
using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Impale
{
    sealed public class Connection
    {
        internal ConfigurableJoint joint;
        public ConfigurableJoint Joint => joint;
        internal Collider collider;
        public Collider Collider => collider;

        internal ReBody reBody = default;
        //depth from last frame;
        internal float depth;
        internal Vector3 initialConnectedAnchor;
        //to know the first lock of the joint and fire an appropriate event
        internal bool connectionWasLocked;
    }

    internal enum ImpaledObjectsCount
    {
        Single,
        Multiple
    }

    public enum ImpalerShape
    {
        Box,
        Capsule
    }

    [RequireComponent(typeof(Rigidbody))]
    public class Impaler : MonoBehaviour, IOnFixedUpdate, IManagedBehaviour, ActorSystem.IActorCallbacks
    {
        [SerializeField]
        protected Collider impalerCollider;
        [Tooltip("A point until which the object can impale another object")]
        [SerializeField]
        private Vector3 impaleStartLocal;
        [Tooltip("Object must move in this direction in order to impale another object")]
        [SerializeField]
        private Vector3 impaleDirection = -Vector3.up;
        [Tooltip("What distance from the start the tip of the impaler should be")]
        [SerializeField]
        private float impalerLength = 1f;
        [SerializeField]
        private ImpaledObjectsCount impaledObjectsCount;
        [SerializeField]
        private float jointBreakForce = 1000;
        [SerializeField]
        private float minVelocityToStartImpale;
        [Tooltip("Set false if impaled object should rotate around Impaler's ImpaleDirection")]
        [SerializeField]
        private bool lockRotationAroundImpaleAxis = true;
        [SerializeField]
        private LayerMask affectedLayers = ~0;
        [Range(0, 1)]
        [SerializeField]
        private float validHitDot = 0.5f;
        [Tooltip("How much collider overlap does impaler tolerate. Impaler will not lock joints if objects it is impaling are overlapping too much")]
        [SerializeField]
        private float collidersOverlapTolerance = 0.05f;

        //align with normal
        [SerializeField]
        private bool alignWithHitNormal;
        private (Quaternion rotation, Vector3 position) alignGoal;
        [SerializeField]
        private float alignWithNormalAnimDuration = 0.05f;
        private float AlignWithNormalAnimDuration => alignWithNormalAnimDuration * alignWithNormalMulti;

        [Tooltip("0 -> does not align with normal, 1 -> aligns fully, 0.5 -> aligns halfway")]
        [Range(0, 1)]
        [SerializeField]
        private float alignWithNormalMulti = 1f;

        private float alignWithNormalAnimTime;
        private bool alignWithNormalAnimActive;
        private bool hasAlignedWithNormal;
        //--------------------

        [SerializeField]
        private bool preventImpalingOtherImpalers = true;
        [SerializeField]
        private ImpalerShape impalerShape = ImpalerShape.Capsule;
        [SerializeField]
        private float impalerRadius = 0.5f;
        [SerializeField]
        private Vector2 impalerDimensions = new Vector2(0.5f, 0.5f);
        //not sure if its the best name
        [Tooltip("Impaler has to be moving along its impale direction in order to impale. Relative velcity deadzone is at the sides, so you can hit an impaler at its head, when impaler is stationary, and still impale, even if velocity direction is opposite to the imaple direction")]
        [Range(0, 1)]
        [SerializeField]
        private float velocityDeadzone = 0.9f;

        //multi stage impaling
        [SerializeField]
        private bool multiHitImpaling = false;
        [SerializeField]
        private float minVelocityToCountAsHit = 5;
        [SerializeField]
        private int maxHitCount = 5;
        [SerializeField]
        private float hitCooldown = 0.2f;
        private float hitCooldownLeft = 0;
        private float DepthPerHit => ImpalerLength / (float)maxHitCount;
        //--------------------

        public Vector3 ImpalerStart { get; protected set; }
        public Vector3 ImpalerTip { get; protected set; }
        //In world space
        public Vector3 ImpaleDirection { get; protected set; }
        //Raycast origin, should be above the start point
        public Vector3 CastStart { get; protected set; }
        public Vector3 CastEnd { get; protected set; }
        public float CastOffset { get; protected set; }
        public float ImpalerLength => impalerLength;
        public float CastLength { get; protected set; }
        public Vector3 BoxCastSize { get; protected set; }
        public Quaternion BoxCastRotation { get; protected set; } = Quaternion.identity;

        protected ReBody reBody;
        public ReBody ReBody => reBody;

        ActorSystem.IActor actor;

        public bool Enabled => isActiveAndEnabled;

        protected List<Collider> connectionsToDelete = new List<Collider>();
        internal Dictionary<Collider, Connection> impaledObjects = new Dictionary<Collider, Connection>();
        public IReadOnlyDictionary<Collider, Connection> ImpaledObjects => impaledObjects;

        [SerializeField, HideInInspector]
        new private Rigidbody rigidbody;
        private Vector3 projectedVelocity;
        private Vector3 impalerTipVelocity;
        //to handle impalers which are impaled from the start
        private bool initialCycle = true;

        private readonly RaycastHit[] raycastHits = new RaycastHit[32];

        private List<Collider> impalerColliders = new List<Collider>();

        [NodeAPI("OnJointLocked")]
        public event Action onJointLocked;
        [NodeAPI("OnImpaleStart")]
        public event Action<ConfigurableJoint, Collider, Vector3> onJointCreated;
        [NodeAPI("OnJointDestroyed")]
        public event Action<ConfigurableJoint, Collider> onImpalerRemoved;
        [NodeAPI("OnUnimpaled")]
        public event Action onUnimpaled;

        //dont see a point in serializing this
        private const float maxVelocityToLockJoint = 5f;
        //make use of onUnimpaled?
        private bool waitForFullDisconnect;
        private bool blockerObjectFound;

        private void OnValidate()
        {
            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();

                if (rigidbody == null)
                {
                    rigidbody = GetComponentInParent<Rigidbody>();
                }

                Debug.Assert(rigidbody != null, "Impaler must have a rigidbody on itself or on parent");
            }
        }

        void OnDisable()
        {
            initialCycle = true;
        }

        public void OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        public void OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        public virtual void OnLevelLoaded()
        {
            reBody = new ReBody(rigidbody);
            actor = GetComponentInParent<ActorSystem.IActor>();
            GetComponentsInChildren(impalerColliders);
        }

        void ActorSystem.IActorCallbacks.OnAfterSpawn() { }

        void ActorSystem.IActorCallbacks.OnAfterDespawn()
        {
            ClearConnections();
        }

        private void UpdateImpalerParameters()
        {
            ImpaleDirection = ImpalerToWorldDirection(impaleDirection).normalized;
            ImpalerStart = ImpalerToWorldPoint(impaleStartLocal);
            ImpalerTip = ImpalerStart + ImpaleDirection * impalerLength;

            if (reBody != default)
            {
                impalerTipVelocity = reBody.GetPointVelocity(ImpalerTip);
                projectedVelocity = ImpalerUtils.GetProjectedVelocity(impalerTipVelocity, ImpaleDirection);
            }

            if (impalerShape == ImpalerShape.Box)
            {
                BoxCastRotation = (ReBody.BodyExists ? ReBody.rotation : impalerCollider.attachedRigidbody.rotation)
                   * (Quaternion.LookRotation(impaleDirection, impaleDirection.y == 0 ? Vector3.up : Vector3.right));
                BoxCastSize = new Vector3(impalerDimensions.x / 2, impalerDimensions.y / 2, impalerLength / 2);
                CastOffset = BoxCastSize.z * 1.1f;
                CastEnd = ImpalerStart + (ImpaleDirection * (ImpalerLength / 2)) + projectedVelocity;
            }
            else if (impalerShape == ImpalerShape.Capsule)
            {
                CastOffset = impalerRadius * 1.1f;
                CastEnd = (ImpalerTip + projectedVelocity) - (ImpaleDirection * impalerRadius);
            }

            CastStart = ImpalerStart - ImpaleDirection * CastOffset;
            CastLength = (CastEnd - CastStart).magnitude;
        }

        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (hitCooldownLeft > 0)
                hitCooldownLeft -= Time.fixedDeltaTime;

            connectionsToDelete.Clear();
            connectionsToDelete.AddKeysCollectionToList(impaledObjects);

            UpdateImpalerParameters();
            var hits = RaycastThroughImpaler();

            if (waitForFullDisconnect && hits > 0)
            {
                initialCycle = false;
                return;
            }
            else if (waitForFullDisconnect && hits == 0)
                waitForFullDisconnect = false;

            blockerObjectFound = false;

            for (int i = 0; i < hits; i++)
            {
                UpdateImpale(raycastHits[i]);

                if (impaledObjects.ContainsKey(raycastHits[i].collider))
                {
                    connectionsToDelete.Remove(raycastHits[i].collider);
                }
            }

            RemoveConnections();

            if (alignWithNormalAnimActive)
            {
                AlignToNormal();
            }

            initialCycle = false;
        }

        private void OnJointBreak(float breakForce)
        {
            foreach (var item in impaledObjects)
            {
                var joint = item.Value.joint;
                if (joint == null)
                    continue;

                if (joint.currentForce.magnitude >= joint.breakForce || joint.currentTorque.magnitude >= joint.breakTorque)
                {
                    connectionsToDelete.Add(item.Key);
                }
            }

            RemoveConnections();
        }

        //used only for multi stage impaling
        private void OnCollisionEnter(Collision collision)
        {
            if (!multiHitImpaling || impaledObjects.Count == 0 || hitCooldownLeft > 0)
                return;
            var impalerCenter = (ImpalerStart + ImpalerTip) / 2;
            var positionDot = Vector3.Dot(collision.GetContact(0).point - impalerCenter, -ImpaleDirection);

            var velocityDot = Vector3.Dot(collision.relativeVelocity, ImpaleDirection);
            var sign = (int)Mathf.Sign(velocityDot);

            //Hit impulse matches impaler direction and is strong enough.
            if (((velocityDot >= 0.9f && positionDot >= 0.95) || (velocityDot <= -0.9f && positionDot <= -0.95))
                && collision.relativeVelocity.magnitude >= minVelocityToCountAsHit)
            {
                hitCooldownLeft = hitCooldown;
                foreach (var item in impaledObjects)
                {
                    var joint = item.Value.joint;
                    if (joint == null)
                        continue;

                    var limit = (item.Value.depth * -1) + DepthPerHit * sign;
                    limit = Mathf.Min(limit, impalerLength);

                    joint.SetJointLinearLimit(limit);
                    joint.xMotion = ConfigurableJointMotion.Limited;
                    joint.connectedAnchor = item.Value.initialConnectedAnchor;
                    reBody.position = ImpalerUtils.GetPositionAtDepth(reBody.position, ImpaleDirection, limit, item.Value.depth);
                }
            }
        }

        private void UpdateImpale(RaycastHit hit)
        {
            Collider other = hit.collider;
            //Dont care for checking impaler colliders, saves some performance
            if (IsPartOfImpaler(other))
                return;

            /* "For colliders that overlap the sphere at the start of the sweep, RaycastHit.normal is set opposite to the direction of the sweep,
            * RaycastHit.distance is set to zero, and the zero vector gets returned in RaycastHit.point. */
            if (hit.point == Vector3.zero)
                return;

            //update existing impale
            if (impaledObjects.ContainsKey(other))
            {
                var depth = ImpalerUtils.CalculateDepth(hit.point, ImpalerTip, ImpaleDirection, CastStart);

                //create new joint
                if (ShouldAddJoint(other, depth))
                {
                    AddJointForBody(other, hit, depth);
                }

                UpdateExistingJoint(impaledObjects[other], depth);

                if (impaledObjects.ContainsKey(other))
                {
                    impaledObjects[other].depth = depth;
                }
            }

            if (!blockerObjectFound)
                blockerObjectFound = ShouldPreventThisAndFurtherImpales(other, hit);

            //impale new object
            if (!blockerObjectFound && CanImpaleNewObject(other, hit, initialCycle))
            {
                ImpaleNewObject(other, hit);
            }
        }

        private void ImpaleNewObject(Collider other, RaycastHit hit)
        {
            var connection = new Connection() { collider = other };
            impaledObjects.Add(other, connection);

            Physics.IgnoreCollision(other, impalerCollider);

            if (!hasAlignedWithNormal && !alignWithNormalAnimActive)
                UpdateOrientation(other, hit);
        }

        private void UpdateExistingJoint(Connection connection, float depth)
        {
            var joint = connection.joint;
            if (joint != null)
            {
                if (joint.xMotion != ConfigurableJointMotion.Limited && ShouldUnlockMovementAlongImpaleAxis())
                {
                    joint.xMotion = ConfigurableJointMotion.Limited;
                    joint.connectedAnchor = connection.initialConnectedAnchor;
                }
                else if (joint.xMotion != ConfigurableJointMotion.Locked)
                {
                    {
                        var relativeMoveDir = ReBody.RelativeVelocityToOtherBody(connection.reBody);
                        //moving inside
                        if (Vector3.Dot(relativeMoveDir.normalized, ImpaleDirection) > 0)
                        {
                            var projectedVelocity = ImpalerUtils.GetProjectedVelocity(relativeMoveDir, ImpaleDirection);
                            var projectedDepth = depth - projectedVelocity.magnitude;

                            if (Mathf.Abs(projectedDepth) > ImpalerLength)
                            {
                                //DO NOT set position immediate, it uses center of mass instead of transform positions
                                reBody.position = ImpalerUtils.GetPositionAtDepth(reBody.position, ImpaleDirection, ImpalerLength, depth);
                                reBody.velocity = Vector3.zero;
                                depth = -ImpalerLength;
                            }
                        }
                    }

                    //lock
                    if (!impaledObjects.CollidersOverlappingTooMuch(collidersOverlapTolerance) && SlowEnoughToLockJoint(connection.reBody))
                    {
                        LockJoint();
                    }
                }

                //if impaler is being pulled out, prevent pushing in
                if (joint.xMotion == ConfigurableJointMotion.Limited)
                {
                    joint.connectedAnchor = connection.initialConnectedAnchor;

                    var relativeMoveDir = ReBody.RelativeVelocityToOtherBody(connection.reBody).normalized;
                    if (Vector3.Dot(relativeMoveDir, ImpaleDirection) < 0)
                    {
                        connection.joint.SetJointLinearLimit(Mathf.Abs(depth));
                    }
                }
            }

            void LockJoint()
            {
                var connectedAnchorPos = reBody.TransformPoint(joint.anchor);
                if (connection.reBody != default)
                {
                    connectedAnchorPos = connection.reBody.InverseTransformPoint(connectedAnchorPos);
                }

                joint.connectedAnchor = connectedAnchorPos;
                joint.xMotion = ConfigurableJointMotion.Locked;

                if (!connection.connectionWasLocked)
                    onJointLocked?.Invoke();
                connection.connectionWasLocked = true;
            }
        }

        #region Align With Normal

        private void UpdateOrientation(Collider other, RaycastHit hit)
        {
            var orientationOverride = other.GetComponentOfType<IImpalerOrientationOverride>();
            if (orientationOverride != null)
            {
                UpdateOrientation(orientationOverride.Normal);
            }
            else if (alignWithHitNormal)
            {
                UpdateOrientation(hit.normal);
            }

            //if impaler is grabbed by human, then this override doesnt fully work, since human moves object after
            void UpdateOrientation(Vector3 normal)
            {
                var distance = ImpalerUtils.GetDistanceFromPivotToImpalerStart(reBody.position, ImpaleDirection, ImpalerStart);
                alignGoal = ImpalerUtils.GetAlignmentWithNormalPosAndRot(reBody.rotation, normal, impaleDirection, distance, hit.point);
                alignGoal.rotation = Quaternion.Slerp(reBody.rotation, alignGoal.rotation, alignWithNormalMulti);
                alignGoal.position = Vector3.Slerp(reBody.position, alignGoal.position, alignWithNormalMulti);
                alignWithNormalAnimTime = 0;
                alignWithNormalAnimActive = true;
                UpdateImpalerParameters();
            }
        }

        private void AlignToNormal()
        {
            alignWithNormalAnimTime += Time.fixedDeltaTime;
            var lerp = alignWithNormalAnimTime / AlignWithNormalAnimDuration;
            reBody.rotation = Quaternion.Slerp(reBody.rotation, alignGoal.rotation, lerp);
            reBody.position = Vector3.Slerp(reBody.position, alignGoal.position, lerp);
            reBody.velocity = Vector3.zero;

            if (alignWithNormalAnimTime >= AlignWithNormalAnimDuration)
            {
                hasAlignedWithNormal = true;
                alignWithNormalAnimActive = false;
                reBody.rotation = alignGoal.rotation;
                reBody.position = alignGoal.position;
            }
        }

        #endregion

        private bool SlowEnoughToLockJoint(ReBody connection)
        {
            var relativeMoveDir = ReBody.RelativeVelocityToOtherBody(connection);

            //need to check for movement magnitude because "stationary" objects can have some movement which would invalidate dot results
            if (relativeMoveDir.sqrMagnitude > 0.1f && Vector3.Dot(relativeMoveDir.normalized, ImpaleDirection) < 0)
                return false;

            return impalerTipVelocity.sqrMagnitude <= maxVelocityToLockJoint * maxVelocityToLockJoint;
        }

        private void AddJointForBody(Collider other, RaycastHit hit, float depth)
        {
            var otherRig = other.attachedRigidbody;

            if (otherRig != null && rigidbody == otherRig) // avoid connecting impaler to itself
            {
                return;
            }

            var joint = rigidbody.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;

            ReBody connectedReBody = default;

            if (otherRig != null)
            {
                connectedReBody = new ReBody(otherRig);
                joint.connectedBody = otherRig;
            }
            var impalePosition = hit.point;

            Vector3 connectedAnchorPos = ImpalerUtils.GetInitialConnectedAnchorPos(impalePosition, ImpaleDirection, ImpalerLength, depth);

            if (otherRig != null)
            {
                connectedAnchorPos = connectedReBody.InverseTransformPoint(connectedAnchorPos);
            }

            joint.connectedAnchor = connectedAnchorPos;
            joint.axis = impaleDirection;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            if (lockRotationAroundImpaleAxis)
                joint.angularXMotion = ConfigurableJointMotion.Locked;
            else
                joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.enableCollision = true;

            if (multiHitImpaling)
                joint.SetJointLinearLimit(DepthPerHit);
            else
                joint.SetJointLinearLimit(ImpalerLength);

            joint.anchor = PointToImpalerLocal(impalePosition);
            joint.breakForce = jointBreakForce;
            joint.breakTorque = jointBreakForce;

            var connection = impaledObjects[other];
            connection.reBody = connectedReBody;
            connection.joint = joint;
            connection.initialConnectedAnchor = connectedAnchorPos;
            onJointCreated?.Invoke(joint, other, impalePosition);

            if (actor != null && ActorSystem.JointModule != null)
                ActorSystem.JointModule.RegisterDynamicJoint(joint, actor);
        }

        private void RemoveConnections()
        {
            bool hadConnections = impaledObjects.Count > 0;
            foreach (var item in connectionsToDelete)
            {
                RemoveConnection(item);
            }
            connectionsToDelete.Clear();

            if (hadConnections && impaledObjects.Count == 0)
            {
                hasAlignedWithNormal = false;
                alignWithNormalAnimActive = false;
                onUnimpaled?.Invoke();
            }
        }

        protected void RemoveConnection(Collider other)
        {
            var joint = impaledObjects[other].joint;

            onImpalerRemoved?.Invoke(joint, other);
            if (joint != null)
            {
                //need to unlock the joint, since its not going to be destroyed instantly
                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;
                Destroy(joint);
            }

            impaledObjects.Remove(other);
            Physics.IgnoreCollision(other, impalerCollider, false);
        }

        private int RaycastThroughImpaler()
        {
            int hits = 0;
            if (impalerShape == ImpalerShape.Capsule)
                hits = Physics.SphereCastNonAlloc(CastStart, impalerRadius, ImpaleDirection, raycastHits, CastLength, affectedLayers, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            else
                hits = Physics.BoxCastNonAlloc(CastStart, BoxCastSize, ImpaleDirection, raycastHits, BoxCastRotation, CastLength, affectedLayers, queryTriggerInteraction: QueryTriggerInteraction.Ignore);

            ArrayUtilities.InsertionSort(raycastHits, hits, ImpalerUtils.Comparer);

            return hits;
        }

        [ContextMenu("Break Connections")]
        public void Unimpale()
        {
            if (impaledObjects.Count == 0)
                return;

            float maxDepth = float.MaxValue;
            foreach (var pair in impaledObjects)
            {
                if (pair.Value.depth < maxDepth)
                    maxDepth = pair.Value.depth;
            }

            waitForFullDisconnect = true;

            reBody.position += (-ImpaleDirection * (Mathf.Abs(maxDepth)));
            ClearConnections();
        }

        public void ClearConnections()
        {
            connectionsToDelete.AddKeysCollectionToList(impaledObjects);
            RemoveConnections();
        }

        private bool IsPartOfImpaler(Collider other)
        {
            return impalerColliders.Contains(other);
        }

        #region Overridable methods

        protected virtual bool ShouldAddJoint(Collider other, float depth)
        {
            if (impaledObjects[other].joint == null)
            {
                if (alignWithNormalAnimActive)
                    return false;

                //allowing jointing to only a single objects if impale mode is single
                if (impaledObjectsCount == ImpaledObjectsCount.Single)
                {
                    foreach (var value in impaledObjects.Values)
                    {
                        if (value.joint != null)
                            return false;
                    }
                }

                //prevent double jointing to the same rigidbody object if multiple colliders of it are hit
                var rig = other.attachedRigidbody;
                if (rig != null)
                {
                    foreach (var collider in impaledObjects.Keys)
                    {
                        if (other != collider && collider.attachedRigidbody == rig && impaledObjects[collider].joint != null)
                        {
                            return false;
                        }
                    }
                }
                //only one joint is needed for all non rigidbody objects
                else
                {
                    foreach (var collider in impaledObjects.Keys)
                    {
                        if (other != collider && collider.attachedRigidbody == null && impaledObjects[collider].joint != null)
                        {
                            return false;
                        }
                    }
                }

                //is tip inside object
                if (depth <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool ShouldUnlockMovementAlongImpaleAxis()
        {
            return false;
        }

        /// <summary>
        /// Blocks objects from impaling any further, used to to prevent impaling objects with are behind a blocker object.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        protected virtual bool ShouldPreventThisAndFurtherImpales(Collider other, RaycastHit hit)
        {
            //IF cast was started inside another object, its hit point will be Vector.zero and its normal will be wrong.
            //Debug.Log($"dot {Vector3.Dot(-hit.normal, ImpaleDirection)} > {validHitDot} dir {ImpaleDirection} normal {-hit.normal} point {hit.point} ");
            if (Vector3.Dot(-hit.normal, ImpaleDirection) < validHitDot)
                return true;

            if (preventImpalingOtherImpalers)
            {
                if (other.GetComponentOfType<Impaler>() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Should ignore collisions with a hit collider
        /// </summary>
        /// <param name="other"></param>
        /// <param name="hit"></param>
        /// <param name="stationaryImpale"></param>
        /// <returns></returns>
        protected virtual bool CanImpaleNewObject(Collider other, RaycastHit hit, bool stationaryImpale = false)
        {
            if (impaledObjects.ContainsKey(other))
                return false;

            if (!stationaryImpale && !alignWithNormalAnimActive && !AreVelocitiesViable(hit))
                return false;

            return true;
        }

        #endregion

        private bool AreVelocitiesViable(RaycastHit hit)
        {
            var relativeMoveDir = reBody.RelativeVelocityToOtherBody(new ReBody(hit.collider.attachedRigidbody)).normalized;
            var dot = Vector3.Dot(relativeMoveDir, ImpaleDirection);
            //relative velcity deadzone is at the sides, so you can hit an impaler at its head, when impaler is stationary and still impale, even if velocity direction is opposite to the imaple direction
            if (Math.Abs(dot) < velocityDeadzone)
                return false;

            if (impalerTipVelocity.sqrMagnitude < minVelocityToStartImpale * minVelocityToStartImpale)
                return false;

            return true;
        }

        #region Conversions

        private Vector3 ImpalerToWorldPoint(Vector3 toConvert)
        {
            if (reBody == default)
                return impalerCollider.attachedRigidbody.TransformPoint(toConvert);
            else
                return reBody.TransformPoint(toConvert);
        }

        private Vector3 ImpalerToWorldDirection(Vector3 toConvert)
        {
            if (reBody == default)
                return impalerCollider.attachedRigidbody.TransformDirection(toConvert);
            else
                return reBody.TransformDirection(toConvert);
        }

        private Vector3 PointToImpalerLocal(Vector3 toConvert)
        {
            if (reBody == default)
                return impalerCollider.attachedRigidbody.InverseTransformPoint(toConvert);
            else
                return reBody.InverseTransformPoint(toConvert);
        }

        #endregion

        protected virtual void OnDrawGizmos()
        {
            if (impalerCollider == null)
                return;

            if (!Application.isPlaying)
            {
                UpdateImpalerParameters();
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CastStart, impalerShape == ImpalerShape.Capsule ? impalerRadius : 0.04f);
            Gizmos.DrawRay(CastStart, ImpaleDirection * CastLength);
            if (Application.isPlaying)
            {
                Gizmos.DrawWireSphere(CastEnd, 0.04f);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ImpalerStart, 0.04f);
            if (impalerShape == ImpalerShape.Box)
            {
                var pos = (ImpalerStart + ImpalerTip) / 2;
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(pos, BoxCastRotation, BoxCastSize * 2);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = prevMatrix;
            }
            else if (impalerShape == ImpalerShape.Capsule)
                DebugExtension.DrawCylinder(ImpalerStart, ImpalerTip, impalerRadius);

            DebugExtension.DrawArrow(ImpalerStart, ImpaleDirection * ImpalerLength);

            //Display connections info
            /*foreach (var item in impaledObjects)
            {
                if (item.Value.joint != null)
                {
                    Gizmos.color = Color.cyan;

                    if (item.Value.reBody != default)
                        Gizmos.DrawSphere(item.Value.reBody.TransformPoint(item.Value.joint.connectedAnchor), 0.05f);
                    else
                        Gizmos.DrawSphere(item.Value.joint.connectedAnchor, 0.05f);

                    Gizmos.color = Color.black;
                    Gizmos.DrawWireSphere(ImpalerToWorldPoint(item.Value.joint.anchor), 0.06f);


                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(alignGoal.position, 0.07f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ReBody.position, 0.07f);
            */
        }

    }
}
