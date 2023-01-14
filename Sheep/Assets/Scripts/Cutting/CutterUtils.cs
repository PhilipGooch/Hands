using NBG.Actor;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Sheep.Cutter
{
    internal static class CutterUtils
    {
        static RaycastHit[] hits = new RaycastHit[16];
        const float parallelCheck = 0.08f;

        internal static void AddKeysCollectionToList(this List<Rigidbody> list, Dictionary<Rigidbody, CutData> dictionary)
        {
            foreach (var pair in dictionary)
            {
                list.Add(pair.Key);
            }
        }

        internal static GameObject CopyComponents(GameObject source, GameObject copyTo)
        {
            foreach (var component in source.GetComponents<Component>())
            {
                if (component.GetType() == typeof(InteractableEntity) ||
                    component.GetType() == typeof(GrabParamsBinding) ||
                    component.GetType() == typeof(ActorComponent))
                    CopyComponent(component, copyTo);
            }

            return copyTo;
        }

        static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            foreach (System.Reflection.FieldInfo field in type.GetFields())
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static void SetJointDamping(this ConfigurableJoint joint, float linearDamping, float angularDampint)
        {
            var angularXDrive = joint.angularXDrive;
            angularXDrive.positionDamper = angularDampint;
            joint.angularXDrive = angularXDrive;

            var angularYZDrive = joint.angularYZDrive;
            angularYZDrive.positionDamper = angularDampint;
            joint.angularYZDrive = angularYZDrive;

            var xDrive = joint.xDrive;
            xDrive.positionDamper = linearDamping;
            joint.xDrive = xDrive;

            var yDrive = joint.yDrive;
            yDrive.positionDamper = linearDamping;
            joint.yDrive = yDrive;

            var zDrive = joint.zDrive;
            zDrive.positionDamper = linearDamping;
            joint.zDrive = zDrive;
        }

        #region IsObjectInside

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
        internal static Vector3[] GetCornerPoints(Rigidbody other, MeshFilter meshFilter)
        {
            Vector3[] corners = new Vector3[8];

            Vector3 size = meshFilter.sharedMesh.bounds.extents;
            Vector3 center = meshFilter.sharedMesh.bounds.center;

            // Get the unrotated points
            corners[0] = new Vector3(-size.x, -size.y, -size.z) + center; // Bot left near
            corners[1] = new Vector3(size.x, -size.y, -size.z) + center; // Bot right near
            corners[2] = new Vector3(-size.x, size.y, -size.z) + center; // Top left near
            corners[3] = new Vector3(size.x, size.y, -size.z) + center; // Top right near
            corners[4] = new Vector3(-size.x, -size.y, size.z) + center; // Bot left far
            corners[5] = new Vector3(size.x, -size.y, size.z) + center; // Bot right far
            corners[6] = new Vector3(-size.x, size.y, size.z) + center; // Top left far
            corners[7] = new Vector3(size.x, size.y, size.z) + center; // Top right far

            for (int t = 0; t < corners.Length; t++)
            {
                corners[t] = other.transform.TransformPoint(corners[t]);
            }

            return corners;
        }

        internal static void CheckIfSliceInside(Rigidbody other, MeshFilter meshFilter, Collider bladeCollider, Vector3 normal, ref bool[] controlPointsCutState)
        {
            Vector3[] points = GetCornerPoints(other, meshFilter);

            IsInsideNearFar(normal, points, bladeCollider, ref controlPointsCutState);
            IsInsideUpDown(normal, points, bladeCollider, ref controlPointsCutState);
            IsInsideRightLeft(normal, points, bladeCollider, ref controlPointsCutState);
        }

        static bool IsParallelToBlade(Vector3 dir, Vector3 bladeNormal)
        {
            //cannot cut edges which are parallel to the blade
            var dot = Mathf.Abs(Vector3.Dot(bladeNormal.normalized, dir.normalized));
            if (dot <= parallelCheck)
            {
                return true;
            }

            return false;
        }

        static void IsInsideNearFar(Vector3 normal, Vector3[] corners, Collider bladeCollider, ref bool[] controlPointsCutState)
        {
            Vector3 dir = (corners[4] - corners[0]).normalized;

            if (IsParallelToBlade(dir, normal))
                return;

            float dist = Vector3.Distance(corners[0], corners[4]);

            if (!controlPointsCutState[0])
            {
                controlPointsCutState[0] = IsInsideOfBlade(corners[0], dir, dist, bladeCollider);
                //need to check from opposite corner in case this corner is inside of the blade and raycast doesnt detect it 
                if (!controlPointsCutState[0])
                {
                    controlPointsCutState[0] = IsInsideOfBlade(corners[4], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[1])
            {
                controlPointsCutState[1] = IsInsideOfBlade(corners[1], dir, dist, bladeCollider);
                if (!controlPointsCutState[1])
                {
                    controlPointsCutState[1] = IsInsideOfBlade(corners[5], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[2])
            {
                controlPointsCutState[2] = IsInsideOfBlade(corners[2], dir, dist, bladeCollider);
                if (!controlPointsCutState[2])
                {
                    controlPointsCutState[2] = IsInsideOfBlade(corners[6], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[3])
            {
                controlPointsCutState[3] = IsInsideOfBlade(corners[3], dir, dist, bladeCollider);
                if (!controlPointsCutState[3])
                {
                    controlPointsCutState[3] = IsInsideOfBlade(corners[7], dir, dist, bladeCollider);
                }
            }
        }

        static void IsInsideUpDown(Vector3 normal, Vector3[] corners, Collider bladeCollider, ref bool[] controlPointsCutState)
        {
            Vector3 dir = (corners[0] - corners[2]).normalized;

            if (IsParallelToBlade(dir, normal))
                return;

            float dist = Vector3.Distance(corners[0], corners[2]);

            if (!controlPointsCutState[4])
            {
                controlPointsCutState[4] = IsInsideOfBlade(corners[2], dir, dist, bladeCollider);
                //need to check from opposite corner in case this corner is inside of the blade and raycast doesnt detect it 
                if (!controlPointsCutState[4])
                {
                    controlPointsCutState[4] = IsInsideOfBlade(corners[0], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[5])
            {
                controlPointsCutState[5] = IsInsideOfBlade(corners[3], dir, dist, bladeCollider);
                if (!controlPointsCutState[5])
                {
                    controlPointsCutState[5] = IsInsideOfBlade(corners[1], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[6])
            {
                controlPointsCutState[6] = IsInsideOfBlade(corners[6], dir, dist, bladeCollider);
                if (!controlPointsCutState[6])
                {
                    controlPointsCutState[6] = IsInsideOfBlade(corners[4], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[7])
            {
                controlPointsCutState[7] = IsInsideOfBlade(corners[7], dir, dist, bladeCollider);
                if (!controlPointsCutState[7])
                {
                    controlPointsCutState[7] = IsInsideOfBlade(corners[5], -dir, dist, bladeCollider);
                }
            }
        }

        static void IsInsideRightLeft(Vector3 normal, Vector3[] corners, Collider bladeCollider, ref bool[] controlPointsCutState)
        {
            Vector3 dir = (corners[0] - corners[1]).normalized;

            if (IsParallelToBlade(dir, normal))
                return;

            float dist = Vector3.Distance(corners[0], corners[1]);

            if (!controlPointsCutState[8])
            {
                controlPointsCutState[8] = IsInsideOfBlade(corners[1], dir, dist, bladeCollider);
                //need to check from opposite corner in case this corner is inside of the blade and raycast doesnt detect it 
                if (!controlPointsCutState[8])
                {
                    controlPointsCutState[8] = IsInsideOfBlade(corners[0], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[9])
            {
                controlPointsCutState[9] = IsInsideOfBlade(corners[3], dir, dist, bladeCollider);
                if (!controlPointsCutState[9])
                {
                    controlPointsCutState[9] = IsInsideOfBlade(corners[2], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[10])
            {
                controlPointsCutState[10] = IsInsideOfBlade(corners[5], dir, dist, bladeCollider);
                if (!controlPointsCutState[10])
                {
                    controlPointsCutState[10] = IsInsideOfBlade(corners[4], -dir, dist, bladeCollider);
                }
            }

            if (!controlPointsCutState[11])
            {
                controlPointsCutState[11] = IsInsideOfBlade(corners[7], dir, dist, bladeCollider);
                if (!controlPointsCutState[11])
                {
                    controlPointsCutState[11] = IsInsideOfBlade(corners[6], -dir, dist, bladeCollider);
                }
            }
        }

        static bool IsInsideOfBlade(Vector3 from, Vector3 dir, float dist, Collider bladeCollider)
        {
            int count = Physics.RaycastNonAlloc(from, dir, hits, dist, Physics.DefaultRaycastLayers);

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == bladeCollider)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
