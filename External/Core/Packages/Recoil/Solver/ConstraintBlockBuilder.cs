using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public class ConstraintBlockBuilder
    {
        public List<ArticulationJoint> joints = new List<ArticulationJoint>();
        //public List<Rigidbody> bodies = new List<Rigidbody>();
        public List<int> links = new List<int>();

        public ConstraintBlockBuilder()
        {
        }

       
        private int FindOrAddLink(int body)
        {
            if (body < 0) return -1;
            var idx = links.IndexOf(body);
            if (idx < 0)
            {
                idx = links.Count;
                links.Add(body);
            }
            return idx;
        }

        public ArticulationJoint GetJoint(int id)
        {
            return joints[id];
        }

    

        public void AddAngularJoint(int link, int connectedLink, quaternion connectedRotation)
        {
            joints.Add(new ArticulationJoint()
            {
                jointType = ArticulationJointType.Angular,
                angular = new AngularArticulationJoint()
                {
                    link = FindOrAddLink(link),
                    connectedLink = FindOrAddLink(connectedLink),
                    connectedRotation = connectedRotation,
                    targetRotation = quaternion.identity,
                }
            });
        }


        public void AddAngularJoint(Rigidbody rigid, Rigidbody connectedRigid, quaternion connectedRotation)
        {
            var body = ManagedWorld.main.FindBody(rigid);
            var connectedBody = ManagedWorld.main.FindBody(connectedRigid);
            AddAngularJoint(body, connectedBody, connectedRotation);
        }

        public void AddLinearJoint(int body, float3 anchor, int connectedBody, float3 connectedAnchor)
        {
            joints.Add(new ArticulationJoint()
            {
                jointType = ArticulationJointType.Linear,
                linear = new LinearArticulationJoint()
                {
                    link = FindOrAddLink(body),
                    connectedLink = FindOrAddLink(connectedBody),
                    anchor = anchor,
                    connectedAnchor = connectedAnchor,
                    spring = Spring.stiff
                }
            });
        }
        public void AddLinearJoint(Rigidbody rigid, float3 transformAnchor, Rigidbody connectedRigid, float3 connectedTransformAnchor)
        {
            var body = ManagedWorld.main.FindBody(rigid);
            var connectedBody = ManagedWorld.main.FindBody(connectedRigid);
            AddLinearJoint(body, rigid == null ? transformAnchor : transformAnchor - (float3)rigid.centerOfMass,
                    connectedBody, connectedRigid == null ? connectedTransformAnchor : connectedTransformAnchor - (float3)connectedRigid.centerOfMass);
        }

        public void ReAddLinearJoint(in Solver otherSolver, LinearArticulationJoint linear)
        {
            linear.connectedLink = FindOrAddLink(otherSolver.GetBodyId(linear.connectedLink));
            linear.link = FindOrAddLink(otherSolver.GetBodyId(linear.link));
            joints.Add(new ArticulationJoint() { 
                 jointType = ArticulationJointType.Linear,
                 linear = linear
            });
        }

        public void ReAddAngularJoint(Solver otherSolver, AngularArticulationJoint angular)
        {
            angular.connectedLink = FindOrAddLink(otherSolver.GetBodyId(angular.connectedLink));
            angular.link = FindOrAddLink(otherSolver.GetBodyId(angular.link));
            joints.Add(new ArticulationJoint()
            {
                jointType = ArticulationJointType.Angular,
                angular = angular
            });
        }

        public ref ConstraintBlock BuildConstraint(out int constraintId)
        {
            ref var constraint = ref ManagedWorld.main.AddConstraint(out constraintId);
            constraint.Allocate(links.ToArray(), joints.ToArray());
            return ref constraint;
        }

        public unsafe static void ReleaseConstraintSolver(int constraintId)
        {
            ref var constraint = ref World.main.GetConstraint(constraintId);
            ManagedWorld.main.RemoveConstraint(constraintId);

        }
    }
}
