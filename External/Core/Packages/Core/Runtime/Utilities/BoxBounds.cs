using UnityEngine;
using Unity.Mathematics;

namespace NBG.Core
{
    public struct BoxBounds
    {
        public float3 size;
        public float3 center;
        public float3 extents { get { return size / 2f; } }
        public float3 max { get { return center + math.mul(rotation, extents); } }
        public float3 min { get { return center - math.mul(rotation, extents); } }
        public quaternion rotation;

        public BoxBounds(BoxCollider collider)
        {
            InitializeFromBoxCollider(collider, out size, out center, out rotation);
        }

        public BoxBounds(SphereCollider collider)
        {
            InitializeFromSphereCollider(collider, out size, out center, out rotation);
        }

        public BoxBounds(MeshCollider collider)
        {
            InitializeFromMeshCollider(collider, out size, out center, out rotation);
        }

        public BoxBounds(CapsuleCollider collider)
        {
            InitializeFromCapsuleCollider(collider, out size, out center, out rotation);
        }

        public BoxBounds(Collider collider)
        {
            if (collider is BoxCollider)
            {
                InitializeFromBoxCollider(collider as BoxCollider, out size, out center, out rotation);
            }
            else if (collider is SphereCollider)
            {
                InitializeFromSphereCollider(collider as SphereCollider, out size, out center, out rotation);
            }
            else if (collider is MeshCollider)
            {
                InitializeFromMeshCollider(collider as MeshCollider, out size, out center, out rotation);
            }
            else if (collider is CapsuleCollider)
            {
                InitializeFromCapsuleCollider(collider as CapsuleCollider, out size, out center, out rotation);
            }
            else
            {
                Debug.LogError("Unsupported collider type for box bounds!", collider.gameObject);
                size = float3.zero;
                center = float3.zero;
                rotation = quaternion.identity;
            }
        }

        public BoxBounds(float3 position, float3 scale, quaternion rotation)
        {
            this.center = position;
            this.size = scale;
            this.rotation = rotation;
        }

        static void InitializeFromBoxCollider(BoxCollider collider, out float3 scale, out float3 position, out quaternion rotation)
        {
            rotation = collider.transform.rotation;
            scale = Vector3.Scale(collider.transform.lossyScale, collider.size);
            position = (float3)collider.transform.position + math.mul(rotation, Vector3.Scale(collider.center, collider.transform.lossyScale));
        }

        static void InitializeFromSphereCollider(SphereCollider collider, out float3 scale, out float3 position, out quaternion rotation)
        {
            rotation = collider.transform.rotation;
            var lossyScale = collider.transform.lossyScale;
            scale = collider.radius * 2 * math.max(math.max(math.abs(lossyScale.x), math.abs(lossyScale.y)), math.abs(lossyScale.z));
            position = (float3)collider.transform.position + math.mul(rotation, Vector3.Scale(collider.center, collider.transform.lossyScale));
        }

        static void InitializeFromMeshCollider(MeshCollider collider, out float3 scale, out float3 position, out quaternion rotation)
        {
            var sharedMesh = collider.sharedMesh;
            rotation = collider.transform.rotation;
            scale = Vector3.Scale(collider.transform.lossyScale, sharedMesh.bounds.size);
            position = (float3)collider.transform.position + math.mul(rotation, Vector3.Scale(sharedMesh.bounds.center, collider.transform.lossyScale));
        }

        static void InitializeFromCapsuleCollider(CapsuleCollider collider, out float3 scale, out float3 position, out quaternion rotation)
        {
            rotation = collider.transform.rotation;
            scale = Vector3.Scale(collider.transform.lossyScale, GetCapsuleSize(collider));
            position = (float3)collider.transform.position + math.mul(rotation, Vector3.Scale(collider.center, collider.transform.lossyScale));
        }

