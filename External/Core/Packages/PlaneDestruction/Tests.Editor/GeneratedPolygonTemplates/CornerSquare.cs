using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class CornerSquare
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(0f, 0.25f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(0.75f, 1f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.25f, 0f, 0f), 
			new float3(0.25f, 0.25f, 0f)
				};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0.25f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(0.75f, 1f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.25f, 0f, 0f), 
			new float3(0.25f, 0.25f, 0f), 
			new float3(0f, 0.25f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(0.75f, 1f, 0.1f), 
			new float3(0.75f, 0.75f, 0.1f), 
			new float3(1f, 0.75f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0.25f, 0f, 0.1f), 
			new float3(0.25f, 0.25f, 0.1f), 
			new float3(0f, 0.25f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(0.75f, 1f, 0f), 
			new float3(0.75f, 0.75f, 0f), 
			new float3(1f, 0.75f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0.25f, 0f, 0f), 
			new float3(0.25f, 0.25f, 0f), 
			new float3(0f, 0.25f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(0.75f, 1f, 0.1f), 
			new float3(0.75f, 0.75f, 0.1f), 
			new float3(1f, 0.75f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0.25f, 0f, 0.1f), 
			new float3(0.25f, 0.25f, 0.1f)
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
			5, 
			6, 
			4, 
			6, 
			7, 
			4, 
			4, 
			7, 
			0, 
			9, 
			8, 
			10, 
			10, 
			8, 
			11, 
			11, 
			8, 
			12, 
			13, 
			12, 
			14, 
			14, 
			12, 
			15, 
			12, 
			8, 
			15, 
			16, 
			25, 
			17, 
			16, 
			24, 
			25, 
			17, 
			26, 
			18, 
			17, 
			25, 
			26, 
			18, 
			27, 
			19, 
			18, 
			26, 
			27, 
			19, 
			28, 
			20, 
			19, 
			27, 
			28, 
			20, 
			29, 
			21, 
			20, 
			28, 
			29, 
			21, 
			30, 
			22, 
			21, 
			29, 
			30, 
			22, 
			31, 
			23, 
			22, 
			30, 
			31, 
			23, 
			24, 
			16, 
			23, 
			31, 
			24
		};
	}
}
