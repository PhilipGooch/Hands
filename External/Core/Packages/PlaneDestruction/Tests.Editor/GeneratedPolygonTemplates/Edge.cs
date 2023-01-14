using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class Edge
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.25f, 0f), 
			new float3(0.25f, 0.25f, 0f), 
			new float3(0.25f, 0.75f, 0f), 
			new float3(0f, 0.75f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(0.75f, 0.25f, 0f), 
			new float3(1f, 0.25f, 0f), 
			new float3(1f, 0f, 0f)
		};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.25f, 0f), 
			new float3(0.25f, 0.25f, 0f), 
			new float3(0.25f, 0.75f, 0f), 
			new float3(0f, 0.75f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(0.75f, 0.25f, 0f), 
			new float3(1f, 0.25f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0.25f, 0.1f), 
			new float3(0.25f, 0.25f, 0.1f), 
			new float3(0.25f, 0.75f, 0.1f), 
			new float3(0f, 0.75f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0.75f, 0.1f), 
			new float3(0.75f, 0.75f, 0.1f), 
			new float3(0.75f, 0.25f, 0.1f), 
			new float3(1f, 0.25f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 0.25f, 0f), 
			new float3(0.25f, 0.25f, 0f), 
			new float3(0.25f, 0.75f, 0f), 
			new float3(0f, 0.75f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(0.75f, 0.25f, 0f), 
			new float3(1f, 0.25f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0.25f, 0.1f), 
			new float3(0.25f, 0.25f, 0.1f), 
			new float3(0.25f, 0.75f, 0.1f), 
			new float3(0f, 0.75f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0.75f, 0.1f), 
			new float3(0.75f, 0.75f, 0.1f), 
			new float3(0.75f, 0.25f, 0.1f), 
			new float3(1f, 0.25f, 0.1f), 
			new float3(1f, 0f, 0.1f)
		};

		public static int[] _triangles =
		{
			1, 
			2, 
			0, 
			4, 
			5, 
			3, 
			5, 
			6, 
			3, 
			7, 
			8, 
			6, 
			6, 
			8, 
			3, 
			3, 
			8, 
			2, 
			8, 
			9, 
			2, 
			2, 
			9, 
			0, 
			9, 
			10, 
			0, 
			10, 
			11, 
			0, 
			13, 
			12, 
			14, 
			16, 
			15, 
			17, 
			17, 
			15, 
			18, 
			19, 
			18, 
			20, 
			18, 
			15, 
			20, 
			15, 
			14, 
			20, 
			20, 
			14, 
			21, 
			14, 
			12, 
			21, 
			21, 
			12, 
			22, 
			22, 
			12, 
			23, 
			24, 
			37, 
			25, 
			24, 
			36, 
			37, 
			25, 
			38, 
			26, 
			25, 
			37, 
			38, 
			26, 
			39, 
			27, 
			26, 
			38, 
			39, 
			27, 
			40, 
			28, 
			27, 
			39, 
			40, 
			28, 
			41, 
			29, 
			28, 
			40, 
			41, 
			29, 
			42, 
			30, 
			29, 
			41, 
			42, 
			30, 
			43, 
			31, 
			30, 
			42, 
			43, 
			31, 
			44, 
			32, 
			31, 
			43, 
			44, 
			32, 
			45, 
			33, 
			32, 
			44, 
			45, 
			33, 
			46, 
			34, 
			33, 
			45, 
			46, 
			34, 
			47, 
			35, 
			34, 
			46, 
			47, 
			35, 
			36, 
			24, 
			35, 
			47, 
			36
		};
	}
}