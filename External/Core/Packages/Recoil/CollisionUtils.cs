using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using NBG.Core;

namespace Recoil
{
    public static class CollisionUtils
    {

        public static bool MatchesLayerMask(int mask, int layer)
        {
            return ((1 << layer) & mask) != 0;
        }
        public static SphereCollider CreateCollisionSphere(string name, float radius)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(1000, 1000, 1000);
            var probeCollider = go.AddComponent<SphereCollider>();
            probeCollider.radius = radius;
            return probeCollider;
        }


        static Collider[] neighbours = new Collider[64];
        static Rigidbody[] bodies = new Rigidbody[32];

        public static int GetBodiesOverlappingSphere(Vector3 point, float radius, out Rigidbody[] list, int layerMask)
        {
            int count = Physics.OverlapSphereNonAlloc(point, radius, neighbours, layerMask, QueryTriggerInteraction.Ignore);
            int x = 0;
            for (int i = 0; i < count; i++)
            {
                bool f = false;
                var neighbour = neighbours[i].attachedRigidbody;
                if (neighbour == null) continue;

                for (int y = 0; y < x; y++)
                {
                    if (bodies[y] == neighbour)
                    {
                        f = true;
                        break;
                    }
                }

                if (!f)
                    bodies[x++] = neighbour;
            }

            list = bodies;
            return x;
        }
        public static bool ComputeCollision(SphereCollider probeCollider, float3 pos, out Collider hitCollider, out float3 hitPoint, out float hitDistance, Transform ignoreRoot = null, int layerMask = -1, bool ignoreBelowSurface = true, List<Transform> ignoredObjects = null)
        {
            var radius = probeCollider.radius;
            hitCollider = null;
            hitPoint = pos;
            hitDistance = radius;
            int count = Physics.OverlapSphereNonAlloc(pos, radius, neighbours, layerMask, QueryTriggerInteraction.Ignore);
            DebugExtension.DebugWireSphere(pos, Color.black, radius);
            for (int i = 0; i < count; ++i)
            {
                var collider = neighbours[i];

                if (ignoreRoot != null && collider.transform.root == ignoreRoot)
                    continue; // skip if part of same hierarchy
                if (ignoredObjects != null)
                {
                    bool cont = false;
                    foreach (var ignoredObj in ignoredObjects)
                    {
                        if (collider.transform.IsChildOf(ignoredObj))
                        {
                            cont = true;
                            break;
                        }
                    }
                    if (cont)
                        continue;
                }

                bool overlapped = Physics.ComputePenetration(
                    probeCollider, pos, Quaternion.identity,
                    collider, collider.transform.position, collider.transform.rotation,
                    out var depenetrateDirection, out var penetrationDistance
                );

                if (!overlapped && !ignoreBelowSurface) penetrationDistance = radius; // overlaps but cant detect hitpoint - inside the mesh
                var dist = radius - penetrationDistance;
                //var dist = ballCollider.radius - penetrationDistance;
                if (dist < hitDistance)
                {
                    hitDistance = dist;
                    hitPoint = pos - (float3)depenetrateDirection * dist;
                    hitCollider = collider;
                }
            }
            return hitDistance < radius;
        }

        static SphereCollider scanCollider;
        public static bool DepenetratePosition(float3 pos, float radius, Transform ignoreRoot, int layers, out float3 depenetratedPos)
        {
            if (scanCollider == null)
                scanCollider = CollisionUtils.CreateCollisionSphere("targetScanCollider", radius);
            if (scanCollider.radius != radius)
                scanCollider.radius = radius;

            depenetratedPos = pos;
            var count = Physics.OverlapSphereNonAlloc(pos, radius, neighbours, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = neighbours[i];
                if (ignoreRoot != null && col.transform.root == ignoreRoot) //TODO: after layer mask is added won't detect own colliders
                    continue; // skip if part of same hierarchy
                if (Physics.ComputePenetration(scanCollider, depenetratedPos, Quaternion.identity, col, col.transform.position, col.transform.rotation, out var dir, out var dist))
                    depenetratedPos += (float3)dir * dist;
            }
            return math.lengthsq(depenetratedPos - pos) > 0;
        }

