using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using Unity.Mathematics;


namespace NBG.XPBDRope
{
    public class RopeArticulation
    {
        Dictionary<List<Rope>, int> articulationIds = new Dictionary<List<Rope>, int>();
        List<List<Rope>> articulationChains = new List<List<Rope>>();

        List<ArticulationJoint> tempJoints = new List<ArticulationJoint>();
        Dictionary<Rigidbody, int> bodyIds = new Dictionary<Rigidbody, int>();
        List<Rigidbody> bodies = new List<Rigidbody>();

        public void RebuildAllArticulations(List<Rope> allRopes)
        {
            Dispose();

            foreach(var rope in allRopes)
            {
                const int unconnected = -1;
                int chainId = unconnected;
                for(int i = articulationChains.Count - 1; i >= 0; i--)
                {
                    var chain = articulationChains[i];
                    for(int x = 0; x < chain.Count; x++)
                    {
                        var chainRope = chain[x];
                        if (RopesAreConnected(rope, chainRope))
                        {
                            if (chainId == unconnected)
                            {
                                chain.Add(rope);
                                chainId = i;
                            }
                            else // Already part of a chain - concatenate the two chains
                            {
                                articulationChains[chainId].AddRange(chain);
                                // TODO: Some form of list pooling would be nice
                                articulationChains.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }

                if (chainId == unconnected)
                {
                    articulationChains.Add(new List<Rope> { rope });
                }
            }

            foreach(var chain in articulationChains)
            {
                RebuildArticulation(chain);
            }
        }

        bool RopesAreConnected(Rope first, Rope second)
        {
            var hasFirstBody = first.BodyStartIsAttachedTo != null;
            var hasSecondBody = first.BodyEndIsAttachedTo != null;
            var firstBodyMatches = first.BodyStartIsAttachedTo == second.BodyStartIsAttachedTo ||
                                   first.BodyStartIsAttachedTo == second.BodyEndIsAttachedTo;
            var secondBodyMatches = first.BodyEndIsAttachedTo == second.BodyStartIsAttachedTo ||
                                    first.BodyEndIsAttachedTo == second.BodyEndIsAttachedTo;
            return hasFirstBody && firstBodyMatches || hasSecondBody && secondBodyMatches;
        }

        void RebuildArticulation(List<Rope> ropes)
        {
            tempJoints.Clear();
            bodies.Clear();
            bodyIds.Clear();

            foreach (var rope in ropes)
            {
                FormRopeConnections(rope);
                int firstSegmentId = bodies.Count - rope.ActiveBoneCount;
                int lastSegmentId = bodies.Count - 1;
                if (rope.BodyStartIsAttachedTo)
                {
                    FormRopeBodyConnection(rope.BodyStartIsAttachedTo, rope.StartBodyJoint, firstSegmentId);
                }
                if (rope.BodyEndIsAttachedTo)
                {
                    FormRopeBodyConnection(rope.BodyEndIsAttachedTo, rope.EndBodyJoint, lastSegmentId);
                }
            }

            // Don't allocate articulations without joints as they will throw errors. This can happen when a rope is reduced to one segment.
            if (tempJoints.Count > 0)
            {
                ClearArticulation(ropes);
                ref var articulation = ref GetArticulationStruct(ropes);
                articulation.Allocate(articulationIds[ropes], bodies.ToArray(), tempJoints.ToArray());
            }
            else
            {
                RemoveArticulation(ropes);
            }
        }

        void ClearArticulation(List<Rope> ropes)
        {
            if (articulationIds.ContainsKey(ropes))
            {
                var articulation = World.main.GetArticulation(articulationIds[ropes]);
                articulation.Dispose();
            }
        }

        void RemoveArticulation(List<Rope> ropes)
        {
            if (articulationIds.ContainsKey(ropes))
            {
                ManagedWorld.main.RemoveArticulation(articulationIds[ropes]);
                articulationIds.Remove(ropes);
            }
        }


        ref Articulation GetArticulationStruct(List<Rope> ropes)
        {
            if (!articulationIds.ContainsKey(ropes))
            {
                ManagedWorld.main.AddArticulation(out var id);
                articulationIds[ropes] = id;
            }

            return ref World.main.GetArticulation(articulationIds[ropes]);
        }

        void FormRopeConnections(Rope rope)
        {
            for (int i = 0; i < rope.BoneCount; i++)
            {
                var bone = rope.Bones[i];
                bodies.Add(bone.body);
                if (i >= rope.FirstActiveBone)
                {
                    int id = bodies.Count - 1;
                    if (bone.connectionToNextSegment != null)
                    {
                        var spring = Spring.stiff;
                        if (rope.LinearSpring > 0f)
                        {
                            spring = new Spring(rope.LinearSpring, rope.LinearDamper);
                        }
                        AddArticulationJoints(id, id + 1, bone.body, bone.connectionToNextSegment, spring);
                    }
                }
            }
        }

        void FormRopeBodyConnection(Rigidbody body, ConfigurableJoint joint, int segmentId)
        {
            var bodyId = -1;

            if (bodyIds.ContainsKey(body))
            {
                bodyId = bodyIds[body];
            }
            else
            {
                bodies.Add(body);
                bodyId = bodies.Count - 1;
                bodyIds[body] = bodyId;
            }

            AddArticulationJoints(bodyId, segmentId, body, joint, Spring.stiff);
        }

        void AddArticulationJoints(int currentId, int connectedId, Rigidbody currentBody, ConfigurableJoint joint, Spring spring)
        {
            var connectedAnchor = joint.connectedAnchor;
            if (joint.connectedBody != null)
            {
                connectedAnchor -= joint.connectedBody.centerOfMass;
            }

            tempJoints.Add(new ArticulationJoint
            {
                jointType = Recoil.ArticulationJointType.Linear,
                linear = new LinearArticulationJoint
                {
                    link = currentId,
                    connectedLink = connectedId,
                    anchor = joint.anchor - currentBody.centerOfMass,
                    connectedAnchor = connectedAnchor,
                    spring = spring,
                }
            });
        }

        public void RebuildArticulationForRope(Rope target)
        {
            foreach(var chain in articulationChains)
            {
                if (chain.Contains(target))
                {
                    RebuildArticulation(chain);
                    break;
                }
            }
        }

        public void Dispose()
        {
            foreach(var articulation in articulationIds)
            {
                ManagedWorld.main.RemoveArticulation(articulation.Value);
            }

            articulationIds.Clear();
            articulationChains.Clear();
        }
    }
}
