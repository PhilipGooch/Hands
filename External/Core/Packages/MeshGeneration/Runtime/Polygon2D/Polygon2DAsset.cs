using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.MeshGeneration
{
    [CreateAssetMenu(fileName = "Polygon2DAsset", menuName = "[NBG] PlaneDestructionSystem/Polygon2DAsset", order = 1)]
    public class Polygon2DAsset : ScriptableObject
    {
        public List<float3> points;
        public Polygon2D polygon => new Polygon2D(points);
    }
}