        public struct GroundObject
        {
            public float3 pos;
            public float dist;
            public float3 normal;
            public Rigidbody body;
        }
        public static GroundObject FindSupportingObject(List<GroundObject> groundObjects)
        {
            var maxUp = 0f;
            GroundObject bestGo = default;
            for (int i = 0; i < groundObjects.Count; i++)
            {
                var go = groundObjects[i];
                var up = go.normal.y * go.dist;
                if (up > maxUp)
                {
                    maxUp = up;
                    bestGo = go;
                }
            }
            return bestGo;
        }

        public static bool DepenetratePosition(float3 pos, float radius, Transform ignoreTree, int layers, out float3 depenetratedPos, out float totalDist, List<GroundObject> groundObjects)
        {
            if (scanCollider == null)
                scanCollider = CollisionUtils.CreateCollisionSphere("targetScanCollider", radius);
            if (scanCollider.radius != radius)
                scanCollider.radius = radius;

            groundObjects.Clear();
            totalDist = 0;
            depenetratedPos = pos;
            var count = Physics.OverlapSphereNonAlloc(pos, radius, neighbours, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = neighbours[i];
                if (ignoreTree != null && col.transform.IsChildOf(ignoreTree)) //TODO: after layer mask is added won't detect own colliders
                    continue; // skip if part of same hierarchy
                if (Physics.ComputePenetration(scanCollider, depenetratedPos, Quaternion.identity, col, col.transform.position, col.transform.rotation, out var normal, out var dist))
                {
                    totalDist += dist;
                    depenetratedPos += (float3)normal * dist;

                    var groundBody = col.GetComponentInParent<Rigidbody>();
                    var groundPos = depenetratedPos - (float3)normal * radius;


                    groundObjects.Add(new GroundObject()
                    {
                        body = groundBody,
                        pos = groundPos,
                        dist = dist,
                        normal = normal
                    });

                }
            }
            return math.lengthsq(depenetratedPos - pos) > 0;
        }
        public static bool CheckSphere(float3 pos, float radius, int layers, Transform ignoreRoot)
        {
            var count = Physics.OverlapSphereNonAlloc(pos, radius, neighbours, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = neighbours[i];
                if (ignoreRoot != null && col.transform.root == ignoreRoot) //TODO: after layer mask is added won't detect own colliders
                    continue; // skip if part of same hierarchy
                return true;
            }
            return false;
        }


        public static float3 GetPoint(this Collision collision)
        {
            return collision.GetContact(0).point;
        }

        // fix for opposite impulse sign
        public static float3 GetImpulse(this Collision collision)
        {
            var imp = collision.impulse;
            if (Vector3.Dot(imp, collision.GetContact(0).normal) < 0)
                return -imp;
            else
                return imp;
        }
    }

    public static class CollisionUtils<T>
    {
        static Collider[] neighbours = new Collider[64];
        static Rigidbody[] bodies = new Rigidbody[32];
        static T[] results = new T[32];
        static List<T> components = new List<T>();
        public static int GetComponentsInSphere(Vector3 point, float radius, out T[] res, int layerMask)
        {
            int count = Physics.OverlapSphereNonAlloc(point, radius, neighbours, layerMask, QueryTriggerInteraction.Ignore);
            int nBodies = 0;
            int nResults = 0;
            for (int i = 0; i < count; i++)
            {
                bool f = false;
                var neighbour = neighbours[i].attachedRigidbody;
                if (neighbour == null) continue;

                for (int j = 0; j < nBodies; j++)
                {
                    if (bodies[j] == neighbour)
                    {
                        f = true;
                        break;
                    }
                }

                if (!f)
                    bodies[nBodies++] = neighbour;

                neighbour.GetComponentsInChildren<T>(components);
                for (int j = 0; j < components.Count; j++)
                    results[nResults++] = components[j];
                components.Clear();
            }
            res = results;

            return nResults;
        }

    }
}