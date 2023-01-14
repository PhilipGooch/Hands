using NBG.Core;
using UnityEngine;

namespace Plugs
{
    public static class PlugAndHoleGizmos
    {
        public static void DrawGizmo(HoleType holeType, Matrix4x4 matrix, Vector3 direction, Color color, float size)
        {
            switch (holeType)
            {
                case HoleType.FreeZRotation:
                    DrawFreeZRotation(matrix, direction, color);
                    break;
                case HoleType.Double:
                    DrawDouble(matrix, size, color);
                    break;
                case HoleType.Fixed:
                    DrawFixed(matrix, size, color);
                    break;
                case HoleType.NoConstraints:
                    DrawNoConstraints(matrix, color);
                    break;
                case HoleType.FreeXRotation:
                    DrawFreeXRotation(matrix, size, color);
                    break;
            }
        }

        static void DrawFreeZRotation(Matrix4x4 matrix, Vector3 direction, Color color)
        {
            Gizmos.matrix = matrix;
            DebugExtension.DrawArrow((direction / 2), direction, color);
        }

        static void DrawDouble(Matrix4x4 matrix, float size, Color color)
        {
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0.5f, 0.5f) * size);
            Gizmos.DrawWireCube(new Vector3(-0.25f * size, 0, 0.5f * size), new Vector3(0.1f * size, 0.1f * size, 0.5f * size));
            Gizmos.DrawWireCube(new Vector3(0.25f * size, 0, 0.5f * size), new Vector3(0.1f * size, 0.1f * size, 0.5f * size));
            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawFixed(Matrix4x4 matrix, float size, Color color)
        {
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0.8f, 0.5f) * size);
            Gizmos.DrawWireCube(new Vector3(-0.25f * size, 0, 0.5f * size), new Vector3(0.1f * size, 0.1f * size, 0.5f * size));
            Gizmos.DrawWireCube(new Vector3(0.25f * size, 0, 0.5f * size), new Vector3(0.1f * size, 0.1f * size, 0.5f * size));
            Gizmos.DrawWireCube(new Vector3(0, 0.25f * size, 0.5f * size), new Vector3(0.1f * size, 0.1f * size, 0.5f * size));
            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawNoConstraints(Matrix4x4 matrix, Color color)
        {
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(Vector3.zero, 0.3f);
            Gizmos.matrix = Matrix4x4.identity;
        }

        static void DrawFreeXRotation(Matrix4x4 matrix, float size, Color color)
        {
            Gizmos.matrix = matrix;
            DebugExtension.DrawCylinder(new Vector3(-0.1f * size, 0, 0), new Vector3(0.1f * size, 0, 0), color, 0.3f);
        }
    }
}
