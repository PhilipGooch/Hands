using NBG.Core;
using NBG.Entities;
using Recoil;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class NoodleSkin : MonoBehaviour
    {
        [ClearOnReload(newInstance: true)]
        internal static readonly List<NoodleSkin> s_Skins = new List<NoodleSkin>();

        [ReadOnlyInPlayModeField, SerializeField]
        NoodleRig rig;

        [ReadOnlyInPlayModeField, SerializeField]
        Transform[] transforms;

        public bool IsAlive => rig.IsAlive;
        
        private void Awake()
        {
            Debug.Assert(!s_Skins.Contains(this));
            s_Skins.Add(this);
        }

        private void OnDestroy()
        {
            Debug.Assert(s_Skins.Contains(this));
            s_Skins.Remove(this);
        }

        internal unsafe void BindRig()
        {
            Debug.Assert(rig.IsAlive);

            var dt = Time.time - Time.fixedTime - Time.fixedDeltaTime;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null)
                    continue;
                var x = World.main.IntegrateTransformPosition(rig.GetBodyId(i), dt);
                if (i == NoodleBones.Hips)
                    transforms[i].position = x.pos;
                transforms[i].rotation = x.rot;
            }
        }

        internal void BindPose()
        {
            Debug.Assert(rig.IsAlive);

            ref var pose = ref EntityStore.GetComponentData<NoodlePose>(rig.entity);
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(rig.entity);
            ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(rig.entity);
            var poseTransform = new RigidTransform(quaternion.RotateY(inputFrame.lookYaw), rig.rootPosition);
            var t = NoodlePoseTransforms.GetBodyTransforms(pose, dim).Transform(poseTransform);

            transforms[NoodleBones.Hips].position = math.transform(t.hips, dim.hipsAnchor);
            transforms[NoodleBones.Hips].rotation = t.hips.rot;
            transforms[NoodleBones.Waist].rotation = t.waist.rot;
            transforms[NoodleBones.Chest].rotation = t.chest.rot;
            transforms[NoodleBones.Head].rotation = t.head.rot;

            transforms[NoodleBones.UpperArmL].rotation = t.upperArmL.rot;
            transforms[NoodleBones.UpperArmR].rotation = t.upperArmR.rot;
            transforms[NoodleBones.LowerArmL].rotation = t.lowerArmL.rot;
            transforms[NoodleBones.LowerArmR].rotation = t.lowerArmR.rot;
            transforms[NoodleBones.UpperLegL].rotation = t.upperLegL.rot;
            transforms[NoodleBones.UpperLegR].rotation = t.upperLegR.rot;
            transforms[NoodleBones.LowerLegL].rotation = t.lowerLegL.rot;
            transforms[NoodleBones.LowerLegR].rotation = t.lowerLegR.rot;
        }

        [ContextMenu("Autodetect Transforms")]
        public void AutodetectTransforms()
        {
            transforms = new Transform[NoodleBones.Last + 1];
            var children = transform.GetComponentsInChildren<Transform>(true);
            DetectChild(children, NoodleBones.Hips, "Hips", "spine");
            DetectChild(children, NoodleBones.Waist, "Waist", "spine.001", "Spine");
            DetectChild(children, NoodleBones.Chest, "Chest", "spine.003");
            DetectChild(children, NoodleBones.Head, "Head", "face");
            DetectChild(children, NoodleBones.UpperArmL, "UpperArmL", "LeftArm", "upper_arm.L", "LeftUpperArm");
            DetectChild(children, NoodleBones.LowerArmL, "LowerArmL", "LeftForearm", "forearm.L", "LeftLowerArm");
            DetectChild(children, NoodleBones.UpperLegL, "UpperLegL", "LeftThigh", "thigh.L", "LeftUpperLeg");
            DetectChild(children, NoodleBones.LowerLegL, "LowerLegL", "LeftLeg", "shin.L", "LeftLowerLeg");
            DetectChild(children, NoodleBones.UpperArmR, "UpperArmR", "RightArm", "upper_arm.R", "RightUpperArm");
            DetectChild(children, NoodleBones.LowerArmR, "LowerArmR", "RightForearm", "forearm.R", "RightLowerArm");
            DetectChild(children, NoodleBones.UpperLegR, "UpperLegR", "RightThigh", "thigh.R", "RightUpperLeg");
            DetectChild(children, NoodleBones.LowerLegR, "LowerLegR", "RightLeg", "shin.R", "RightLowerLeg");
        }

        private void DetectChild(Transform[] children, int target, params string[] names)
        {
            foreach (var name in names)
                if (transforms[target] == null)
                    transforms[target] = children.FirstOrDefault(c => c.name.Contains(name));
        }
    }
}
