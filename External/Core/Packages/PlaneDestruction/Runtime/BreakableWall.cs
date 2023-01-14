using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using NBG.MeshGeneration;
using NBG.Core;

namespace NBG.PlaneDestructionSystem
{
    public delegate void OnBreak(float3 position);

    [RequireComponent(typeof(MeshCollider))]
    public class BreakableWall : MonoBehaviour
    {
        [SerializeField] private float lastPointThreshold = 0.05f;

        [HideInInspector]
        public bool pour;
        [HideInInspector]
        public float rayDistance;

        private float extrusionDepth = 0.1f;
        [HideInInspector]
        public bool hasBeenCreatedRuntime;
        [HideInInspector]
        public float cutRadious = 0.1f;
        [HideInInspector]
        public int sides = 5;

        [SerializeField] private bool breakOnHit;
        [SerializeField] private float breakMinImpulse = 200;
        [SerializeField] private float pieceDensity = 10;

        [HideInInspector]
        public Polygon2D polygon;
        private List<Polygon2D> staticPolygons, dynamicPolygons, outOfFramePolygons;
        private Vector3 lastBooleanPos;
        private Transform targetTransform;

        private bool hasBeenBrokenThisFrame;
        [HideInInspector]
        public bool hasBeenBroken;
        public VisualEffect effect;
        public VisualEffect[] childEffects;
        public OnBreak onBreak;

        public HashSet<IBreakableWallSubscription> subscriptions = new HashSet<IBreakableWallSubscription>();
        public List<IBreakableWallSubscription> subscriptionRemovables = new List<IBreakableWallSubscription>();

        public enum WallAttachment
        {
            Frame,
            Bottom,
            Top,
            Nothing
        }

        public WallAttachment wallAttachment;

        public enum MaterialType
        {
            Wood,
            Concrete
        }

        public MaterialType materialType = MaterialType.Concrete;
        public PhysicMaterial physicMaterial;
        [ClearOnReload] private static List<BreakableWall> walls;

        private Vector3 initialWallSize;

        private void Awake()
        {
            initialWallSize = transform.localScale;

            (walls ??= new List<BreakableWall>()).Add(this);
            if (effect != null)
                childEffects = effect.GetComponentsInChildren<VisualEffect>();
        }

        private void Start()
        {
            if (!hasBeenCreatedRuntime && GetComponent<BreakableWallDesigner>() == null)
            {
                CreateWallFromSize();

                UpdateAttachment();

                polygon.EarClipping();
                polygon.Extrusion(extrusionDepth);
            }
        }

        public void ApplyPolygon(Polygon2D newpolygon, float thickness = 0.1f, WallAttachment wallAttachment = WallAttachment.Frame)
        {
            polygon?.Dispose();

            polygon = newpolygon;

            this.wallAttachment = wallAttachment;
            UpdateAttachment();

            extrusionDepth = thickness;
            polygon.EarClipping();
            polygon.Extrusion(extrusionDepth);
        }

        private void Update()
        {
            if (polygon.CheckJob())
            {
                SetPolygonMesh(polygon);
                foreach (var subscribedObject in subscriptions)
                    subscribedObject.OnBreakableWallChanged(this);

                foreach (var removedSub in subscriptionRemovables)
                    subscriptions.Remove(removedSub);
            }

            if (!polygon.pendingJob && targetTransform != null)
            {
                float3 dir = math.normalize(transform.InverseTransformDirection(targetTransform.forward));
                float3 origin = transform.InverseTransformPoint(targetTransform.position);

                float3 rayOffset = math.abs(origin.z / dir.z) * dir;
                float length = math.length(rayOffset);
                float3 pos = rayOffset + origin;

                if (length < rayDistance)
                {
                    WoodDrill(pos);
                    PlayParticles(targetTransform.position, -targetTransform.forward, false);
                }
            }

            if (hasBeenBrokenThisFrame)
                hasBeenBrokenThisFrame = false;
        }

        public void ForceUpdate()
        {
            Update();
        }

        public void SetPolygonMesh(Polygon2D polygonWithMesh)
        {
            if (polygonWithMesh.extrudedPolygonVertices.IsCreated)
            {
                Mesh newMesh = new Mesh();

                newMesh.SetVertices(polygonWithMesh.extrudedPolygonVertices);
                newMesh.SetTriangles(polygonWithMesh.extrudedTriangles.ToArray(), 0);

                newMesh.RecalculateNormals();
                GetComponent<MeshFilter>().mesh = newMesh;
                GetComponent<MeshCollider>().sharedMesh = newMesh;
            }
        }

