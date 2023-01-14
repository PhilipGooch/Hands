using NBG.Entities;
using Noodles;
using Recoil;
using System;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Burst;
using Unity.Mathematics;


namespace Noodles
{
    public unsafe struct NoodleArmJoints
    {
        // pointers
        Angular3ArticulationJoint* _upper;
        Angular3ArticulationJoint* _lower;
        LinearArticulationJoint* _upperLinear;
        LinearArticulationJoint* _lowerLinear;
        FulcrumJoint* _IK;

        // public API - refs
        public ref Angular3ArticulationJoint upper => ref *_upper;
        public ref Angular3ArticulationJoint lower => ref *_lower;
        public ref LinearArticulationJoint upperLinear => ref *_upperLinear;
        public ref LinearArticulationJoint lowerLinear => ref *_lowerLinear;
        public ref FulcrumJoint IK => ref *_IK;
        public int ikID;

        public NoodleArmJoints(ArticulationJointArray joints, int chest, int upper, int lower, int ik)
        {
            ikID = ik;
            _IK = joints.GetJoint<FulcrumJoint>(ik).AsPointer();
            _upper = joints.GetJoint<Angular3ArticulationJoint>(upper ).AsPointer();
            _lower = joints.GetJoint<Angular3ArticulationJoint>(lower ).AsPointer();
            _upperLinear = joints.GetJoint<LinearArticulationJoint>(upper+1).AsPointer();
            _lowerLinear = joints.GetJoint<LinearArticulationJoint>(lower+1).AsPointer();

        }
    }

  
    public unsafe struct NoodleLegJoints
    {
        // pointers
        AngularArticulationJoint* _upper;
        Angular3ArticulationJoint* _lower;
        FulcrumJoint* _IK;
        public ref FulcrumJoint IK => ref *_IK;
        //LinearArticulationJoint* _upperLinear;
        //LinearArticulationJoint* _lowerLinear;

        // public API - refs
        public ref AngularArticulationJoint upper => ref *_upper;
        public ref Angular3ArticulationJoint lower => ref *_lower;
        //public ref LinearArticulationJoint upperLinear => ref *_upperLinear;
        //public ref LinearArticulationJoint lowerLinear => ref *_lowerLinear;



        public NoodleLegJoints(ArticulationJointArray joints, int upper, int lower, int ik)
        {
            _IK = joints.GetJoint<FulcrumJoint>(ik).AsPointer();
            _upper = joints.GetJoint<AngularArticulationJoint>(upper ).AsPointer();
            _lower = joints.GetJoint<Angular3ArticulationJoint>(lower).AsPointer();


        }
    }
 
    public unsafe struct NoodleTorsoJoints
    {
        //AngularArticulationJoint* _hips;
        Angular3ArticulationJoint* _angularHips;
        Angular3ArticulationJoint* _angularChest;
        AngularArticulationJoint* _waist;
        AngularArticulationJoint* _chest;
        AngularArticulationJoint* _head;
        public ref Angular3ArticulationJoint angularHips => ref *_angularHips;
        public ref Angular3ArticulationJoint angularChest => ref *_angularChest;
        public ref AngularArticulationJoint waist => ref *_waist;
        public ref AngularArticulationJoint chest => ref *_chest;
        public ref AngularArticulationJoint head => ref *_head;



        public NoodleTorsoJoints(ArticulationJointArray joints, int angular, int angularchest, int waist, int chest, int head)
        {
            _angularChest = joints.GetJoint<Angular3ArticulationJoint>(angularchest).AsPointer();
            _angularHips = joints.GetJoint<Angular3ArticulationJoint>(angular).AsPointer();
            _waist = joints.GetJoint<AngularArticulationJoint>(waist).AsPointer();
            _chest = joints.GetJoint<AngularArticulationJoint>(chest).AsPointer();
            _head = joints.GetJoint<AngularArticulationJoint>(head).AsPointer();


        }

    }

   
    public unsafe struct NoodleJoints
    {
        public ArticulationJointArray joints;
        public NoodleJoints(in ArticulationJointArray joints) { this.joints = joints; }

        // Torso
        //public ref AngularArticulationJoint angularSpring => ref joints.GetJoint<AngularArticulationJoint>(IDX_Angular);
        //public ref AngularArticulationJoint waist => ref joints.GetJoint<AngularArticulationJoint>(IDX_Waist );
        //public ref AngularArticulationJoint chest => ref joints.GetJoint<AngularArticulationJoint>(IDX_Chest );
        //public ref AngularArticulationJoint head => ref joints.GetJoint<AngularArticulationJoint>(IDX_Head );
        public NoodleTorsoJoints torso => new NoodleTorsoJoints(joints, IDX_AngularHips, IDX_AngularChest, IDX_Waist, IDX_Chest, IDX_Head);

