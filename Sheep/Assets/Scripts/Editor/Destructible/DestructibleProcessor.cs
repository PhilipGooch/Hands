using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DestructibleProcessor
{
    class Connection
    {
        public Rigidbody first;
        public Rigidbody second;

        public Connection(Rigidbody first, Rigidbody second)
        {
            this.first = first;
            this.second = second;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Connection;
            if (other != null)
            {
                return (other.first == first && other.second == second) || (other.first == second && other.second == first);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return first.GetHashCode() + second.GetHashCode();
        }
    }

    public static void ProcessDestructible(GameObject target, float strength = 1000f)
    {
        if (target)
        {
            Debug.Log("Processing destructible " + target.name);
            for (int i = 0; i < target.transform.childCount; i++)
            {
                var child = target.transform.GetChild(i);
                EnsureRequiredComponentsExist(child.gameObject);
            }

            var allRigidbodies = new List<Rigidbody>(target.GetComponentsInChildren<Rigidbody>());
            var connectionsToMake = new HashSet<Connection>();

            for (int i = 0; i < target.transform.childCount; i++)
            {
                var child = target.transform.GetChild(i);
                DetectPotentialConnections(child.gameObject, allRigidbodies, connectionsToMake);
            }

            foreach (var connection in connectionsToMake)
            {
                var joint = connection.first.gameObject.AddComponent<FixedJoint>();
                Undo.RegisterCreatedObjectUndo(joint, "Add joint connection");
                joint.connectedBody = connection.second;
                joint.breakForce = strength;
                joint.breakTorque = strength;
            }
        }
    }

    static void EnsureRequiredComponentsExist(GameObject target)
    {
        var rig = target.GetComponent<Rigidbody>();
        var joints = target.GetComponents<Joint>();
        var meshFilters = target.GetComponentsInChildren<MeshFilter>();
        
        if (meshFilters.Length > 0)
        {
            if (rig == null)
            {
                rig = target.AddComponent<Rigidbody>();
                Undo.RegisterCreatedObjectUndo(rig, "Add rigidbody");
                rig.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            foreach (var joint in joints)
            {
                Undo.DestroyObjectImmediate(joint);
            }

            foreach(var filter in meshFilters)
            {
                var collider = filter.GetComponent<Collider>();
                if (collider == null)
                {
                    var meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                    Undo.RegisterCreatedObjectUndo(meshCollider, "Add mesh collider");
                    meshCollider.convex = true;
                }
            }
        }
    }

    static void DetectPotentialConnections(GameObject target, List<Rigidbody> allRigidbodies, HashSet<Connection> connectionsToMake)
    {
        var raycastHits = new RaycastHit[64];
        var meshFilters = target.GetComponentsInChildren<MeshFilter>();
        var currentRig = target.GetComponent<Rigidbody>();
        if (meshFilters.Length > 0)
        {
            foreach(var filter in meshFilters)
            {
                var mesh = filter.sharedMesh;
                var verts = mesh.vertices;
                var normals = mesh.normals;
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var tri0 = triangles[i];
                    var tri1 = triangles[i + 1];
                    var tri2 = triangles[i + 2];
                    var triangleNormal = filter.transform.TransformDirection((normals[tri0] + normals[tri1] + normals[tri2]) / 3f);
                    var triangleCenter = filter.transform.TransformPoint((verts[tri0] + verts[tri1] + verts[tri2]) / 3f) - triangleNormal * 0.01f;

                    var hitCount = Physics.RaycastNonAlloc(new Ray(triangleCenter, triangleNormal), raycastHits, 0.1f);
                    for (int h = 0; h < hitCount; h++)
                    {
                        var hit = raycastHits[h];

                        if (hit.rigidbody == null)
                            continue;

                        if (allRigidbodies.Contains(hit.rigidbody) && currentRig != hit.rigidbody)
                        {
                            var connection = new Connection(currentRig, hit.rigidbody);
                            connectionsToMake.Add(connection);
                        }
                    }
                }
            }
        }
    }
}
