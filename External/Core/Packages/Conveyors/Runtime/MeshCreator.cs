using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Conveyors
{
    public class MeshCreator
    {
        private List<Vector3> pos;
        private List<Vector3> normal;
        private List<Vector2> uv;
        private List<int> indexes;
        private int lastIndex;
        public MeshCreator()
        {
            pos = new List<Vector3>();
            normal = new List<Vector3>();
            uv = new List<Vector2>();
            indexes = new List<int>();
        }
        public void AddMesh(Mesh mesh, Matrix4x4 matrix)
        {
            var vertices = mesh.vertices;
            
            for(int i = 0; i < vertices.Length; i++)
            {
                pos.Add(matrix.MultiplyPoint3x4(vertices[i]));
                uv.Add(mesh.uv[i]);
                normal.Add((matrix * mesh.normals[i]).normalized);
            }

            var otherIndexes = mesh.GetIndices(0);
            for (int j = 0; j < otherIndexes.Length; j++)
                indexes.Add(otherIndexes[j]+ lastIndex);

            lastIndex += vertices.Length;
        }

        public Mesh CreateNewMesh()
        {
            Mesh newMesh = new Mesh();
            newMesh.vertices = pos.ToArray();
            newMesh.normals = normal.ToArray();
            newMesh.uv = uv.ToArray();
            newMesh.SetIndices(indexes.ToArray(),MeshTopology.Triangles,0);
            return newMesh;
        }
    }
}
