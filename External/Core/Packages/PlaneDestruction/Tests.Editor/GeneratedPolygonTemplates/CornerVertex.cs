using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class CornerVertex
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.9933899f, 0f), 
			new float3(1f, 0.9888705f, 0f), 
			new float3(1f, 0f, 0f)
				};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.9933899f, 0f), 
			new float3(1f, 0.9888705f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0.9933899f, 0.1f), 
			new float3(1f, 0.9888705f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.9933899f, 0f), 
			new float3(1f, 0.9888705f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0.9933899f, 0.1f), 
			new float3(1f, 0.9888705f, 0.1f), 
			new float3(1f, 0f, 0.1f)
		};

		public static int[] _triangles =
		{
			1, 
			2, 
			0, 
			2, 
			3, 
			0, 
			3, 
			4, 
			0, 
			4, 
			5, 
			0, 
			7, 
			6, 
			8, 
			8, 
			6, 
			9, 
			9, 
			6, 
			10, 
			10, 
			6, 
			11, 
			12, 
			19, 
			13, 
			12, 
			18, 
			19, 
			13, 
			20, 
			14, 
			13, 
			19, 
			20, 
			14, 
			21, 
			15, 
			14, 
			20, 
			21, 
			15, 
			22, 
			16, 
			15, 
			21, 
			22, 
			16, 
			23, 
			17, 
			16, 
			22, 
			23, 
			17, 
			18, 
			12, 
			17, 
			23, 
			18
		};
	}
}
