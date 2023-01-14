using UnityEngine;

namespace NBG.Core
{
    public static class VectorExtensions
    {
        public static Vector3 To3D(this Vector2 v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        public static Vector3 To3D(this Vector2 v2, float y)
        {
            return new Vector3(v2.x, y, v2.y);
        }
        public static Vector3 ZeroY(this Vector3 v2)
        {
            return new Vector3(v2.x, 0, v2.z);
        }
        public static Vector3 InvertZ(this Vector3 v2)
        {
            return new Vector3(v2.x, v2.y, -v2.z);
        }
        public static Vector3 SetY(this Vector3 v2, float y)
        {
            return new Vector3(v2.x, y, v2.z);
        }
        public static Vector3 SetX(this Vector3 v2, float x)
        {
            return new Vector3(x, v2.y, v2.z);
        }
        public static Vector3 SetZ(this Vector3 v2, float z)
        {
            return new Vector3(v2.x, v2.y, z);
        }
        public static Vector2 To2D(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }
        public static Vector3 XZtoXY(this Vector3 v3)
        {
            return new Vector3(v3.x, v3.z, -v3.y);
        }
        public static Vector3 XYtoXZ(this Vector3 v3)
        {
            return new Vector3(v3.x, -v3.z, v3.y);
        }

        public static Vector2 Rotate(this Vector2 p, float angle)
        {
            var cs = Mathf.Cos(-angle);
            var sn = Mathf.Sin(-angle);
            return new Vector2(
                    p.x * cs - p.y * sn,
                    p.x * sn + p.y * cs);

        }
        public static Vector2 RotateDeg(this Vector2 p, float angle)
        {
            angle *= Mathf.Deg2Rad;
            var cs = Mathf.Cos(-angle);
            var sn = Mathf.Sin(-angle);
            return new Vector2(
                    p.x * cs - p.y * sn,
                    p.x * sn + p.y * cs);

        }
        public static Vector3 Rotate(this Vector3 p, Vector3 axis, float angle)
        {
            return Quaternion.AngleAxis(angle, axis) * p;

        }
        public static Vector3 RotateYCW90(this Vector3 p)
        {
            return new Vector3(p.z, p.y, -p.x);
        }
        public static Vector3 RotateYDeg(this Vector3 p, float angle)
        {
            return p.RotateY(angle * Mathf.Deg2Rad);
        }
        public static Vector3 RotateY(this Vector3 p, float angle)
        {
            var cs = Mathf.Cos(-angle);
            var sn = Mathf.Sin(-angle);
            return new Vector3(
                    p.x * cs - p.z * sn,
                    p.y,
                    p.x * sn + p.z * cs);
        }
        public static Vector3 RotateZCW90(this Vector3 p)
        {
            return new Vector3(p.y, -p.x, p.z);
        }
        public static Vector2 RotateCW90(this Vector2 p)
        {
            return new Vector2(p.y, -p.x);
        }
        public static bool HasNaN(this Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }

        // returns vector expressed in reference frame represented by 3 point plane
        public static Vector3 ToPlaneCoords(this Vector3 v, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var t = Math3d.GetPlaneRotation(p1, p2, p3);
            return Quaternion.Inverse(t) * (v - p1);

        }
        // returns vector expressed in reference frame represented by 3 point plane
        public static Vector3 FromPlaneCoords(this Vector3 v, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var t = Math3d.GetPlaneRotation(p1, p2, p3);
            return p1 + t * v;

        }

        public static Vector3 OrthoNormalize(this Vector3 v, Vector3 normal)
        {
            Vector3.OrthoNormalize(ref normal, ref v);
            return v;
        }

        public static string Format(this Vector3 v)
        {
            return System.String.Format("({0:F5}, {1:F5}, {2:F5})", v.x, v.y, v.z);
        }
        public static Vector3 ClampAndInverseLerp(this Vector3 vec, float min, float max)
        {
            vec.x = Mathf.InverseLerp(min, max, Mathf.Clamp(vec.x, min, max));
            vec.y = Mathf.InverseLerp(min, max, Mathf.Clamp(vec.y, min, max));
            vec.z = Mathf.InverseLerp(min, max, Mathf.Clamp(vec.z, min, max));
            return vec;
        }
        public static Vector3 AbsComponents(this Vector3 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            vec.z = Mathf.Abs(vec.z);
            return vec;
        }
    }
}