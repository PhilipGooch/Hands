using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public partial struct Solver
    {

        public unsafe void CalculateBiasDeltaV(MotionVector g)
        {
            var dt = World.main.dt;
            var bodies = GetBodies();

            // for now
            var a = new NativeArray<MotionVector>(nLinks, Allocator.Temp);
            var v = new NativeArray<MotionVector>(nLinks, Allocator.Temp);
            var world = World.main;
            for (int i = 0; i < nLinks; i++)
                v[i] = world.GetVelocity(links[i]);

            for (int i = 0; i < nJoints; i++)
            {
                //ref var j = ref joints[i * 2 + 1];
                //if (j.jointType != SolverJointType.Angular) continue;
                //ref var joint = ref j.angular;
                if (joints.GetType(i) != ArticulationJointType.Linear) continue;
                ref var joint = ref joints.GetJoint<LinearArticulationJoint>(i);
                var b = joint.link;
                var p = joint.connectedLink;

                if (p != -1)
                {
                    ref var body = ref bodies.GetBody(b);
                    ref var bodyP = ref bodies.GetBody(p);
                    var rFromCG = math.rotate(body.x.rot, joint.anchor);
                    var rFromParent = math.rotate(bodyP.x.rot, joint.connectedAnchor) - rFromCG;
                    a[b] = a[p].TranslateBy(rFromParent) + v[b].justAngular.cross(v[b] - v[p].TranslateBy(rFromParent));
                }
                else
                    a[b] = -g;


                //var unityBias = ForceVector.Angular(math.cross(v[i].angular, re.mul(node.I, v[i].angular)));

                //var rbi = new RigidBodyInertia(node.m, float3.zero, node.I);
                //var biasForce = v[i].cross(re.mul(rbi, v[i])) - unityBias;
                //f[i] = re.mul(rbi, a[i]) + biasForce;
                // unwrap
                //// term that is skipped by unity integration
                //var biasForce = v[i].cross(new ForceVector(re.mul(node.I, v[i].angular), node.m * v[i].linear)) - unityBias;
                //f[i] = new ForceVector(re.mul(node.I, a[i].angular), node.m * a[i].linear) + biasForce;
            }
            //for (int i = nBodies - 1; i >= 1; i--)
            //{

            //    ref var joint = ref joints[i * 2 + 1].angular;
            //    var p = joint.bodyA;
            //    ref var node = ref nodes.GetNode(i);
            //    ref var nodeP = ref nodes.GetNode(p);
            //    var rFromCG = math.rotate(node.x.rot, joint.anchorB);
            //    bias[i] = f[i].TranslateBy(rFromCG).angular;

            //    var rFromParent = math.rotate(nodeP.x.rot, joint.anchorA) - math.rotate(node.x.rot, joint.anchorB);
            //    f[p] += f[i].TranslateBy(-rFromParent);
            //}

            for (int i = 0; i < nLinks; i++)
                biasDV[i] =new Velocity4( a[i] * dt);
            a.Dispose();
            v.Dispose();
        }
    }
}
