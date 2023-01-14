#define DEBUGGING
using Unity.Jobs;

namespace NBG.Water
{
    public interface IFloatingMeshBackend
    {
        unsafe JobHandle ScheduleJobs(JobHandle dependsOn);
        void ApplyForces();
        void DrawDebugGizmos();
        void Dispose();

        float CalculatedMass { get; }
        float CalculatedVolume { get; }
        float CalculatedBuoyancyMultiplier { get; }
        int OriginalVertexCount { get; }
        int OptimizedVertexCount { get; }
        int TriangleCount { get; }
    }
}