        public void ExplosionAndUpdate(float3 pos, float radius, int breakSides = 12, float radiusRandomDifference = 0.0f)
        {
            if (!polygon.pendingJob && !hasBeenBrokenThisFrame)
            {
                Explosion(pos, radius, breakSides, radiusRandomDifference);
                polygon.EarClipping();
                polygon.Extrusion(extrusionDepth);
            }
        }

        public void BreakAndUpdate(float3 pos, Vector3 velocity, Polygon2D shape, float resize = 1.0f, float cutWidth = 0.05f, bool ignoreCollisionBriefly = false, float customScale = 1.0f, float shatterAngle1 = -1.0f, float shatterAngle2 = -1.0f)
        {
            if (!polygon.pendingJob && !hasBeenBrokenThisFrame)
            {
                pos.z = 0.0f;

                shape.ScaleFromCenter(resize);

                if (shatterAngle1 == -1.0f || shatterAngle2 == -1.0f)
                {
                    shatterAngle1 = UnityEngine.Random.Range(0.0f, Mathf.PI);
                    shatterAngle2 = shatterAngle1 + UnityEngine.Random.Range(Mathf.PI * 0.25f, Mathf.PI * 0.75f);
                }

                //Debug.Log("Hitting pos:" + pos + "\n" + shatterAngle1 + "\n" + shatterAngle2 + "\n" + Polygon2D.ToArrayText(polygon.vertices) + "\n" + Polygon2D.ToArrayText(shape.vertices));

                Break(shape, pos, velocity, shatterAngle1, shatterAngle2, cutWidth, ignoreCollisionBriefly, customScale);

                polygon.EarClipping();
                polygon.Extrusion(extrusionDepth);
            }
        }

        internal void Break(Polygon2D shape, float3 pos, Vector3 velocity, float shatterAngle1, float shatterAngle2, float cutWidth = 0.05f, bool ignoreCollisionBriefly = false, float customScale = 1.0f)
        {
            if (hasBeenBrokenThisFrame || materialType != MaterialType.Concrete)
                return;
            else
                hasBeenBrokenThisFrame = true;

            hasBeenBroken = true;

            onBreak?.Invoke(pos);

            shape.AddOffset(pos);

            polygon.SubtractPolygon(shape, out staticPolygons, out dynamicPolygons, out outOfFramePolygons);

            //CreateFramePieces(dynamicPolygons, polygon.initialFramePoints);
            //return;

            CreateFramePieces(staticPolygons, polygon.initialFramePoints);
            CreatePieces(outOfFramePolygons, true, Vector3.zero);

            List<Polygon2D> newDynamicPolygons = new List<Polygon2D>();

            Polygon2D shatter = new Polygon2D(pos + new float3(math.cos(shatterAngle1) * -100.0f, math.sin(shatterAngle1) * -100.0f, 0.0f), pos + new float3(math.cos(shatterAngle1) * 100.0f, math.sin(shatterAngle1) * 100.0f, 0.0f), cutWidth);
            Polygon2D shatter2 = new Polygon2D(pos + new float3(math.cos(shatterAngle2) * -100.0f, math.sin(shatterAngle2) * -100.0f, 0.0f), pos + new float3(math.cos(shatterAngle2) * 100.0f, math.sin(shatterAngle2) * 100.0f, 0.0f), cutWidth);

            shatter.BasicAdd(shatter2);

            for (int i = 0; i < dynamicPolygons.Count; i++)
            {
                if (dynamicPolygons[i].IsPolygonValid())
                {
                    dynamicPolygons[i].SubtractPolygon(shatter, out _, out _, out List<Polygon2D> pieces2);
                    newDynamicPolygons.AddRange(pieces2);
                }
            }

            dynamicPolygons = newDynamicPolygons;

            CreatePieces(dynamicPolygons, true, velocity, ignoreCollisionBriefly, customScale);

            if (polygon.isEmpty)
            {
                Destroy(gameObject);
            }
        }

