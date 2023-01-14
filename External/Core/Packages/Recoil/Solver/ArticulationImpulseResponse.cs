using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public unsafe partial struct Articulation
    {
        //public static float3x3 ComputeImpulseResponseReference(in Articulation articulation, NativeArray<Body> worldBodies, in NativeArray<float4> worldV, int b, float3 anchor, ProfilerMarkers profiler)
        //{
        //    var baseline = ComputeImpulseResponseReference(articulation, worldBodies, worldV, b, anchor, new float3(0, 0, 0), profiler);
        //    return math.transpose(new float3x3(
        //        ComputeImpulseResponseReference(articulation, worldBodies, worldV, b, anchor, new float3(1, 0, 0), profiler) - baseline,
        //        ComputeImpulseResponseReference(articulation, worldBodies, worldV, b, anchor, new float3(0, 1, 0), profiler) - baseline,
        //        ComputeImpulseResponseReference(articulation, worldBodies, worldV, b, anchor, new float3(0, 0, 1), profiler) - baseline));
        //}
        //public static float3 ComputeImpulseResponseReference(in Articulation articulation, NativeArray<Body> worldBodies, in NativeArray<float4> worldV, int b, float3 anchor, float3 impulse, ProfilerMarkers profiler)
        //{

        //    using (var v = articulation.ExtractWorldVelocityCopy(worldV))
        //    using (var context = articulation.GetContext(worldBodies, v))
        //    {
        //        var vOriginalCG = context.GetVelocity(b);
        //        ref var body = ref context.GetBody(b);
        //        // apply impulse to handle body
        //        var offset = math.rotate(body.x.rot, anchor);

        //        articulation.ApplyImpulse(context, b, anchor, impulse, false);


        //        // calculate deltaV at secific body
        //        var vFinalCG = context.GetVelocity(b);
        //        var deltaVCG = vFinalCG - vOriginalCG;
        //        return deltaVCG.TranslateBy(offset).linear;
        //    }
        //}
        #region Linear impulse response at anchor

        public static float3x3 ComputeImpulseResponseFast(in Articulation articulation, int b, float3 anchor)
        {
            var bodies = articulation.GetBodies();
            return math.transpose(new float3x3(
                ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, anchor, articulation.solver.jacobian, articulation.solver.sparseLDL, new float3(1, 0, 0)),
                ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, anchor, articulation.solver.jacobian, articulation.solver.sparseLDL, new float3(0, 1, 0)),
                ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, anchor, articulation.solver.jacobian, articulation.solver.sparseLDL, new float3(0, 0, 1))));
        }
        public static float3 ComputeImpulseResponseFast(in Articulation articulation, int b, float3 anchor, float3 impulse, ProfilerMarkers profiler)
        {
            var bodies = articulation.GetBodies();
            return ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, anchor, articulation.solver.jacobian, articulation.solver.sparseLDL, impulse);
        }
        public static float3 ComputeImpulseResponseFast<T>(in T bodies, int nBlocks, int nBodyBlocks, int b, float3 anchor, in ArticulationJacobian jacobian, in SparseLDL sparseLDL, float3 impulse) where T : IGetBody
        {
            //var vOriginalCG = MotionVector.zero;
            ref var body = ref bodies.GetBody(b);

            // apply impulse to handle body
            var offset = math.rotate(body.x.rot, anchor);
            var impulseCG = ForceVector.Linear(impulse).TranslateBy(-offset);
            var dVCG = new MotionVector(re.mul(body.invI, impulseCG.angular), body.invM * impulseCG.linear);
            var dVangular = new float4(dVCG.angular, 0);
            var dVlinear = new float4(dVCG.linear, 0);
            //            var vModifiedCG = vOriginalCG + dV;

            var profiler = ProfilerMarkers.instance;
            profiler.profileBuildB.Begin();

            // unwrap AddJv
            var x = new NativeArray<float4>(nBlocks, Allocator.Temp, NativeArrayOptions.ClearMemory);

            for (int r = 0; r < nBlocks; r++)
            {
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 0])
                    x.ItemAsRef(r) += math.mul(jacobian.J[r * nBodyBlocks + b * 2 + 0], dVangular);
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 1])
                    x.ItemAsRef(r) += math.mul(jacobian.J[r * nBodyBlocks + b * 2 + 1], dVlinear);
            }
            profiler.profileBuildB.End();

            profiler.profileSparseSolve.Begin();
            SparseLDL.MulInvDecomposed(sparseLDL, x);
            profiler.profileSparseSolve.End();


            profiler.profileApplyImpulse.Begin();
            //unwrap apply delta impulse
            for (int r = 0; r < nBlocks; r++)
            {
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 0])
                    dVangular -= math.mul(jacobian.WJT[(b * 2 + 0) * nBlocks + r], x[r]);
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 1])
                    dVlinear -= math.mul(jacobian.WJT[(b * 2 + 1) * nBlocks + r], x[r]);
            }
            profiler.profileApplyImpulse.End();
            x.Dispose();

            dVCG = new MotionVector(dVangular.xyz, dVlinear.xyz);
            return dVCG.TranslateBy(offset).linear;
        }
        #endregion

        #region Full 6DOF articulated inertia
        public static ArticulatedBodyInertia ComputeImpulseResponseFast(in Articulation articulation, int b)
        {
            var bodies = articulation.GetBodies();
            var baseline = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.zero);
            var aX = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Angular(1, 0, 0)) - baseline;
            var aY = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Angular(0, 1, 0)) - baseline;
            var aZ = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Angular(0, 0, 1)) - baseline;
            var lX = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Linear(1, 0, 0)) - baseline;
            var lY = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Linear(0, 1, 0)) - baseline;
            var lZ = ComputeImpulseResponseFast(bodies, articulation.solver.nBlocks, articulation.solver.nBodyBlocks, b, articulation.solver.jacobian, articulation.solver.sparseLDL, ForceVector.Linear(0, 0, 1)) - baseline;

            return new ArticulatedBodyInertia(
                new lt3x3(lX.linear.x, lY.linear.x, lY.linear.y, lZ.linear.x, lZ.linear.y, lZ.linear.z),
                new float3x3(
                    aX.linear.x, aX.linear.y, aX.linear.z,
                    aY.linear.x, aY.linear.y, aY.linear.z,
                    aZ.linear.x, aZ.linear.y, aZ.linear.z),
                new lt3x3(aX.angular.x, aY.angular.x, aY.angular.y, aZ.angular.x, aZ.angular.y, aZ.angular.z));
        }

        public static MotionVector ComputeImpulseResponseFast<T>(in T bodies, int nBlocks, int nBodyBlocks, int b, in ArticulationJacobian jacobian, in SparseLDL sparseLDL, ForceVector impulse) where T : IGetBody
        {
            ref var body = ref bodies.GetBody(b);

            var dV = new MotionVector(re.mul(body.invI, impulse.angular), body.invM * impulse.linear);
            var dVangular = new float4(dV.angular, 0);
            var dVlinear = new float4(dV.linear, 0);
            // unwrap AddJv
            var x = new NativeArray<float4>(nBlocks, Allocator.Temp, NativeArrayOptions.ClearMemory);

            //profiler.profileBuildB.Begin();
            for (int r = 0; r < nBlocks; r++)
            {
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 0])
                    x.ItemAsRef(r) += math.mul(jacobian.J[r * nBodyBlocks + b * 2 + 0], dVangular);
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 1])
                    x.ItemAsRef(r) += math.mul(jacobian.J[r * nBodyBlocks + b * 2 + 1], dVlinear);
            }
            //profiler.profileBuildB.End();

            //profiler.profileSparseSolve.Begin();
            SparseLDL.MulInvDecomposed(sparseLDL, x);
            //profiler.profileSparseSolve.End();


            //profiler.profileApplyImpulse.Begin();
            //unwrap apply delta impulse
            for (int r = 0; r < nBlocks; r++)
            {
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 0])
                    dVangular -= math.mul(jacobian.WJT[(b * 2 + 0) * nBlocks + r], x[r]);
                if (jacobian.Jsparsity[r * nBodyBlocks + b * 2 + 1])
                    dVlinear -= math.mul(jacobian.WJT[(b * 2 + 1) * nBlocks + r], x[r]);
            }
            //profiler.profileApplyImpulse.End();
            x.Dispose();

            dV = new MotionVector(dVangular.xyz, dVlinear.xyz);
            return dV;
        }
        #endregion

    }
}
