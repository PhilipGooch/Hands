using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PaperMesh : MonoBehaviour
{
    public void Refresh(List<Vector3> vertices)
    {
        CreateMeshFilter(CreateMesh(vertices));
        CreateMeshRenderer();
    }

    MeshRenderer CreateMeshRenderer()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.sharedMaterial.SetFloat("_Cull", (float)CullMode.Off);
        return meshRenderer;
    }

    MeshFilter CreateMeshFilter(Mesh mesh)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = mesh;
        return meshFilter;
    }

    Mesh CreateMesh(List<Vector3> vertices)
    {
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);
        if (vertices.Count == 4)
        {
            normals.Add(Vector3.back);
            uvs.Add(new Vector2(1, 1));
            triangles.Add(0);
            triangles.Add(2);
            triangles.Add(3);
        }
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            normals = normals.ToArray(),
            uv = uvs.ToArray(),
            triangles = triangles.ToArray()
        };
        return mesh;
    }
}