        public bool Explosion(float3 pos, float radius, int breakSides = 12, float radiousRandomDifference = 0.0f)
        {
            float distanceToPlane = math.abs(pos.z);
            pos.z = 0.0f;
            if (distanceToPlane < radius)
            {
                float sphereRadious = math.sqrt(radius * radius - distanceToPlane * distanceToPlane);

                float minRadious = sphereRadious - radiousRandomDifference * 0.5f;
                float maxRadious = sphereRadious + radiousRandomDifference * 0.5f;

                Polygon2D substraction = CreateRandomBreakShape(breakSides, minRadious, maxRadious);
                substraction.AddOffset(pos);

                polygon.SubtractPolygon(substraction, out staticPolygons, out dynamicPolygons, out outOfFramePolygons);

                CreateFramePieces(staticPolygons, polygon.initialFramePoints);
                DisposePieces(outOfFramePolygons);
                DisposePieces(dynamicPolygons);
                return true;
            }
            return false;
        }

        public void CreateWallFromSize()
        {
            Vector3 size = initialWallSize;
            transform.localScale = Vector3.one;

            transform.position -= 0.5f * size.z * transform.forward;
            extrusionDepth = size.z;

            size.x *= 0.5f;
            size.y *= 0.5f;

            polygon = new Polygon2D(new float3[]{
            new float3(size.x, size.y, 0.0f),
            new float3(size.x, -size.y, 0.0f),
            new float3(-size.x, -size.y, 0.0f),
            new float3(-size.x, size.y, 0.0f)});
        }

        public void UpdateAttachment()
        {
            float3[] attachmentPoints = polygon.vertices.ToArray();
            List<float3> newAttachmentPoints = new List<float3>();

            switch (wallAttachment)
            {
                case WallAttachment.Frame:
                    for (int i = 0; i < attachmentPoints.Length; i++)
                    {
                        newAttachmentPoints.Add(attachmentPoints[i]);
                    }
                    polygon.initialFramePoints = newAttachmentPoints.ToArray();
                    break;
                case WallAttachment.Bottom:
                    for (int i = 0; i < attachmentPoints.Length; i++)
                    {
                        if (attachmentPoints[i].y < 0.0f)
                            newAttachmentPoints.Add(attachmentPoints[i]);
                    }
                    polygon.initialFramePoints = newAttachmentPoints.ToArray();
                    break;
                case WallAttachment.Top:
                    for (int i = 0; i < attachmentPoints.Length; i++)
                    {
                        if (attachmentPoints[i].y > 0.0f)
                            newAttachmentPoints.Add(attachmentPoints[i]);
                    }
                    polygon.initialFramePoints = newAttachmentPoints.ToArray();
                    break;
                case WallAttachment.Nothing:
                    polygon.initialFramePoints = null;
                    break;
            }
        }

        public void CreateWall()
        {
            polygon = new Polygon2D(new float3[]{
            new float3(3.0f, 3.0f, 0.0f),
            new float3(3.0f, -3.0f, 0.0f),
            new float3(-3.0f, -3.0f, 0.0f),
            new float3(-3.0f, 3.0f, 0.0f)});
        }

        public void SetTarget(Transform targetTransform)
        {
            this.targetTransform = targetTransform;
        }
        public static void RemoveTarget(Transform transform)
        {
            if (walls?.Count > 0)
            {
                for (int i = 0; i < walls.Count; i++)
                {
                    BreakableWall wall = walls[i];
                    if (wall != null && wall.targetTransform == transform)
                    {
                        wall.SetTarget(null);
                    }
                }
            }
        }
        public void WoodDrill(Vector3 pos)
        {
            if (polygon.pendingJob || materialType != MaterialType.Wood)
                return;

            if (math.distance(lastBooleanPos, pos) > lastPointThreshold)
            {
                lastBooleanPos = pos;
            }
            else
            {
                return;
            }

            float3 polygonPosition = new float3(pos.x, pos.y, 0.0f);
            Polygon2D polygon2 = new Polygon2D(sides, cutRadious, polygonPosition, false);

            polygon.SubtractPolygon(polygon2, out staticPolygons, out dynamicPolygons, out outOfFramePolygons);

            if (staticPolygons != null)
                CreateFramePieces(staticPolygons, polygon.initialFramePoints);

            if (wallAttachment == WallAttachment.Nothing && outOfFramePolygons.Count >= 1)
            {
                polygon.Dispose();
                polygon.SetVertices(outOfFramePolygons[0].vertices);
                outOfFramePolygons[0].Dispose();
                outOfFramePolygons.RemoveAt(0);
            }
            else
            {
                CreatePieces(outOfFramePolygons, true, Vector3.zero);//The out of framePolygons
            }

            DisposePieces(dynamicPolygons);//The subdivision boolean

            polygon.EarClipping();
            polygon.Extrusion(extrusionDepth);

            polygon2.Dispose();

            if (polygon.isEmpty)
            {
                Destroy(gameObject);
            }
        }
        public void CreatePieces(List<Polygon2D> polygons, bool areDynamic, Vector3 velocity, bool ignoreCollisionBriefly = false, float customScale = 1.0f)
        {
            List<MeshCollider> colliders = null;

            if (ignoreCollisionBriefly)
            {
                colliders = new List<MeshCollider>(4);
            }

            for (int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].vertices.Count > 0)
                {
                    GameObject newGameObject = new GameObject();
                    newGameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
                    newGameObject.layer = 10;
                    newGameObject.AddComponent<ProceduralPiece>().SetPolygon(polygons[i], GetComponent<MeshRenderer>().material, extrusionDepth, velocity, areDynamic, pieceDensity * math.abs(polygons[i].PolygonArea()), customScale, physicMaterial, true);

                    if (ignoreCollisionBriefly)
                    {
                        colliders.Add(newGameObject.GetComponent<MeshCollider>());
                    }
                }
            }

