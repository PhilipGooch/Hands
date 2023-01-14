using Recoil;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class ToolCarryable : MetaGunCarryable
    {
        protected override Spring GetHandJointSpring() => new Spring(100000, 10000); // small tools - super strong connection to wrist
        protected override Spring GetHandJointSpringDual() => base.GetHandJointSpringDual();
    }
}
