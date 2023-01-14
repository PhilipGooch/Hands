using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class DistanceJoint
{
    public Rigidbody bodyA;
    public Rigidbody bodyB;

    public float length;
    public float minLength;
    public float maxLength;
    public float stiffness = 10;
    public float damping = 1;

    float m_gamma;
    float m_bias;
    float m_currentLength;
    float m_mass;
    float m_softMass;
    float m_impulse;
    //float m_lowerImpulse;
    //float m_upperImpulse;
    float3 m_u;
    float3 m_rA;
    float3 m_rB;
    float m_invMassA;
    float m_invMassB;
    // TODO: make lt3x3
    float m_invIA;
    float m_invIB;
    const float b2_linearSlop = .000001f;

    public float3 m_localCenterA;
    public float3 m_localCenterB;
    public float3 m_localAnchorA;
    public float3 m_localAnchorB;
    public void InitVelocityConstraints()
    {
        //var m_localAnchorA = float3.zero;
        //var m_localAnchorB = float3.zero;

        //var m_localCenterA = float3.zero; //m_bodyA->m_sweep.localCenter;
        //var m_localCenterB = float3.zero; //m_bodyB->m_sweep.localCenter;
        m_invMassA = bodyA != null ? 1 / bodyA.mass : 0;
        m_invMassB = bodyB != null ? 1 / bodyB.mass : 0;
        //m_invIA = m_bodyA->m_invI;
        //m_invIB = m_bodyB->m_invI;
        m_invIA = 0;
        m_invIB = 0;

        float3 cA = bodyA.position;
        quaternion qA = bodyA.rotation;
        float3 vA = bodyA.velocity;
        float3 wA = bodyA.angularVelocity;

        float3 cB = bodyB!=null?bodyB.position:default;
        quaternion qB = bodyB != null ? bodyB.rotation:Quaternion.identity;
        float3 vB = bodyB != null ? bodyB.velocity: default;
        float3 wB = bodyB != null ? bodyB.angularVelocity : default;


        m_rA = math.mul(qA, m_localAnchorA - m_localCenterA);
        m_rB = math.mul(qB, m_localAnchorB - m_localCenterB);
        m_u = cB + m_rB - cA - m_rA;

        // Handle singularity.
        m_currentLength = math.length(m_u);
        if (m_currentLength > b2_linearSlop)
        {
            m_u *= 1.0f / m_currentLength;
        }
        else
        {
            m_u = float3.zero;
            m_mass = 0.0f;
            m_impulse = 0.0f;
        }

        var crAu = math.cross(m_rA, m_u);
        var crBu = math.cross(m_rB, m_u);
        float invMass = m_invMassA + m_invIA * math.dot(crAu, crAu) + m_invMassB + m_invIB * math.dot(crBu, crBu);
        m_mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;

        if (stiffness > 0.0f && minLength < maxLength)
        {
            // soft
            float C = m_currentLength - length;

            float d = damping;
            float k = stiffness;

            // magic formulas
            float h = Time.fixedDeltaTime;

            // gamma = 1 / (h * (d + h * k))
            // the extra factor of h in the denominator is since the lambda is an impulse, not a force
            m_gamma = h * (d + h * k);
            m_gamma = m_gamma != 0.0f ? 1.0f / m_gamma : 0.0f;
            m_bias = C * h * k * m_gamma;

            invMass += m_gamma;
            m_softMass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
        }
        else
        {
            // rigid
            m_gamma = 0.0f;
            m_bias = 0.0f;
            m_softMass = m_mass;
        }

        m_impulse = 0.0f;

    }
    public void SolveVelocityConstraints()
    {

        float3 vA = bodyA != null ? bodyA.velocity : default;
        float3 wA = bodyA != null ? bodyA.angularVelocity : default;
        float3 vB = bodyB != null ? bodyB.velocity : default;
        float3 wB = bodyB != null ? bodyB.angularVelocity : default;


        if (minLength < maxLength)
        {
            if (stiffness > 0.0f)
            {
                // Cdot = dot(u, v + cross(w, r))
                var vpA = vA + math.cross(wA, m_rA);
                var vpB = vB + math.cross(wB, m_rB);
                float Cdot = math.dot(m_u, vpB - vpA);

                float impulse = -m_softMass * (Cdot + m_bias + m_gamma * m_impulse);
                m_impulse += impulse;

                var P = impulse * m_u;
                vA -= m_invMassA * P;
                wA -= m_invIA * math.cross(m_rA, P);
                vB += m_invMassB * P;
                wB += m_invIB * math.cross(m_rB, P);
            }

            //// lower
            //{
            //	float C = m_currentLength - m_minLength;
            //	float bias = b2Max(0.0f, C) * step.inv_dt;

            //	b2Vec2 vpA = vA + b2Cross(wA, m_rA);
            //	b2Vec2 vpB = vB + b2Cross(wB, m_rB);
            //	float Cdot = b2Dot(m_u, vpB - vpA);

            //	float impulse = -m_mass * (Cdot + bias);
            //	float oldImpulse = m_lowerImpulse;
            //	m_lowerImpulse = b2Max(0.0f, m_lowerImpulse + impulse);
            //	impulse = m_lowerImpulse - oldImpulse;
            //	b2Vec2 P = impulse * m_u;

            //	vA -= m_invMassA * P;
            //	wA -= m_invIA * b2Cross(m_rA, P);
            //	vB += m_invMassB * P;
            //	wB += m_invIB * b2Cross(m_rB, P);
            //}

            //// upper
            //{
            //	float C = m_maxLength - m_currentLength;
            //	float bias = b2Max(0.0f, C) * step.inv_dt;

            //	b2Vec2 vpA = vA + b2Cross(wA, m_rA);
            //	b2Vec2 vpB = vB + b2Cross(wB, m_rB);
            //	float Cdot = b2Dot(m_u, vpA - vpB);

            //	float impulse = -m_mass * (Cdot + bias);
            //	float oldImpulse = m_upperImpulse;
            //	m_upperImpulse = b2Max(0.0f, m_upperImpulse + impulse);
            //	impulse = m_upperImpulse - oldImpulse;
            //	b2Vec2 P = -impulse * m_u;

            //	vA -= m_invMassA * P;
            //	wA -= m_invIA * b2Cross(m_rA, P);
            //	vB += m_invMassB * P;
            //	wB += m_invIB * b2Cross(m_rB, P);
            //}
        }
        //	else
        //{
        //	// Equal limits

        //	// Cdot = dot(u, v + cross(w, r))
        //	b2Vec2 vpA = vA + b2Cross(wA, m_rA);
        //	b2Vec2 vpB = vB + b2Cross(wB, m_rB);
        //	float Cdot = b2Dot(m_u, vpB - vpA);

        //	float impulse = -m_mass * Cdot;
        //	m_impulse += impulse;

        //	b2Vec2 P = impulse * m_u;
        //	vA -= m_invMassA * P;
        //	wA -= m_invIA * b2Cross(m_rA, P);
        //	vB += m_invMassB * P;
        //	wB += m_invIB * b2Cross(m_rB, P);
        //}


        if (bodyA != null) bodyA.velocity = vA;
        if (bodyA != null) bodyA.angularVelocity = wA;
        if (bodyB != null) bodyB.velocity = vB;
        if (bodyB != null) bodyB.angularVelocity = wB;


    }

}

public class DistanceJointExperiment : MonoBehaviour
{
    public DistanceJoint[] joints;

    private void Awake()
    {
        
        var springs = GetComponentsInChildren<SpringJoint>();
        joints = new DistanceJoint[springs.Length];
        for (int i=0; i<springs.Length;i++)
        {
            var s = springs[i];
            joints[i] = new DistanceJoint()
            {
                bodyA = s.GetComponent<Rigidbody>(),
                bodyB = s.connectedBody,
                m_localAnchorA = s.anchor,
                m_localAnchorB = s.connectedAnchor,
                m_localCenterA = s.GetComponent<Rigidbody>().centerOfMass,
                m_localCenterB = s.connectedBody!=null? s.connectedBody.centerOfMass:default,
                stiffness = s.spring,
                damping = s.damper,
                length = 0,
                minLength = 0,
                maxLength = 4
            };
            Destroy(s);
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < joints.Length; i++)
            joints[i].InitVelocityConstraints();
        for(int iteration=0; iteration < 10; iteration++)
            for (int i = 0; i < joints.Length; i++)
                joints[i].SolveVelocityConstraints();
    }

    
  
}