        static float3 GetCapsuleSize(CapsuleCollider collider)
        {
            float height = collider.height;
            float width = collider.radius * 2;
            float3 result = float3.zero;
            for (int i = 0; i < 3; i++)
            {
                if (i == collider.direction)
                {
                    result[i] = height;
                }
                else
                {
                    result[i] = width;
                }
            }
            return result;
        }

        public float3 ClosestPoint(float3 worldSpaceTarget)
        {
            if (math.lengthsq(size) < 0.001f)
            {
                return center;
            }
            var localTarget = InverseTransformPoint(worldSpaceTarget);
            var halfScale = size / 2f;
            var localSpaceClosest = math.clamp(localTarget, -halfScale, halfScale);
            return center + math.mul(rotation, localSpaceClosest);
        }

        public bool Contains(float3 worldSpacePoint)
        {
            var localTarget = InverseTransformPoint(worldSpacePoint);
            var absolutePosition = math.abs(localTarget);
            var halfScale = size / 2f;
            if (absolutePosition.x <= halfScale.x && absolutePosition.y <= halfScale.y && absolutePosition.z <= halfScale.z)
            {
                return true;
            }
            return false;
        }

        public float3 InverseTransformPoint(float3 worldPoint)
        {
            return math.mul(math.inverse(rotation), worldPoint - center);
        }

        public float3 TransformPoint(float3 localPoint)
        {
            return math.mul(rotation, localPoint) + center;
        }

        public float3 InverseTransformDirection(float3 worldVector)
        {
            return math.mul(math.inverse(rotation), worldVector);
        }

        public float3 TransformDirection(float3 localVector)
        {
            return math.mul(rotation, localVector);
        }

        public float2 IntersectRay(float3 worldRayOrigin, float3 worldRayDir)
        {
            var rayOrigin = InverseTransformPoint(worldRayOrigin);
            var rayDir = InverseTransformDirection(worldRayDir);

            var boxMin = -extents;
            var boxMax = extents;

            float3 tMin = (boxMin - rayOrigin) / rayDir;
            float3 tMax = (boxMax - rayOrigin) / rayDir;
            float3 t1 = math.min(tMin, tMax);
            float3 t2 = math.max(tMin, tMax);
            float tNear = math.max(math.max(t1.x, t1.y), t1.z);
            float tFar = math.min(math.min(t2.x, t2.y), t2.z);

            return new float2(tNear, tFar);
        }

        /// <summary>
        /// Grows the bounds to encapsulate the other bounds. Does not change the current bounds rotation.
        /// </summary>
        /// <param name="otherBounds"></param>
        public void Encapsulate(BoxBounds otherBounds)
        {
            var otherMin = otherBounds.min;
            var otherMax = otherBounds.max;
            Encapsulate(otherMin);
            Encapsulate(new float3(otherMin.x, otherMin.y, otherMax.z));
            Encapsulate(new float3(otherMin.x, otherMax.y, otherMin.z));
            Encapsulate(new float3(otherMax.x, otherMin.y, otherMin.z));
            Encapsulate(new float3(otherMin.x, otherMax.y, otherMax.z));
            Encapsulate(new float3(otherMax.x, otherMax.y, otherMin.z));
            Encapsulate(new float3(otherMax.x, otherMin.y, otherMax.z));
            Encapsulate(otherMax);
        }

        /// <summary>
        /// Grows the bounds to encapsulate the point. Does not change the current bounds rotation.
        /// </summary>
        /// <param name="point"></param>
        public void Encapsulate(float3 point)
        {
            var localPos = InverseTransformPoint(point);
            var deltaExtents = localPos - extents;
            var directions = math.sign(deltaExtents);
            deltaExtents = math.abs(localPos) - extents;
            // If absolute pos is smaller than the extents, it's inside the bounds.
            deltaExtents = math.clamp(deltaExtents, 0f, float.MaxValue);

            var directionalDeltas = deltaExtents * directions;
            // Increase size and move center
            size += deltaExtents;
            center += TransformDirection(directionalDeltas / 2f);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DrawGizmos(bool wire = true)
        {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, size);
            if (wire)
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            else
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = matrix;
        }
    }
}
