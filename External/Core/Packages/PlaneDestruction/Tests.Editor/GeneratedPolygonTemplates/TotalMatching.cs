using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class TotalMatching
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
				new float3(0f, 0f, 0f), 
				new float3(0f, 1f, 0f), 
				new float3(1f, 1f, 0f), 
				new float3(1f, 0f, 0f)
			}
		};

		public static float3[] _vertices =
		{
			new float3(0f, 0f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 0f, 0f), 
			new float3(0f, 1f, 0f), 
			new float3(1f, 1f, 0f), 
			new float3(1f, 0f, 0f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(1f, 0f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 0f, 0.1f), 
			new float3(0f, 1f, 0.1f), 
			new float3(1f, 1f, 0.1f), 
			new float3(1f, 0f, 0.1f)
		};

		public static int[] _triangles =
		{
			7, 
			8, 
			6, 
			8, 
			9, 
			6, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			0, 
			17, 
			16, 
			18, 
			18, 
			16, 
			19, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			10, 
			20, 
			31, 
			21, 
			20, 
			30, 
			31, 
			21, 
			32, 
			22, 
			21, 
			31, 
			32, 
			22, 
			33, 
			23, 
			22, 
			32, 
			33, 
			23, 
			34, 
			24, 
			23, 
			33, 
			34, 
			24, 
			35, 
			25, 
			24, 
			34, 
			35, 
			25, 
			36, 
			26, 
			25, 
			35, 
			36, 
			26, 
			37, 
			27, 
			26, 
			36, 
			37, 
			27, 
			38, 
			28, 
			27, 
			37, 
			38, 
			28, 
			39, 
			29, 
			28, 
			38, 
			39, 
			29, 
			30, 
			20, 
			29, 
			39, 
			30
		};
	}
}