            if (ignoreCollisionBriefly)
            {
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (colliders[i] != null)
                        colliders[i].GetComponent<ProceduralPiece>().IgnoreCollidersByDistance(colliders, 0.05f);
                }
            }
        }
        public void CreateFramePieces(List<Polygon2D> polygons, float3[] initialFramePoints)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                GameObject newGameObject = new GameObject();
                newGameObject.transform.parent = transform.parent;
                newGameObject.transform.localPosition = transform.localPosition;
                newGameObject.transform.localRotation = transform.localRotation;

                BreakableWall other = newGameObject.AddComponent<BreakableWall>();
                other.breakOnHit = breakOnHit;

                other.extrusionDepth = extrusionDepth;
                other.hasBeenCreatedRuntime = true;
                other.polygon = polygons[i];
                other.polygon.initialFramePoints = initialFramePoints;

                newGameObject.AddComponent<MeshFilter>();
                newGameObject.AddComponent<MeshRenderer>();

                newGameObject.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;

                other.polygon.EarClipping();
                other.polygon.Extrusion(extrusionDepth);
            }
        }
        public void DisposePieces(List<Polygon2D> polygons)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                polygons[i].Dispose();
            }
        }
        public void OnCollisionEnter(Collision collision)
        {
            if (breakOnHit && collision.impulse.magnitude > breakMinImpulse)
            {
                Polygon2D shape = CreateRandomBreakShape(20, 0.9f, 1.4f);
                BreakByCollision(collision, shape);
            }
        }
        public void BreakByCollision(Collision collision, Polygon2D shape, float cutWidth = 0.05f, float resize = 1.0f)
        {
            Vector3 pos = transform.InverseTransformPoint(collision.contacts[0].point);
            Vector3 velocity = -collision.contacts[0].normal * 2.0f;

            BreakAndUpdate(pos, velocity, shape, resize, cutWidth, true, 0.9f);

            PlayParticles(collision.GetContact(0).point, collision.GetContact(0).normal, true);
        }
        private void PlayEffect(bool play)
        {
            if (play)
            {
                effect.Play();
                effect.gameObject.SetActive(true);
            }
            else
            {
                effect.Stop();
            }
            foreach (VisualEffect VFX in childEffects)
            {
                if (play)
                {
                    VFX.Play();
                    VFX.gameObject.SetActive(true);
                }
                else
                {
                    VFX.Stop();
                }
            }
        }
        public void PlayParticles(Vector3 position, Vector3 normal, bool play)
        {
            // Particles feedback
            if (effect != null)
            {
                effect.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal, Vector3.up));
                if (play)
                    PlayEffect(true);
            }
        }
        public static Polygon2D CreateRandomBreakShape(int breakSides = 20, float minRadious = 0.9f, float maxRadious = 1.4f)
        {
            return new Polygon2D(breakSides, minRadious, maxRadious);
        }
        public void Subscribe(IBreakableWallSubscription newSubscription)
        {
            subscriptions.Add(newSubscription);
        }
        public void Unsubscribe(IBreakableWallSubscription newSubscription)
        {
            subscriptionRemovables.Add(newSubscription);
        }
        public void ClearSubscriptionRemovables()
        {
            subscriptionRemovables.Clear();
        }
        public bool IsWorldPointFill(Vector3 worldPoint)
        {
            float3 localPos = transform.InverseTransformPoint(worldPoint);
            return polygon.IsFill(localPos);
        }
        private void OnDestroy()
        {
            polygon.Dispose();
        }
        private void OnDrawGizmos()
        {
            polygon?.DebugBounds(transform);
        }
    }
}
