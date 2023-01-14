using Unity.Mathematics;

namespace NBG.PlaneDestructionSystem
{
    public interface IPlaneDestructionHandler
    {
        void OnNewProceduralPiece(ProceduralPiece piece);
        void OnDestroyProceduralPiece(ProceduralPiece piece);
        void SetVelocity(ProceduralPiece piece, float3 velocity, float3 angularVelocity);
    }
}
