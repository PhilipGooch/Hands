using NBG.MeshGeneration;
using Unity.Mathematics;
namespace NBG.PlaneDestructionSystem.Codegen
{
	internal static class PieceDisappearingWithHole
	{
		public static Polygon2D polygon => new Polygon2D(outline, holes);
		public static float3[] vertices => _vertices;
		public static int[] triangles => _triangles;

		public static float3[] outline  =
		{
			new float3(1f, 0.4f, 0f), 
			new float3(0.8f, 0.4f, 0f), 
			new float3(0.8f, 0.6f, 0f), 
			new float3(1f, 0.6f, 0f)
				};

		public static float3[][] holes =
		{
		};

		public static float3[] _vertices =
		{
			new float3(1f, 0.4f, 0f), 
			new float3(0.8f, 0.4f, 0f), 
			new float3(0.8f, 0.6f, 0f), 
			new float3(1f, 0.6f, 0f), 
			new float3(1f, 0.4f, 0.1f), 
			new float3(0.8f, 0.4f, 0.1f), 
			new float3(0.8f, 0.6f, 0.1f), 
			new float3(1f, 0.6f, 0.1f), 
			new float3(1f, 0.4f, 0f), 
			new float3(0.8f, 0.4f, 0f), 
			new float3(0.8f, 0.6f, 0f), 
			new float3(1f, 0.6f, 0f), 
			new float3(1f, 0.4f, 0.1f), 
			new float3(0.8f, 0.4f, 0.1f), 
			new float3(0.8f, 0.6f, 0.1f), 
			new float3(1f, 0.6f, 0.1f)
		};

		public static int[] _triangles =
		{
			1, 
			2, 
			0, 
			2, 
			3, 
			0, 
			5, 
			4, 
			6, 
			6, 
			4, 
			7, 
			8, 
			13, 
			9, 
			8, 
			12, 
			13, 
			9, 
			14, 
			10, 
			9, 
			13, 
			14, 
			10, 
			15, 
			11, 
			10, 
			14, 
			15, 
			11, 
			12, 
			8, 
			11, 
			15, 
			12
		};
	}
}
