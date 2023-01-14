using NBG.MeshGeneration;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NBG.PlaneDestructionSystem
{
    public class ProceduralPiece : MonoBehaviour
    {
        private Polygon2D polygon;

        private List<MeshCollider> ignoredColliders;
        private float ignoreDistance;
        private bool isIgnoring;
        private bool jobDone = false;
        private Vector3 ignoreInitialPosition;
        public bool isDynamic;

        public static bool isTesting;
        internal void SetPolygon(
            Polygon2D newPolygon,
            Material mat,
            float depth,
            Vector3 velocity,
            bool dynamic = true,
            float pieceMass = 10.0f,
            float customScale = 1.0f,
            PhysicMaterial physicMaterial = null,
            bool useBoxCollider = false)
        {
            isDynamic = dynamic;
            newPolygon.Scale(customScale);
            polygon = newPolygon;

            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();

            polygon.EarClipping();
            polygon.Extrusion(depth);

            if (useBoxCollider)
            {
                CreateBoxColliders(newPolygon, gameObject, 0.2f, depth, physicMaterial);
            }
            else
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

                if (physicMaterial != null)
                    meshCollider.material = physicMaterial;
            }

            if (dynamic)
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = pieceMass;
                rb.drag = 0.1f;
                rb.angularDrag = 0.01f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Add small random rotation to prevent the static positioning
            gameObject.transform.Rotate(Random.Range(-5,5),Random.Range(-5,5),Random.Range(-5,5),Space.Self);

            GetComponent<MeshRenderer>().material = mat;

            if (!isTesting)
            {
                if (PlaneDestructionManager.IsHandlerAvailable)
                {
                    PlaneDestructionManager.Handler.OnNewProceduralPiece(this);
                    if (dynamic)
                        PlaneDestructionManager.Handler.SetVelocity(this, velocity, UnityEngine.Random.insideUnitSphere.normalized * 3.0f);
                }
            }
        }

        internal void Update()
        {
            if (polygon.CheckJob())
            {
                gameObject.name = "WrongStaticPiece";
                Mesh newMesh = new Mesh();
                newMesh.SetVertices(polygon.extrudedPolygonVertices);
                newMesh.SetTriangles(polygon.extrudedTriangles.ToArray(), 0);
                newMesh.RecalculateNormals();
                GetComponent<MeshFilter>().mesh = newMesh;

                gameObject.name = "StaticPiece";
                jobDone = true;
            }

            if (isIgnoring && Vector3.Distance(ignoreInitialPosition, transform.position) > ignoreDistance)
            {
                Ignore(false);
                isIgnoring = false;
            }

            if (!isIgnoring && jobDone)
            {
                enabled = false;
            }
        }

        internal void IgnoreCollidersByDistance(List<MeshCollider> colliders, float ignoreDistance)
        {
            ignoredColliders = colliders;
            ignoreInitialPosition = transform.position;
            this.ignoreDistance = ignoreDistance;
            isIgnoring = true;
            Ignore(true);
        }

        internal void Ignore(bool isIgnoring)
        {
            MeshCollider myCollider = GetComponent<MeshCollider>();
            for (int i = 0; i < ignoredColliders.Count; i++)
            {
                if (myCollider != ignoredColliders[i])
                {
                    Physics.IgnoreCollision(myCollider, ignoredColliders[i], isIgnoring);
                }
            }
        }

        internal void OnDestroy()
        {
            if (!isTesting && PlaneDestructionManager.IsHandlerAvailable)
                PlaneDestructionManager.Handler.OnDestroyProceduralPiece(this);

            polygon.Dispose();
        }

        public bool HasVertexFurtherAwayThan(float distance = 30.0f)
        {
            int count = polygon.vertices.Count;

            for (int i = 0; i < count; i++)
                if (math.length(polygon.vertices[i]) > distance)
                    return true;

            return false;
        }

        public static void CreateBoxColliders(Polygon2D inputPolygon, GameObject subject, float width = 0.2f, float extrusionDepth = 0.1f, PhysicMaterial physicMaterial = null)
        {
            BoxCollider[] boxColliders = subject.GetComponents<BoxCollider>();
            for (int i = 0; i < boxColliders.Length; i++)
            {
                Destroy(boxColliders[i]);
            }

            List<float4> newColliders = inputPolygon.GenerateBoxColliders(width);

            for (int i = 0; i < newColliders.Count; i++)
            {
                float4 colliderData = newColliders[i];
                BoxCollider newBox = subject.AddComponent<BoxCollider>();
                if (physicMaterial != null)
                    newBox.material = physicMaterial;
                newBox.center = new Vector3(colliderData.x, colliderData.y, extrusionDepth * 0.5f);
                newBox.size = new Vector3(Mathf.Abs(colliderData.z), Mathf.Abs(colliderData.w), extrusionDepth);
            }
        }
    }
}


