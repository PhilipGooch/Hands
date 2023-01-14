using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public struct ArticulationReaderLink
    {
        public bool isConnected;
        public int link;
        public int parent;
        public float3 anchor;
        public float3 connectedAnchor;
        public quaternion connectedRotation;
        public float mass;

        public ArticulationReaderLink(int link, int parent, float mass)
        {
            this.isConnected = true;
            this.parent = parent;
            this.link = link;
            this.connectedRotation = quaternion.identity;
            anchor = connectedAnchor = float3.zero;
            this.mass = mass;
        }

        public ArticulationJoint CreateAngularJoint(RotationTargetMode rotationMode= RotationTargetMode.SelfOffset)
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.Angular,
                angular = new AngularArticulationJoint
                {
                    link = link,
                    connectedLink = parent,
                    connectedRotation = connectedRotation,
                    targetRotation = quaternion.identity,
                    rotationMode =rotationMode
                }
            };
        }

        public ArticulationJoint CreateHingeJoint(float3 axisX, float3 axisY, float3 axisZ, RotationTargetMode rotationMode = RotationTargetMode.SelfOffset)
        {
            var joint = CreateAngularJoint(rotationMode);
            joint.jointType = ArticulationJointType.Angular3;
            joint.angular3.axisX = axisX;
            joint.angular3.axisY = axisY;
            joint.angular3.axisZ = axisZ;
            joint.angular3.springY = Spring.stiff;
            joint.angular3.springZ = Spring.stiff;
            return joint;
        }

        public ArticulationJoint CreateLinearJoint()
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.Linear,
                linear = new LinearArticulationJoint
                {
                    link = link,
                    connectedLink = parent,
                    anchor = anchor,
                    connectedAnchor = connectedAnchor,
                    spring = Spring.stiff
                }
            };
        }
    }

    public static class ArticulationReader
    {
        public static List<ArticulationReaderLink> ReadStructure(Rigidbody[] chain)
        {
            var links = new List<ArticulationReaderLink>();
            
            for (var i = 0; i < chain.Length; i++)
            {
                // read rigidbody data
                var body = chain[i];
                body.maxAngularVelocity = 100;
                var joint = body.GetComponent<ConfigurableJoint>();

                if (joint == null)
                    links.Add(new ArticulationReaderLink() { link = i, isConnected = false, mass = body.mass });
                if (joint != null)
                {
                    //joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
                    var link = new ArticulationReaderLink()
                    {
                        isConnected=true,
                        link = i,
                        parent = -1,
                        connectedAnchor = (float3)joint.connectedAnchor,
                        connectedRotation = (quaternion)body.rotation,
                        anchor = (float3)(joint.anchor - body.centerOfMass),
                        mass = body.mass,
                    };
                    // xBody.pos += math.rotate(xBody.rot, anchor);
                    if (joint.connectedBody != null)
                    {
                        link.connectedRotation = math.normalize(re.invmul(joint.connectedBody.rotation, link.connectedRotation));
                        link.connectedAnchor -= (float3)joint.connectedBody.centerOfMass;
                        for (int j = 0; j < i; j++)
                            if (joint.connectedBody == chain[j])
                                link.parent = j; // store parent index
                    }
                    links.Add(link);

                }
            }
            return links;
        }

        public static ArticulationJoint[] CreateArticulationJoints(List<ArticulationReaderLink> structure)
        {
            var joints = new List<ArticulationJoint>();

            foreach (var link in structure)
            {
                if (link.isConnected)
                {
                    joints.Add(link.CreateLinearJoint());
                    joints.Add(link.CreateAngularJoint());
                }
            }
            return joints.ToArray();
        }


    }
}
