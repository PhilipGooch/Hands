using NBG.Actor;
using NBG.Core;
using NBG.LogicGraph;
using NBG.MeshGeneration;
using Recoil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Sheep.Cutter
{
    internal struct NewPieceData
    {
        public MeshCollider collider;
        public ReBody reBody;
    }

    public class Cutter : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private Collider blade;
        [SerializeField]
        private LayerMask affectedLayers = ~0;
        [SerializeField]
        private float cutWidth;
        [SerializeField]
        private Vector3 cutNormal;
        [SerializeField]
        private float dragOfCutObject = 10;
        [SerializeField]
        private float angularDragOfCutObject = 10;

        [Header("Advanced")]
        [SerializeField]
        float minAllowedSingleEdgeLengthToDestroy = 0.2f;
        [SerializeField]
        float maxVolumeToDestroy = 0.027f;

        [NodeAPI("OnCut")]
        public event Action<Vector3> onCut;
        [NodeAPI("OnStartCutting")]
        public event Action onStartCutting;
        [NodeAPI("OnStopCutting")]
        public event Action onStopCutting;
        [NodeAPI("OnTurnedOn")]
        public event Action onTurnedOn;
        [NodeAPI("OnTurnedOff")]
        public event Action onTurnedOff;
        [NodeAPI("CanCut")]
        public bool CanCut { get; set; } = true;

        protected Vector3 CutNormal { get; private set; }
        protected Vector3 BladeCenter { get; private set; }

        protected Dictionary<Rigidbody, CutData> activeCuts = new Dictionary<Rigidbody, CutData>();

        private List<Rigidbody> toRemove = new List<Rigidbody>();
        private const float lowestMassAmountAfterSlice = 10f;

        List<ActorSystem.IActor> actorsToAdd = new List<ActorSystem.IActor>();

        bool wasCutting = false;
        bool couldCut = true;

        private void FixedUpdate()
        {
            UpdateParameters();

            toRemove.Clear();

            if (CanCut)
            {
                foreach (var activeCut in activeCuts)
                {
                    UpdateCut(activeCut.Key, activeCut.Value);
                }
            }

            foreach (var remove in toRemove)
            {
                RemoveActiveCut(remove);
            }

            UpdateEvents();

            wasCutting = activeCuts.Count > 0 && CanCut;
            couldCut = CanCut;
        }

        void UpdateEvents()
        {
            //just turned off
            if (!CanCut && couldCut)
            {
                LockActiveCuts();
                onTurnedOff?.Invoke();
            }
            //just turned on
            else if (CanCut && !couldCut)
            {
                UnlockActiveCuts();
                onTurnedOn?.Invoke();
            }

            if (CanCut)
            {
                //turned on and started cutting
                if (activeCuts.Count > 0 && !wasCutting)
                {
                    onStartCutting?.Invoke();
                }
                //turned on and stopped cutting
                else if (activeCuts.Count == 0 && wasCutting)
                {
                    onStopCutting?.Invoke();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryStartCutting(other);
        }

        private void OnTriggerExit(Collider other)
        {
            TryStopCutting(other);
        }

        void LockActiveCuts()
        {
            foreach (var activeCut in activeCuts)
            {
                var joint = activeCut.Value.joint;

                var connectedAnchorPos = transform.TransformPoint(joint.anchor);
                if (joint.connectedBody != null)
                {
                    connectedAnchorPos = joint.connectedBody.InverseTransformPoint(connectedAnchorPos);
                }

                joint.connectedAnchor = connectedAnchorPos;
                joint.axis = joint.transform.InverseTransformDirection(CutNormal);

                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
            }
        }

        void UnlockActiveCuts()
        {
            foreach (var activeCut in activeCuts)
            {
                var joint = activeCut.Value.joint;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;
                joint.angularXMotion = ConfigurableJointMotion.Free;
            }
        }

        private void UpdateParameters()
        {
            CutNormal = blade.transform.TransformDirection(cutNormal);
            var bladeBounds = new BoxBounds(blade);
            BladeCenter = bladeBounds.center;
        }

        private void UpdateCut(Rigidbody rigidbody, CutData cutData)
        {
            CutterUtils.CheckIfSliceInside(rigidbody, cutData.meshFilter, blade, CutNormal, ref cutData.controlPointsCutState);

            if (cutData.HasBeenCut())
                Cut(cutData, cutWidth);
        }

        public void Cut(CutData cutData, float cutWidth)
        {
            Mesh mesh = cutData.meshFilter.mesh;
            var closesPoint = blade.ClosestPointSafe(cutData.collider.ClosestPointSafe(BladeCenter));

            MeshCutting.Cut(
                mesh,
                new CutPlane(
                    cutData.reBody.InverseTransformPoint(closesPoint),
                    cutData.reBody.InverseTransformDirection(CutNormal),
                    cutWidth),
                out Mesh piece1,
                out Mesh piece2);

            if (piece1.triangles.Length != 0 && piece2.triangles.Length != 0)
            {
                var mat = cutData.meshFilter.GetComponent<MeshRenderer>().material;

                var newPieceData1 = CreatePiece(piece1, mat, cutData);
                var newPieceData2 = CreatePiece(piece2, mat, cutData);

                if (ActorSystem.InstantiationModule != null)
                {
                    actorsToAdd.Clear();
                    if (newPieceData1.reBody.BodyExists)
                        actorsToAdd.Add(newPieceData1.collider.GetComponent<ActorSystem.IActor>());
                    if (newPieceData2.reBody.BodyExists)
                        actorsToAdd.Add(newPieceData2.collider.GetComponent<ActorSystem.IActor>());

                    ActorSystem.InstantiationModule.InitializeActorSet(actorsToAdd);
                }

                SplitMass(cutData.reBody, newPieceData1, newPieceData2);
                Regrab(cutData.reBody, newPieceData1, newPieceData2);

                cutData.Rigidbody.gameObject.SetActive(false);
            }

            toRemove.Add(cutData.Rigidbody);
            onCut?.Invoke(closesPoint);
        }

        private NewPieceData CreatePiece(Mesh pieceMesh, Material mat, CutData cutData)
        {
            var data = new NewPieceData();
            if (pieceMesh != null)
            {
                Transform original = cutData.reBody.rigidbody.transform;

                GameObject newPiece = new GameObject();
                newPiece.transform.position = original.position;
                newPiece.transform.rotation = original.rotation;
                newPiece.transform.localScale = original.localScale;

                var meshfilter = newPiece.AddComponent<MeshFilter>();
                meshfilter.mesh = pieceMesh;

                var meshRenderer = newPiece.AddComponent<MeshRenderer>();
                meshRenderer.material = mat;

                var meshCollider = newPiece.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = pieceMesh;

                if (DestroyIfSmall(newPiece))
                    return data;

                var rig = newPiece.AddComponent<Rigidbody>();
                rig.ResetCenterOfMass();
                rig.ResetInertiaTensor();
                rig.collisionDetectionMode = cutData.Rigidbody.collisionDetectionMode;
                rig.interpolation = cutData.Rigidbody.interpolation;

                CutterUtils.CopyComponents(original.gameObject, newPiece);

                ReBody rigidbody = new ReBody(rig);
                ManagedWorld.main.ResyncPhysXBody(rig);

                data.collider = meshCollider;
                data.reBody = rigidbody;
            }

            return data;
        }

        private void TryStartCutting(Collider other)
        {
            if (CanCut)
            {
                Rigidbody attached = other.attachedRigidbody;
                if (attached != null)
                {
                    MeshFilter meshFilter = attached.GetComponentInChildren<MeshFilter>();

                    if (meshFilter != null && IsCutableObject(other, attached))
                    {
                        var otherRebody = new ReBody(attached);
                        var cutInfo = new CutData()
                        {
                            meshFilter = meshFilter,
                            reBody = otherRebody,
                            collider = other,
                        };

                        var joint = attached.gameObject.AddComponent<ConfigurableJoint>();
                        joint.axis = other.transform.InverseTransformDirection(CutNormal);
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.angularYMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                        joint.SetJointDamping(dragOfCutObject, angularDragOfCutObject);
                        cutInfo.joint = joint;

                        activeCuts.Add(attached, cutInfo);
                        Physics.IgnoreCollision(other, blade, true);
                    }
                }
            }
        }

        private void TryStopCutting(Collider other)
        {
            RemoveActiveCut(other.attachedRigidbody);
        }

        private void RemoveActiveCut(Rigidbody rigidbody)
        {
            if (rigidbody != null && activeCuts.ContainsKey(rigidbody))
            {
                var cutData = activeCuts[rigidbody];
                var collider = cutData.collider;
                Destroy(cutData.joint);
                Physics.IgnoreCollision(collider, blade, false);
                activeCuts.Remove(rigidbody);
            }
        }

        private bool IsCutableObject(Collider collider, Rigidbody attached)
        {
            if (activeCuts.ContainsKey(attached))
                return false;

            if (!LayerUtils.IsPartOfLayer(collider.gameObject.layer, affectedLayers))
                return false;

            //check if object did not come from the side of the blade
            var closesPoint = blade.ClosestPoint(collider.ClosestPointSafe(BladeCenter));
            var dirToPoint = (closesPoint - BladeCenter).normalized;
            if (Mathf.Abs(Vector3.Dot(dirToPoint, CutNormal)) > 0.1f)
                return false;

            return true;
        }

        #region Regrab

        private void Regrab(ReBody originalRigidbody, NewPieceData slice1, NewPieceData slice2)
        {
            Hand hand = Player.Instance.GetHandThatIsGrabbingBody(originalRigidbody.rigidbody);
            if (hand != null)
            {
                //both destroyed
                if (!slice1.reBody.BodyExists && !slice2.reBody.BodyExists)
                    return;

                Vector3 dirToSlice1 = Vector3.zero;
                Vector3 dirToSlice2 = Vector3.zero;

                if (slice1.reBody.BodyExists)
                {
                    Vector3 slice1Pos = new BoxBounds(slice1.collider).center;
                    dirToSlice1 = slice1Pos - BladeCenter;
                }

                if (slice2.reBody.BodyExists)
                {
                    Vector3 slice2Pos = new BoxBounds(slice2.collider).center;
                    dirToSlice2 = slice2Pos - BladeCenter;
                }

                if (slice1.reBody.BodyExists && slice2.reBody.BodyExists)
                {
                    if (hand.HoldingObjectWithTwoHands)
                    {
                        RegrabTwoSlices(slice1, slice2, dirToSlice1, dirToSlice2, hand);
                        RegrabTwoSlices(slice1, slice2, dirToSlice1, dirToSlice2, hand.otherHand);
                    }
                    else
                    {
                        RegrabTwoSlices(slice1, slice2, dirToSlice1, dirToSlice2, hand);
                    }
                }
                else if (slice1.reBody.BodyExists && !slice2.reBody.BodyExists)
                {
                    RegrabOneSlice(slice1, dirToSlice1, hand);
                }
                else if (!slice1.reBody.BodyExists && slice2.reBody.BodyExists)
                {
                    RegrabOneSlice(slice2, dirToSlice2, hand);
                }
            }

        }

        void RegrabTwoSlices(NewPieceData slice1, NewPieceData slice2, Vector3 dirToSlice1, Vector3 dirToSlice2, Hand hand)
        {
            Vector3 grabPosition = hand.worldAttachedAnchorPos;
            Vector3 dirToHand = grabPosition - BladeCenter;

            var dotSlice1 = Vector3.Dot(dirToHand, dirToSlice1);
            var dotSlice2 = Vector3.Dot(dirToHand, dirToSlice2);

            if (dotSlice1 > dotSlice2)
            {
                hand.InterceptGrab(slice1.reBody.rigidbody, grabPosition);
            }
            else
            {
                hand.InterceptGrab(slice2.reBody.rigidbody, grabPosition);
            }
        }

        void RegrabOneSlice(NewPieceData slice, Vector3 dirToSlice, Hand hand)
        {
            if (hand.HoldingObjectWithTwoHands)
            {
                RegrabOneSlice(hand);
                RegrabOneSlice(hand.otherHand);
            }
            else
            {
                RegrabOneSlice(hand);
            }

            void RegrabOneSlice(Hand hand)
            {
                Vector3 grabPos = hand.worldAttachedAnchorPos;
                Vector3 dirToHand = grabPos - BladeCenter;

                if (Vector3.Dot(dirToHand, dirToSlice) > 0)
                    hand.InterceptGrab(slice.reBody.rigidbody, grabPos);
            }
        }

        #endregion

        private bool DestroyIfSmall(GameObject obj)
        {
            var bounds = new BoxBounds(obj.GetComponent<Collider>());

            //if one of the edges is too small -> destroy
            if (bounds.size.x < minAllowedSingleEdgeLengthToDestroy
                || bounds.size.y < minAllowedSingleEdgeLengthToDestroy
                || bounds.size.z < minAllowedSingleEdgeLengthToDestroy)
            {
                Destroy(obj);
                return true;
            }

            var volume = bounds.size.x * bounds.size.y * bounds.size.z;
            //If volume is too small -> destroy
            if (volume <= maxVolumeToDestroy)
            {
                Destroy(obj);
                return true;
            }

            return false;
        }

        //maybe make this a generic method?
        private void SplitMass(ReBody originalRigidbody, NewPieceData slice1, NewPieceData slice2)
        {
            //both destroyed
            if (!slice1.reBody.BodyExists && !slice2.reBody.BodyExists)
                return;

            if (slice1.reBody.BodyExists && !slice2.reBody.BodyExists)
            {
                slice1.reBody.mass = Mathf.Max(originalRigidbody.mass * 0.95f, lowestMassAmountAfterSlice);
                slice1.reBody.rigidbody.ResetInertiaTensor();
                ManagedWorld.main.ResyncPhysXBody(slice1.reBody.rigidbody);
                return;
            }

            if (!slice1.reBody.BodyExists && slice2.reBody.BodyExists)
            {
                slice2.reBody.mass = Mathf.Max(originalRigidbody.mass * 0.95f, lowestMassAmountAfterSlice);
                slice2.reBody.rigidbody.ResetInertiaTensor();
                ManagedWorld.main.ResyncPhysXBody(slice2.reBody.rigidbody);
                return;
            }

            var slice1Bounds = new BoxBounds(slice1.collider);
            var slice2Bounds = new BoxBounds(slice2.collider);

            float slice1Volume = slice1Bounds.size.x * slice1Bounds.size.y * slice1Bounds.size.z;
            float slice2Volume = slice2Bounds.size.x * slice2Bounds.size.y * slice2Bounds.size.z;

            float slice1MassMulti = slice1Volume / (slice1Volume + slice2Volume);
            slice1.reBody.mass = Mathf.Max(originalRigidbody.mass * slice1MassMulti, lowestMassAmountAfterSlice);
            slice2.reBody.mass = Mathf.Max(originalRigidbody.mass * (1 - slice1MassMulti), lowestMassAmountAfterSlice);

            slice1.reBody.rigidbody.ResetInertiaTensor();
            slice2.reBody.rigidbody.ResetInertiaTensor();
            ManagedWorld.main.ResyncPhysXBody(slice1.reBody.rigidbody);
            ManagedWorld.main.ResyncPhysXBody(slice2.reBody.rigidbody);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(blade.transform.position, blade.transform.TransformDirection(cutNormal));

            if (activeCuts != null)
            {
                foreach (KeyValuePair<Rigidbody, CutData> pair in activeCuts)
                {
                    if (pair.Key == null)
                        continue;
                    /*      2--------3
                    *      /|       /|
                    *     / |      / |
                    *    /  0-----/--1
                    *   /  /     /  /       
                    *  6--------7  /
                    *  | /      | /
                    *  |/       |/
                    *  4--------5
                    */
                    Vector3[] corners = CutterUtils.GetCornerPoints(pair.Key, pair.Value.meshFilter);
                    CutterUtils.CheckIfSliceInside(pair.Key, pair.Value.meshFilter, blade, CutNormal, ref pair.Value.controlPointsCutState);
                    var cutState = pair.Value.controlPointsCutState;
                    //draw which edge was cut 
                    Debug.DrawLine(corners[0], corners[4], cutState[0] ? Color.green : Color.red);
                    Debug.DrawLine(corners[1], corners[5], cutState[1] ? Color.green : Color.red);
                    Debug.DrawLine(corners[2], corners[6], cutState[2] ? Color.green : Color.red);
                    Debug.DrawLine(corners[3], corners[7], cutState[3] ? Color.green : Color.red);

                    Debug.DrawLine(corners[2], corners[0], cutState[4] ? Color.green : Color.red);
                    Debug.DrawLine(corners[3], corners[1], cutState[5] ? Color.green : Color.red);
                    Debug.DrawLine(corners[6], corners[4], cutState[6] ? Color.green : Color.red);
                    Debug.DrawLine(corners[7], corners[5], cutState[7] ? Color.green : Color.red);

                    Debug.DrawLine(corners[1], corners[0], cutState[8] ? Color.green : Color.red);
                    Debug.DrawLine(corners[3], corners[2], cutState[9] ? Color.green : Color.red);
                    Debug.DrawLine(corners[5], corners[4], cutState[10] ? Color.green : Color.red);
                    Debug.DrawLine(corners[7], corners[6], cutState[11] ? Color.green : Color.red);

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(corners[0], 0.05f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(corners[1], 0.05f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(corners[2], 0.05f);
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(corners[3], 0.05f);

                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(corners[4], 0.05f);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(corners[5], 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(corners[6], 0.05f);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(corners[7], 0.05f);
                }
            }
        }
    }
}
