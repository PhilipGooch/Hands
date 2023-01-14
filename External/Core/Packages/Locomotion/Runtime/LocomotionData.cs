using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Recoil;

public struct LocomotionData
{
    public float3 targetVelocity;
    public float3 strafeVelocity;
    public bool jump;
    public readonly float3 CurrentFacing;

    internal GroundData groundData;
    internal AgentData agentData;

    public float3 GroundNormal => groundData.groundNormal;
    public float3 VelocityOnGround => groundData.velocityOnGround;
    public bool Grounded => groundData.grounded;
    public float JumpHeight => agentData.jumpHeight;
    public float JumpDuration => agentData.jumpDuration;
    public float AgentSphereRadius => agentData.agentSphereRadius;
    public int AgentRecoilId => agentData.recoilId;
    public float3 position => World.main.GetBodyPosition(agentData.recoilId).pos;
    public float3 velocity => World.main.GetVelocity(agentData.recoilId).linear;

    public LocomotionData(float3 targetVelocity, float3 strafeVelocity, bool jump, float3 currentFacing, GroundData groundData, AgentData agentData)
    {
        this.targetVelocity = targetVelocity;
        this.strafeVelocity = strafeVelocity;
        this.jump = jump;
        CurrentFacing = currentFacing;
        this.groundData = groundData;
        this.agentData = agentData;
    }

    public struct GroundData
    {
        public readonly float3 groundNormal;
        public readonly float3 velocityOnGround;
        public readonly bool grounded;

        public GroundData(float3 groundNormal, float3 velocityOnGround, bool grounded)
        {
            this.groundNormal = groundNormal;
            this.velocityOnGround = velocityOnGround;
            this.grounded = grounded;
        }
    }

    public struct AgentData
    {
        public readonly float jumpHeight;
        public readonly float jumpDuration;
        public readonly float agentSphereRadius;
        public readonly int recoilId;

        public AgentData(float jumpHeight, float jumpDuration, float agentSphereRadius, int recoilId)
        {
            this.jumpHeight = jumpHeight;
            this.jumpDuration = jumpDuration;
            this.agentSphereRadius = agentSphereRadius;
            this.recoilId = recoilId;
        }
    }
}
