using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Conveyors
{
    /// <summary>
    /// Structure that retrieves every body and radius for every roller axis.
    /// </summary>
    [Serializable]
    public class AxisInput
    {
        public Rigidbody body;
        public float radius = 0.5f;
    }

    /// <summary>
    /// Has the required data to compute each roller axis.
    /// </summary>
    [Serializable]
    internal class AxisData
    {
        public Vector3 localPosition;
        public float trackPosition;
        public float radius;
        public bool concave;
        public float axisRotation;
    }

    /// <summary>
    /// Conveyor class that creates the meshes. You need to press generate to apply changes.
    /// </summary>
    public class Conveyor : MonoBehaviour
    {
        /// <summary>
        /// Stores the data needed to create and process the extra attachments you can add to the conveyor belt.
        /// </summary>
        [Serializable]
        public class PieceAttachment
        {
            public GameObject prefab;
            public float position;
            [HideInInspector] [SerializeField] internal Rigidbody rb;
        }

        /// <summary>
        /// The belts are divided into parts and this holds all the necessary data. This is built when you generate.
        /// </summary>
        [Serializable]
        internal class PartData
        {
            [SerializeField] internal Transform transform;
            [SerializeField] internal float progress;
            [SerializeField] internal float renderProgress;
            [SerializeField] internal Vector3 dir;
            [SerializeField] internal Vector3 origin;
            [SerializeField] internal float remainingDistance;
            [SerializeField] internal bool isLong;
            [SerializeField] internal bool isLinear;
            [SerializeField] internal float radius;
            [SerializeField] internal MeshFilter meshFilter;
            [SerializeField] internal Mesh shortMesh;
            [SerializeField] internal Mesh longMesh;
            [SerializeField] internal Rigidbody rb;
            [SerializeField] internal Transform renderTransform;
            [SerializeField] internal Collider lastRadialCollider;
        }

        /// <summary>
        /// Stores the information for custom grabbing.
        /// </summary>
        private class GrabbingPiece
        {
            public float pieceBeltPos;
            public Rigidbody body;
        }

        [Header("Piece")]
        public float pieceLength;
        public Mesh pieceMesh;
        public Material pieceMaterial;
        public Vector2 colliderSize;

        [Header("Speed")]
        public float speed;

        [Header("Roller Axis")]
        public AxisInput[] axis;
        [HideInInspector] [SerializeField] private AxisData[] axisData;

        [Header("Custom pieces")]
        public PieceAttachment[] pieceAttachments;

        [HideInInspector] [SerializeField] private float perPieceLength;
        [HideInInspector] [SerializeField] private int pieceCount;
        [HideInInspector] [SerializeField] private ConveyorTrack track;
        [HideInInspector] [SerializeField] private PartData[] partData;

        private float[] piecePositions;
        private List<GrabbingPiece> grabbingPieces = new List<GrabbingPiece>();
        public void Start()
        {
            if (TryGetComponent(out IConveyorCustomObject customObject))
            {
                DestroyImmediate(customObject as MonoBehaviour);
            }
        }

        public void Generate()
        {
            ClearEverything();
            CalculateTrack();
            CreatePieces();
        }
        private void CalculateTrack()
        {
            FillAxisData();

            List<LinearTrackPart> linearParts = new List<LinearTrackPart>();
            List<RadialTrackPart> radialParts = new List<RadialTrackPart>();
            List<ITrackPart> parts = new List<ITrackPart>();

            if (axis.Length >= 2)
            {
                AxisData current, next;
                current = axisData[0];

                for (int i = 1; i < axis.Length; i++)
                {
                    next = axisData[i];

                    var newPart = new LinearTrackPart(current, next);
                    linearParts.Add(newPart);

                    current = next;
                }

                next = axisData[0];

                linearParts.Add(new LinearTrackPart(current, next));

                radialParts.Add(new RadialTrackPart(axisData[0].localPosition, axisData[0].radius, linearParts[linearParts.Count - 1].Direction, linearParts[0].Direction, axisData[0].concave));
                for (int i = 0; i < linearParts.Count - 1; i++)
                {
                    var radialPart = new RadialTrackPart(axisData[i + 1].localPosition, axisData[i + 1].radius, linearParts[i].Direction, linearParts[i + 1].Direction, axisData[i + 1].concave);
                    radialParts.Add(radialPart);
                }

                float partStart = 0;
                for (int i = 0; i < linearParts.Count; i++)
                {
                    parts.Add(radialParts[i]);
                    radialParts[i].SetStart(partStart);
                    partStart += radialParts[i].Length;

                    parts.Add(linearParts[i]);
                    linearParts[i].SetStart(partStart);
                    partStart += linearParts[i].Length;
                }
            }

            track = new ConveyorTrack(parts.ToArray(), pieceLength);
        }

        private void FillAxisData()
        {
            axisData = new AxisData[axis.Length];
            for (int i = 0; i < axis.Length; i++)
            {
                var data = new AxisData();
                data.radius = axis[i].radius;
                data.localPosition = axis[i].body.transform.localPosition;
                axisData[i] = data;
            }

            for (int i = 0; i < axis.Length; i++)
            {
                Vector3 prev = axisData[i == 0 ? axis.Length - 1 : i - 1].localPosition;
                Vector3 current = axisData[i].localPosition;
                Vector3 next = axisData[i == axis.Length - 1 ? 0 : i + 1].localPosition;

                Vector3 AB = current - prev;
                Vector3 BC = next - current;
                Vector3 normal = Vector3.Cross(AB, Vector3.right);
                axisData[i].concave = Vector3.Dot(normal, BC) > 0.0f;
            }
        }
        private void CreatePieces()
        {
            pieceCount = (int)(track.fullTrackLength / pieceLength);
            piecePositions = new float[pieceCount];
            perPieceLength = track.fullTrackLength / pieceCount;

            Type conveyorCustomObject = null;
            if (TryGetComponent(out IConveyorCustomObject customObject))
            {
                conveyorCustomObject = customObject.GetType();
            }

            for (int i = 0; i < pieceCount; i++)
            {
                piecePositions[i] = i * perPieceLength;
            }

            Dictionary<ITrackPart, float[]> positionsPerPart = new Dictionary<ITrackPart, float[]>();
            int currentPiece = 0;

            for (int i = 0; i < track.parts.Length; i++)
            {
                var part = track.parts[i];
                var partPositionsList = new List<float>();
                for (int j = currentPiece; j < piecePositions.Length; j++)
                {
                    float pos = piecePositions[j];

                    if (pos >= part.Start && pos < (part.Start + part.Length))
                    {
                        partPositionsList.Add(pos);
                    }
                    else
                    {
                        currentPiece = j;
                        break;
                    }
                }

                if (partPositionsList.Count == 0)
                {
                    for (int j = currentPiece; j < piecePositions.Length; j++)
                    {
                        float pos = piecePositions[j];

                        if (pos > part.Start)
                        {
                            partPositionsList.Add(pos);
                            break;
                        }
                    }
                }
                positionsPerPart.Add(part, partPositionsList.ToArray());
            }

            partData = new PartData[track.parts.Length];
            for (int i = 0; i < track.parts.Length; i++)
            {
                var part = track.parts[i];
                var partPositions = positionsPerPart[part];

                PartData newPartData = new PartData();
                partData[i] = newPartData;

                GameObject physicsGO = new GameObject("Belt part(Physics)");
                Transform newTransform = physicsGO.transform;
                newTransform.parent = transform;
                newPartData.transform = newTransform;

                int piecesLength = (int)(part.Length / perPieceLength) + 1;
                newPartData.remainingDistance = part.Length % perPieceLength;
                newPartData.isLinear = part is LinearTrackPart;

                if (newPartData.isLinear)
                {
                    Vector3 originLocalPosition = part.GetLocalPosition(0);
                    Quaternion originLocalRotation = part.GetLocalRotation(0, false);
                    newTransform.localPosition = originLocalPosition;
                    newTransform.localRotation = originLocalRotation;
                    newPartData.origin = originLocalPosition;
                    newPartData.dir = transform.InverseTransformDirection(newTransform.forward).normalized;

                    var collider = physicsGO.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0.0f, 0.0f, part.Length * 0.5f - perPieceLength * 0.5f);
                    collider.size = new Vector3(colliderSize.x, colliderSize.y, part.Length);
                }
                else
                {
                    var radial = part as RadialTrackPart;
                    newTransform.localPosition = radial.center;
                    newTransform.localRotation = Quaternion.identity;
                    newPartData.origin = radial.center;
                    newPartData.radius = radial.radius;
                }

                MeshCreator meshCreator = new MeshCreator();

                for (int j = 0; j < piecesLength; j++)
                {
                    Vector3 lp = part.GetLocalPosition(j * perPieceLength);
                    Quaternion lr = part.GetLocalRotation(j * perPieceLength, false);

                    newPartData.renderProgress = newPartData.progress = partPositions[0] - part.Start;

                    Matrix4x4 matrix = Matrix4x4.TRS(
                        newTransform.InverseTransformPoint(transform.TransformPoint(lp)),
                        Quaternion.Inverse(newTransform.rotation) * transform.rotation * lr,
                        Vector3.one
                        );

                    meshCreator.AddMesh(pieceMesh, matrix);
                    if (j == (piecesLength - 2))
                        newPartData.shortMesh = meshCreator.CreateNewMesh();

                    if (!newPartData.isLinear)
                    {
                        var colliderObject = new GameObject();
                        colliderObject.transform.parent = transform;
                        colliderObject.transform.localPosition = lp;
                        colliderObject.transform.localRotation = lr;
                        var collider = colliderObject.AddComponent<BoxCollider>();
                        collider.size = new Vector3(colliderSize.x, colliderSize.y, perPieceLength);
                        colliderObject.transform.parent = physicsGO.transform;
                        newPartData.lastRadialCollider = collider;
                    }
                }

                GameObject renderGO = new GameObject("Belt part(Rendering)");
                renderGO.transform.position = physicsGO.transform.position;
                renderGO.transform.rotation = physicsGO.transform.rotation;
                renderGO.transform.parent = physicsGO.transform.parent;
                newPartData.renderTransform = renderGO.transform;

                newPartData.longMesh = meshCreator.CreateNewMesh();

                var meshRenderer = renderGO.AddComponent<MeshRenderer>();
                meshRenderer.material = pieceMaterial;

                newPartData.meshFilter = renderGO.AddComponent<MeshFilter>();
                newPartData.meshFilter.sharedMesh = newPartData.longMesh;

                newPartData.rb = physicsGO.AddComponent<Rigidbody>();
                newPartData.rb.isKinematic = true;

                if (conveyorCustomObject != null)
                    (physicsGO.AddComponent(conveyorCustomObject) as IConveyorCustomObject)?.Initialize(this);
            }

            for (int i = 0; i < pieceAttachments.Length; i++)
            {
                var attachment = pieceAttachments[i];
                var newAttachmentGO = GameObject.Instantiate(attachment.prefab);
                newAttachmentGO.transform.parent = transform;
                attachment.rb = newAttachmentGO.GetComponent<Rigidbody>();
            }

            UpdateAttachmentPositions(0.0f, false);
        }
        private void FixedUpdate()
        {
            float deltaMovement = speed * Time.fixedDeltaTime;

            for (int i = 0; i < axis.Length; i++)
            {
                float direction = axisData[i].concave ? -1.0f : 1.0f;
                axisData[i].axisRotation += direction * (deltaMovement * 360.0f) / (2f * Mathf.PI * axisData[i].radius);
                Quaternion axisRotationQuaternion = transform.rotation * Quaternion.Euler(axisData[i].axisRotation, 0.0f, 0.0f);
                axis[i].body.MoveRotation(axisRotationQuaternion);
            }

            for (int i = 0; i < partData.Length; i++)
            {
                var partData = this.partData[i];
                var part = track.parts[i];

                partData.progress += deltaMovement;

                bool gapFrame = false;
                if (partData.progress >= perPieceLength)
                {
                    partData.progress -= perPieceLength;
                    gapFrame = true;
                }
                else if (partData.progress < 0.0f)
                {
                    partData.progress += perPieceLength;
                    gapFrame = true;
                }

                if (partData.isLinear)
                {
                    Vector3 localPos = part.GetLocalPosition(partData.progress);
                    if (gapFrame)
                        partData.transform.localPosition = localPos;
                    else
                        partData.rb.MovePosition(transform.TransformPoint(localPos));
                }
                else
                {
                    Quaternion localRotation = part.GetLocalRotation(partData.progress, true);
                    if (gapFrame)
                        partData.transform.localRotation = localRotation;
                    else
                        partData.rb.MoveRotation(transform.rotation * localRotation);
                }
            }

            UpdateAttachmentPositions(deltaMovement);

            UpdateGrabbingPieces(deltaMovement);
        }

        private void UpdateAttachmentPositions(float delta, bool useRigidbody = true)
        {
            for (int i = 0; i < pieceAttachments.Length; i++)
            {
                var attachment = pieceAttachments[i];
                attachment.position += delta;

                track.CalculatePieceTransform(ref attachment.position, out Vector3 localPosition, out Quaternion localRotation);
                if (useRigidbody)
                {
                    attachment.rb.MoveRotation(transform.rotation * localRotation);
                    attachment.rb.MovePosition(transform.TransformPoint(localPosition));
                }
                else
                {
                    attachment.rb.transform.localRotation = localRotation;
                    attachment.rb.transform.localPosition = localPosition;
                }
            }
        }
        private void Update()
        {
            float deltaMovement = speed * Time.deltaTime;

            for (int i = 0; i < partData.Length; i++)
            {
                var partData = this.partData[i];
                var part = track.parts[i];

                partData.renderProgress += deltaMovement;

                if (partData.renderProgress >= perPieceLength)
                    partData.renderProgress -= perPieceLength;
                else if (partData.renderProgress < 0.0f)
                    partData.renderProgress += perPieceLength;

                bool isLongNow = partData.renderProgress < partData.remainingDistance;
                if (partData.isLong != isLongNow)
                {
                    partData.isLong = isLongNow;
                    partData.meshFilter.sharedMesh = partData.isLong ? partData.longMesh : partData.shortMesh;
                    if (!partData.isLinear)
                    {
                        partData.lastRadialCollider.enabled = false;
                    }
                }

                if (partData.isLinear)
                {
                    partData.renderTransform.localPosition = part.GetLocalPosition(partData.renderProgress);
                }
                else
                {
                    partData.renderTransform.localRotation = part.GetLocalRotation(partData.renderProgress, true);
                }
            }
        }
        public Rigidbody CreateGrabbingPiece(Vector3 worldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            float minDistance = float.MaxValue;
            Vector3 newPieceLocalPos = Vector3.zero;
            Quaternion newPieceLocalRot = Quaternion.identity;

            float pieceBeltPos = 0;

            for (int i = 0; i < pieceCount; i++)
            {
                float pos = i * perPieceLength;
                track.CalculatePieceTransform(ref pos, out Vector3 pieceLocalPosition, out Quaternion pieceLocalRotation);
                float newDistance = Vector3.Distance(localPos, pieceLocalPosition);
                if (newDistance < minDistance)
                {
                    minDistance = newDistance;
                    pieceBeltPos = pos;
                    newPieceLocalPos = pieceLocalPosition;
                    newPieceLocalRot = pieceLocalRotation;
                }
            }

            GameObject fakeGrab = new GameObject("Conveyor grab point");

            Rigidbody body = fakeGrab.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.transform.position = body.position = transform.TransformPoint(newPieceLocalPos);
            body.transform.rotation = body.rotation = transform.rotation * newPieceLocalRot;

            grabbingPieces.Add(new GrabbingPiece { pieceBeltPos = pieceBeltPos, body = body });

            return body;
        }

        private void UpdateGrabbingPieces(float dt)
        {
            for (int i = 0; i < grabbingPieces.Count; i++)
            {
                var piece = grabbingPieces[i];
                if (piece.body == null)
                {
                    grabbingPieces.RemoveAt(i);
                    i--;
                }
                else
                {
                    piece.pieceBeltPos += dt;

                    track.CalculatePieceTransform(ref piece.pieceBeltPos, out Vector3 localPosition, out Quaternion localRotation);
                    piece.body.MovePosition(transform.TransformPoint(localPosition));
                    piece.body.MoveRotation(transform.rotation * localRotation);
                }
            }
        }

        private void ClearEverything()
        {
            for (int i = 0; i < partData.Length; i++)
            {
                var part = partData[i];
                if (part.renderTransform != null)
                    DestroyImmediate(part.renderTransform.gameObject);
                if (part.transform != null)
                    DestroyImmediate(part.transform.gameObject);
            }

            for (int i = 0; i < pieceAttachments.Length; i++)
            {
                var attachment = pieceAttachments[i];
                if (attachment.rb != null)
                    DestroyImmediate(attachment.rb.gameObject);
            }
        }
    }
}