        // Limbs
        public NoodleArmJoints armL => new NoodleArmJoints(joints, IDX_Chest, IDX_UpperArmL, IDX_LowerArmL, IDX_IK+0);
        public NoodleArmJoints armR => new NoodleArmJoints(joints, IDX_Chest, IDX_UpperArmR, IDX_LowerArmR, IDX_IK+1);
        public NoodleLegJoints legL => new NoodleLegJoints(joints, IDX_UpperLegL, IDX_LowerLegL, IDX_IK + 2);
        public NoodleLegJoints legR => new NoodleLegJoints(joints, IDX_UpperLegR, IDX_LowerLegR, IDX_IK + 3);
    

        // Special
        public ref CGArticulationJoint cg => ref joints.GetJoint<CGArticulationJoint>(IDX_CG);
        //public ref Linear3ArticulationJoint hips => ref joints.GetJoint<Linear3ArticulationJoint>(IDX_CG);

        public ref PreserveAngularArticulationJoint preserveAngular => ref joints.GetJoint<PreserveAngularArticulationJoint>(IDX_PreserveAngular);

        
        private const int IDX_AngularHips = 0;
        private const int IDX_AngularChest = IDX_AngularHips + 1;
        private const int linkJointsOffset = NoodleBones.Waist * 2 - (IDX_AngularChest + 1);
        private const int IDX_Waist = NoodleBones.Waist * 2 - linkJointsOffset;
        private const int IDX_Chest = NoodleBones.Chest * 2 - linkJointsOffset;
        private const int IDX_Head = NoodleBones.Head * 2 - linkJointsOffset;
        public const int IDX_UpperArmL = NoodleBones.UpperArmL * 2 - linkJointsOffset;
        public const int IDX_LowerArmL = NoodleBones.LowerArmL * 2 - linkJointsOffset;
        public const int IDX_UpperArmR = NoodleBones.UpperArmR * 2 - linkJointsOffset;
        private const int IDX_LowerArmR = NoodleBones.LowerArmR * 2 - linkJointsOffset;
        private const int IDX_UpperLegL = NoodleBones.UpperLegL * 2 - linkJointsOffset;
        private const int IDX_UpperLegR = NoodleBones.UpperLegR * 2 - linkJointsOffset;
        private const int IDX_LowerLegL = NoodleBones.LowerLegL * 2 - linkJointsOffset;
        private const int IDX_LowerLegR = NoodleBones.LowerLegR * 2 - linkJointsOffset;
        private const int IDX_CG = IDX_LowerLegR + 2;
        private const int IDX_PreserveAngular = IDX_CG + 1;
        public const int IDX_IK = IDX_PreserveAngular + 1;
        


        public static ArticulationJoint[] CreateJoints(List<ArticulationReaderLink> structure, NoodleDimensions dimensions)
        {
            var joints = new List<ArticulationJoint>();

            //joints.Add(new ArticulationJoint());// empty 0
            //joints.Add(new ArticulationReaderLink(NoodleBones.Hips, -1).CreateAngularJoint(RotationTargetMode.Absolute));
            joints.Add(Angular3ArticulationJoint.Create(NoodleBones.Hips, -1, re.up, re.right, re.forward, RotationTargetMode.Absolute));
            joints.Add(Angular3ArticulationJoint.Create(NoodleBones.Chest, -1, re.up, re.right, re.forward, RotationTargetMode.Absolute));

            UnityEngine.Debug.Assert(joints.Count == IDX_Waist, "Incompatible joint structure");
            foreach (var link in structure)
            {
                if (!link.isConnected) continue;

                var angular = link.link switch
                {
                    NoodleBones.Head=> link.CreateAngularJoint(RotationTargetMode.AbsolutePosRelativeVel),
                    NoodleBones.UpperArmL => link.CreateHingeJoint(re.forward, re.up, re.right, RotationTargetMode.Absolute),
                    NoodleBones.UpperArmR => link.CreateHingeJoint(re.forward, re.up, re.right, RotationTargetMode.Absolute),
                    NoodleBones.UpperLegL => link.CreateAngularJoint(RotationTargetMode.Absolute),
                    NoodleBones.UpperLegR => link.CreateAngularJoint(RotationTargetMode.Absolute),
                    NoodleBones.LowerArmL => link.CreateHingeJoint(re.forward, re.up, re.right),
                    NoodleBones.LowerArmR => link.CreateHingeJoint(re.forward, re.up, re.right),
                    NoodleBones.LowerLegL => link.CreateHingeJoint(re.right, re.up, re.forward),
                    NoodleBones.LowerLegR => link.CreateHingeJoint(re.right, re.up, re.forward),
                    _ => link.CreateAngularJoint()
                };
               
                joints.Add(angular);
                joints.Add(link.CreateLinearJoint());
            }

            UnityEngine.Debug.Assert(joints.Count == IDX_CG, "Incompatible joint structure");

            //if (justPrimaryJoints) return joints.ToArray();
            // CG above ball
            joints.Add(CGArticulationJoint.Create(NoodleBones.Hips, NoodleBones.Last, NoodleBones.Ball));

            //Preserve angular momentum
            UnityEngine.Debug.Assert(joints.Count == IDX_PreserveAngular, "Incompatible joint structure");
            joints.Add(PreserveAngularArticulationJoint.Create(NoodleBones.Hips, NoodleBones.Last));


            UnityEngine.Debug.Assert(joints.Count == IDX_IK, "Incompatible joint structure");
            var anchorChestL = structure[NoodleBones.UpperArmL].connectedAnchor + re.right * NoodleConstants.HAND_IK_ANCHOR_SHIFT_TO_CENTER;
            var anchorChestR = structure[NoodleBones.UpperArmR].connectedAnchor - re.right * NoodleConstants.HAND_IK_ANCHOR_SHIFT_TO_CENTER;
            var anchorHipsL = structure[NoodleBones.UpperLegL].connectedAnchor *0; // just use center of hips
            var anchorHipsR = structure[NoodleBones.UpperLegR].connectedAnchor *0; // just use center of hips

            var ikL = FulcrumJoint.Create(NoodleBones.LowerArmL, dimensions.armL.handAnchor, NoodleBones.Ball, float3.zero, NoodleBones.Chest, anchorChestL);
            var ikR = FulcrumJoint.Create(NoodleBones.LowerArmR, dimensions.armR.handAnchor, NoodleBones.Ball, float3.zero, NoodleBones.Chest, anchorChestR);

            var legIkL = FulcrumJoint.Create(NoodleBones.LowerLegL, dimensions.legL.footAnchor, NoodleBones.Ball, float3.zero, NoodleBones.Hips, anchorHipsL);
            var legIkR = FulcrumJoint.Create(NoodleBones.LowerLegR, dimensions.legR.footAnchor, NoodleBones.Ball, float3.zero, NoodleBones.Hips, anchorHipsR);

            ikL.fulcrum.weights = new float4(0, 1, 0, 0);
            ikR.fulcrum.weights = new float4(0, 1, 0, 0);
            legIkL.fulcrum.weights = new float4(1, 0, 0, 0);
            legIkR.fulcrum.weights = new float4(1, 0, 0, 0);

            joints.Add(ikL);
            joints.Add(ikR);
            joints.Add(legIkL);
            joints.Add(legIkR);
            return joints.ToArray();
        }

