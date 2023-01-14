using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class SquareWithTwoHole
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f)
				};

		public static float3[][] holes =
		{
			new float3[] {
				new float3(0.125f, 0.375f, 0f), 
				new float3(0.125f, 0.625f, 0f), 
				new float3(0.375f, 0.625f, 0f), 
				new float3(0.375f, 0.375f, 0f)
			},

			new float3[] {
				new float3(0.625f, 0.375f, 0f), 
				new float3(0.625f, 0.625f, 0f), 
				new float3(0.875f, 0.625f, 0f), 
				new float3(0.875f, 0.375f, 0f)
			}
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0f, 0f), 
			new float3(0.125f, 0.375f, 0f), 
			new float3(0.375f, 0.375f, 0f), 
			new float3(0.375f, 0.625f, 0f), 
			new float3(0.625f, 0.375f, 0f), 
			new float3(0.875f, 0.375f, 0f), 
			new float3(0.875f, 0.625f, 0f), 
			new float3(0.625f, 0.625f, 0f), 
			new float3(0.625f, 0.375f, 0f), 
			new float3(0.375f, 0.625f, 0f), 
			new float3(0.125f, 0.625f, 0f), 
			new float3(0.125f, 0.375f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0.125f, 0.375f, 0.1f), 
			new float3(0.375f, 0.375f, 0.1f), 
			new float3(0.375f, 0.625f, 0.1f), 
			new float3(0.625f, 0.375f, 0.1f), 
			new float3(0.875f, 0.375f, 0.1f), 
			new float3(0.875f, 0.625f, 0.1f), 
			new float3(0.625f, 0.625f, 0.1f), 
			new float3(0.625f, 0.375f, 0.1f), 
			new float3(0.375f, 0.625f, 0.1f), 
			new float3(0.125f, 0.625f, 0.1f), 
			new float3(0.125f, 0.375f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0f, 0f, 0f), 
			new float3(0.125f, 0.375f, 0f), 
			new float3(0.375f, 0.375f, 0f), 
			new float3(0.375f, 0.625f, 0f), 
			new float3(0.625f, 0.375f, 0f), 
			new float3(0.875f, 0.375f, 0f), 
			new float3(0.875f, 0.625f, 0f), 
			new float3(0.625f, 0.625f, 0f), 
			new float3(0.625f, 0.375f, 0f), 
			new float3(0.375f, 0.625f, 0f), 
			new float3(0.125f, 0.625f, 0f), 
			new float3(0.125f, 0.375f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0.125f, 0.375f, 0.1f), 
			new float3(0.375f, 0.375f, 0.1f), 
			new float3(0.375f, 0.625f, 0.1f), 
			new float3(0.625f, 0.375f, 0.1f), 
			new float3(0.875f, 0.375f, 0.1f), 
			new float3(0.875f, 0.625f, 0.1f), 
			new float3(0.625f, 0.625f, 0.1f), 
			new float3(0.625f, 0.375f, 0.1f), 
			new float3(0.375f, 0.625f, 0.1f), 
			new float3(0.125f, 0.625f, 0.1f), 
			new float3(0.125f, 0.375f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f)
		};

		public static int[] _triangles =
		{
			1, 
			2, 
			0, 
			3, 
			4, 
			2, 
			2, 
			4, 
			0, 
			4, 
			5, 
			0, 
			8, 
			9, 
			7, 
			11, 
			12, 
			10, 
			12, 
			13, 
			10, 
			10, 
			13, 
			9, 
			9, 
			13, 
			7, 
			7, 
			13, 
			6, 
			13, 
			14, 
			6, 
			6, 
			14, 
			5, 
			14, 
			15, 
			5, 
			5, 
			15, 
			0, 
			17, 
			16, 
			18, 
			19, 
			18, 
			20, 
			18, 
			16, 
			20, 
			20, 
			16, 
			21, 
			24, 
			23, 
			25, 
			27, 
			26, 
			28, 
			28, 
			26, 
			29, 
			26, 
			25, 
			29, 
			25, 
			23, 
			29, 
			23, 
			22, 
			29, 
			29, 
			22, 
			30, 
			22, 
			21, 
			30, 
			30, 
			21, 
			31, 
			21, 
			16, 
			31, 
			32, 
			49, 
			33, 
			32, 
			48, 
			49, 
			33, 
			50, 
			34, 
			33, 
			49, 
			50, 
			34, 
			51, 
			35, 
			34, 
			50, 
			51, 
			35, 
			52, 
			36, 
			35, 
			51, 
			52, 
			36, 
			53, 
			37, 
			36, 
			52, 
			53, 
			37, 
			54, 
			38, 
			37, 
			53, 
			54, 
			38, 
			55, 
			39, 
			38, 
			54, 
			55, 
			39, 
			56, 
			40, 
			39, 
			55, 
			56, 
			40, 
			57, 
			41, 
			40, 
			56, 
			57, 
			41, 
			58, 
			42, 
			41, 
			57, 
			58, 
			42, 
			59, 
			43, 
			42, 
			58, 
			59, 
			43, 
			60, 
			44, 
			43, 
			59, 
			60, 
			44, 
			61, 
			45, 
			44, 
			60, 
			61, 
			45, 
			62, 
			46, 
			45, 
			61, 
			62, 
			46, 
			63, 
			47, 
			46, 
			62, 
			63, 
			47, 
			48, 
			32, 
			47, 
			63, 
			48
		};
	}
}
