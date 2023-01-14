using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LockLinearExperiment : MonoBehaviour
{
    public Rigidbody bodyA;
    public float3 anchorA;
    public Rigidbody bodyB;
    public float3 anchorB;

    public float kp;
    public float kd;

    private void Awake()
    {
        Physics.gravity = Vector3.zero;
    }

    void FixedUpdate()
    {
        //var xA = anchorA;
        //var invMassA = float3x3.zero;
        //var vA = float3.zero;

        var rA = math.rotate(bodyA.rotation, anchorA - (float3)bodyA.centerOfMass);
        var tensorARot = math.mul(bodyA.rotation, bodyA.inertiaTensorRotation);

        var IA = RigidBodyInertia.CalculatIFromTensor(tensorARot, bodyA.inertiaTensor);
        var invIA = re.mul(re.cross(rA), re.mul(re.inverse(IA), re.cross(-rA)));
        var invMA = 1 / bodyB.mass;
        var invMassA = invIA + float3x3.Scale(invMA);
        var xA = (float3)bodyA.worldCenterOfMass + rA;
        var vA = math.cross(bodyA.angularVelocity, rA) + (float3)bodyA.velocity;


        var rB = math.rotate(bodyB.rotation, anchorB - (float3)bodyB.centerOfMass);
        var tensorRot = math.mul(bodyB.rotation, bodyB.inertiaTensorRotation);

        var IB = RigidBodyInertia.CalculatIFromTensor(tensorRot, bodyB.inertiaTensor);
        var invIB = re.mul(re.cross(rB), re.mul(re.inverse(IB), re.cross(-rB)));
        var invMB = 1 / bodyB.mass;
        var invMassB = invIB + float3x3.Scale(invMB);
        var xB = (float3)bodyB.worldCenterOfMass + rB;
        var vB = math.cross(bodyB.angularVelocity, rB) + (float3)bodyB.velocity;

        var C = xB-xA;

        var h = Time.fixedDeltaTime; var k = kp; var d = kd;
        var gamma = h * (d + h * k);
        gamma = gamma != 0.0f ? 1.0f / gamma : 0.0f;
        var bias = C * h * k * gamma;


        var softMass = math.inverse(invMassA + invMassB + float3x3.identity * gamma);

        
        var vErr = vB - vA;
        var impulse = float3.zero;
        var deltaImpulse = -re.mul(softMass, vErr + bias + gamma * impulse);

        // apply delta impulse
        //blockA.ApplyImpulse(bA, anchorA, deltaImpulse);
        bodyA.velocity += (Vector3)(invMA * -deltaImpulse);
        bodyA.angularVelocity += (Vector3)re.mul(invIA, math.cross(rA, -deltaImpulse));
        bodyB.velocity += (Vector3)( invMB * deltaImpulse);
        bodyB.angularVelocity += (Vector3)re.mul(invIB ,math.cross(rB, deltaImpulse));



        impulse += deltaImpulse;
    }


}