        public static NoodleDimensions Measure(List<ArticulationReaderLink> structure, in NoodleSuspensionData suspension, float ballRadius, float3 handAnchorL, float3 handAnchorR, float3 footAnchorL, float3 footAnchorR)
        {

            var waistLinear = structure[NoodleBones.Waist];
            var chestLinear = structure[NoodleBones.Chest];
            var headLinear = structure[NoodleBones.Head]; 
            //var bodies = articulation.GetBodies();
            var dim = new NoodleDimensions()
            {
                ballRadius = ballRadius,
                hipsAnchor = suspension.anchor,
                waistAnchor = waistLinear.anchor,
                chestAnchor = chestLinear.anchor,
                headAnchor = headLinear.anchor,

                hipsToWaist = waistLinear.connectedAnchor - suspension.anchor,// - spring.ballAnchor;//would need to remove NoodleSpring.anchorB if not zero
                waistToChest = chestLinear.connectedAnchor - waistLinear.anchor,
                chestToHead = headLinear.connectedAnchor - chestLinear.anchor,

                armL = new NoodleArmDimensions(structure, NoodleBones.Chest, NoodleBones.UpperArmL, NoodleBones.LowerArmL, handAnchorL),
                armR = new NoodleArmDimensions(structure, NoodleBones.Chest, NoodleBones.UpperArmR, NoodleBones.LowerArmR, handAnchorR),
                legL = new NoodleLegDimensions(structure, suspension.anchor, NoodleBones.UpperLegL, NoodleBones.LowerLegL, footAnchorL),
                legR = new NoodleLegDimensions(structure, suspension.anchor, NoodleBones.UpperLegR, NoodleBones.LowerLegR, footAnchorR),

                massHips = structure[NoodleBones.Hips].mass,
                massWaist = structure[NoodleBones.Waist].mass,
                massChest = structure[NoodleBones.Chest].mass,
                massHead = structure[NoodleBones.Head].mass,
                massUpperArm = structure[NoodleBones.UpperArmL].mass,
                massLowerArm = structure[NoodleBones.LowerArmL].mass,
                massUpperLeg = structure[NoodleBones.UpperLegL].mass,
                massLowerLeg = structure[NoodleBones.LowerLegL].mass,


            };
            dim.suspendedMass = dim.massHips + dim.massWaist + dim.massChest + dim.massHead +
                2 * (dim.massUpperArm + dim.massLowerArm + dim.massUpperLeg + dim.massLowerLeg);
            dim.totalMass = dim.suspendedMass + structure[NoodleBones.Ball].mass;
            return dim;

        }

    }
  
}