using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class ColinearCorner
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(0f, 0.5f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.5f, 0f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(4.460244E-05f, 0.5049998f, 0f)
				};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0.5f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.5f, 0f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(4.460244E-05f, 0.5049998f, 0f), 
			new float3(0f, 0.5f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0.5f, 0f, 0.1f), 
			new float3(0.5f, 0.5f, 0.1f), 
			new float3(4.460244E-05f, 0.5049998f, 0.1f), 
			new float3(0f, 0.5f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.5f, 0f, 0f), 
			new float3(0.5f, 0.5f, 0f), 
			new float3(4.460244E-05f, 0.5049998f, 0f), 
			new float3(0f, 0.5f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0.5f, 0f, 0.1f), 
			new float3(0.5f, 0.5f, 0.1f), 
			new float3(4.460244E-05f, 0.5049998f, 0.1f)
		};

		public static int[] _triangles =
		{
			3, 
			4, 
			2, 
			4, 
			5, 
			2, 
			2, 
			5, 
			1, 
			5, 
			6, 
			1, 
			1, 
			6, 
			0, 
			10, 
			9, 
			11, 
			11, 
			9, 
			12, 
			9, 
			8, 
			12, 
			12, 
			8, 
			13, 
			8, 
			7, 
			13, 
			14, 
			22, 
			15, 
			14, 
			21, 
			22, 
			15, 
			23, 
			16, 
			15, 
			22, 
			23, 
			16, 
			24, 
			17, 
			16, 
			23, 
			24, 
			17, 
			25, 
			18, 
			17, 
			24, 
			25, 
			18, 
			26, 
			19, 
			18, 
			25, 
			26, 
			19, 
			27, 
			20, 
			19, 
			26, 
			27, 
			20, 
			21, 
			14, 
			20, 
			27, 
			21
		};
	}
}
