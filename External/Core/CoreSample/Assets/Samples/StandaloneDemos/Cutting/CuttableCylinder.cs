using NBG.MeshGeneration;
using Unity.Mathematics;
using UnityEngine;

namespace CoreSample.Cutting
{
    public class CuttableCylinder : MonoBehaviour
    {

        public void Cut(float3 pos, float3 normal, float cutWidth)
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;

            MeshCutting.Cut(mesh, new CutPlane(pos, normal, cutWidth), out Mesh piece1, out Mesh piece2);

            if (piece1.triangles.Length != 0 && piece2.triangles.Length != 0)
            {
                CreatePiece(piece1);
                CreatePiece(piece2);

                Destroy(gameObject);
            }
        }

        public void CreatePiece(Mesh pieceMesh)
        {
            if (pieceMesh != null)
            {
                GameObject newPiece = new GameObject();
                newPiece.transform.position = transform.position;
                newPiece.transform.rotation = transform.rotation;
                newPiece.transform.localScale = transform.localScale;

                newPiece.AddComponent<Rigidbody>();

                var meshfilter = newPiece.AddComponent<MeshFilter>();
                meshfilter.mesh = pieceMesh;

                var meshRenderer = newPiece.AddComponent<MeshRenderer>();
                meshRenderer.material = GetComponent<MeshRenderer>().material;

                var meshCollider = newPiece.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = pieceMesh;

                newPiece.AddComponent<CuttableCylinder>();

            }
        }
    }
}
