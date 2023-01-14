using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Utilities/Transform")]
    public static class UtilityTransform
    {
        [NodeAPI("Position in World Space")]
        public static Vector3 Position(Transform transform)
        {
            return transform.position;
        }

        [NodeAPI("Rotation in World Space")]
        public static Quaternion Rotation(Transform transform)
        {
            return transform.rotation;
        }

        [NodeAPI("World Space")]
        public static void World(Transform transform, out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;
        }

        [NodeAPI("Position in Local Space")]
        public static Vector3 LocalPosition(Transform transform)
        {
            return transform.localPosition;
        }

        [NodeAPI("Rotation in Local Space")]
        public static Quaternion LocalRotation(Transform transform)
        {
            return transform.localRotation;
        }

        [NodeAPI("Local Space")]
        public static void Local(Transform transform, out Vector3 position, out Quaternion rotation)
        {
            position = transform.localPosition;
            rotation = transform.localRotation;
        }
    }
}
