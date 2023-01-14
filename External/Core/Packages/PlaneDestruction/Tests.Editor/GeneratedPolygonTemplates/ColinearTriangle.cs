using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class ColinearTriangle
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.986671f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(1f, 0.9867855f, 0f), 
			new float3(1f, 0f, 0f)
				};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.986671f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(1f, 0.9867855f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0.986671f, 0.1f), 
			new float3(0.5f, 0.5f, 0.1f), 
			new float3(1f, 0.9867855f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.986671f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(1f, 0.9867855f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0.986671f, 0.1f), 
			new float3(0.5f, 0.5f, 0.1f), 
			new float3(1f, 0.9867855f, 0.1f), 
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
			6, 
			5, 
			7, 
			7, 
			5, 
			8, 
			8, 
			5, 
			9, 
			10, 
			16, 
			11, 
			10, 
			15, 
			16, 
			11, 
			17, 
			12, 
			11, 
			16, 
			17, 
			12, 
			18, 
			13, 
			12, 
			17, 
			18, 
			13, 
			19, 
			14, 
			13, 
			18, 
			19, 
			14, 
			15, 
			10, 
			14, 
			19, 
			15
		};
	}
}
