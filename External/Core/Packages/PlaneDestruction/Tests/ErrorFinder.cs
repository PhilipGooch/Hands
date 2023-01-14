using NBG.MeshGeneration;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace NBG.PlaneDestructionSystem.Tests
{
    public class ErrorFinder : MonoBehaviour
    {
        public bool start = false;
        public int times = 200;
        public int shapeSides = 4;
        public Vector3 targetPos = new Vector3(0.0f,1.0f,0.0f);
        public float randomOffsetDistance = 0.1f;
        public float minSize = 0.3f;
            public float maxSize = 0.4f;
        public void Update()
        {
            if (start)
            {
                start = false;
                StartCoroutine(FindErrors());
            }
        }
        public IEnumerator FindErrors()
        {
            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            wall.transform.position = new Vector3(0.0f, 5.0f, 3.0f);
            wall.transform.rotation = Quaternion.identity;

            for (int i = 0; i < times; i++)
            {
                yield return BreakTests.SetupWall();

                Vector3 pos = targetPos + (Vector3)UnityEngine.Random.insideUnitCircle * randomOffsetDistance;

                Polygon2D shape = BreakableWall.CreateRandomBreakShape(shapeSides, minSize, maxSize);
                Polygon2D shapeCopy = new Polygon2D(new float3[] { });
                shapeCopy.SetVertices(shape.vertices, true);

                float shatterAngle1 = UnityEngine.Random.Range(0.0f, Mathf.PI);
                float shatterAngle2 = shatterAngle1 + UnityEngine.Random.Range(Mathf.PI * 0.25f, Mathf.PI * 0.75f);

                wall.BreakAndUpdate(
                    pos,
                    Vector3.forward,
                    shape,
                    shatterAngle1: shatterAngle1,
                    shatterAngle2: shatterAngle2
                    );

                yield return new WaitForSeconds(0.2f);

                var pieces = GameObject.FindObjectsOfType<ProceduralPiece>();

                foreach (var piece in pieces)
                {
                    if (piece.HasVertexFurtherAwayThan())
                    {
                        Debug.LogError("New case where big pieces spawn discovered and saved");
                        DestructionTestsCodegen.GenerateBreakCode(shapeCopy, pos, shatterAngle1, shatterAngle2);
                        break;
                    }
                }

                if(pieces.Length == 0)
                {
                    Debug.LogError("New case with no pieces discovered and saved");
                    DestructionTestsCodegen.GenerateBreakCode(shapeCopy, pos, shatterAngle1, shatterAngle2);
                }

                foreach (var piece in pieces)
                {
                    GameObject.Destroy(piece.gameObject);
                }
            }
        }
    }
}
