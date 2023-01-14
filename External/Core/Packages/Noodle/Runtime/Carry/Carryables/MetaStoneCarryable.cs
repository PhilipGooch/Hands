using NBG.Entities;
using Recoil;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // Meta stone - carryable using hand space to describe the motion
    // Pivot trajectory is specified as world rotation when hand is extended forward
    [BurstCompile]

    public abstract class MetaStoneCarryable : CarryableBase
    {
        protected override CarryAlgorithmSingle GetCarryFunction(in HandCarryData hand) => CarryAlgorithmSingle.MetaStone;

    }
}