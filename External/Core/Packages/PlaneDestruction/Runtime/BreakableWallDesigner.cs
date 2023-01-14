using NBG.MeshGeneration;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    public class BreakableWallDesigner : MonoBehaviour
    {
        [SerializeField] private float thickness = 0.1f;
        [SerializeField] private List<float3> mainShape;
        [SerializeField] private List<List<float3>> holes;

        public void Start()
        {
            CreatePolygon();
        }

        public void CreatePolygon()
        {
            SaveData();
            Polygon2D polygon = new Polygon2D(mainShape)
            {
                holes = holes
            };

            GetComponent<BreakableWall>().ApplyPolygon(polygon, thickness);
        }

        public void ForceGenerateMesh()
        {
            BreakableWall wall = GetComponent<BreakableWall>();
            if (wall.polygon != null)
                wall.ForceUpdate();
        }

        public void SaveData()
        {
            Transform mainShapeTransform = transform.GetChild(0);
            int mainShapeChildCount = mainShapeTransform.childCount;

            if (mainShape == null)
                mainShape = new List<float3>();
            else
                mainShape.Clear();

            for (int i = 0; i < mainShapeChildCount; i++)
            {
                mainShape.Add(mainShapeTransform.GetChild(i).localPosition);
            }

            Transform holesTransform = transform.GetChild(1);
            int holesCount = holesTransform.childCount;

            if (holes == null)
                holes = new List<List<float3>>();
            else
                holes.Clear();
            for (int i = 0; i < holesCount; i++)
            {
                Transform hole = holesTransform.GetChild(i);
                int holeVertexCount = hole.transform.childCount;
                if (holeVertexCount > 0)
                {
                    List<float3> newHoleVertexStream = new List<float3>();
                    for (int j = 0; j < holeVertexCount; j++)
                    {
                        Vector3 pos = hole.GetChild(j).localPosition;
                        pos.z = 0.0f;
                        hole.GetChild(j).localPosition = pos;
                        newHoleVertexStream.Add(pos);
                    }
                    holes.Add(newHoleVertexStream);
                }
            }
        }
    }
}
