using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Recoil;

namespace NBG.XPBDRope
{
    public static class XpbdConstraints
    {
        const float FLT_EPSILON = 1e-7f;
        public static void Solve(ref float3 x1, float inv_mass1, ref float3 x2, float inv_mass2, float length, ref float lambda, float compliance, float dt, float maxSeparation)
        {
            var sum_mass = inv_mass1 + inv_mass2;

            if (sum_mass == 0.0f) { return; }

            var p1_minus_p2 = x1 - x2;
            var distance = math.length(p1_minus_p2);
            var direction = math.normalizesafe(p1_minus_p2, float3.zero);
            var constraint = distance - length; // Cj(x)
                                                //if (constraint < 0) return;

            var hardLimit = math.clamp(math.abs(constraint) - maxSeparation, 0f, float.MaxValue);
            if (hardLimit > 0)
            {
                var c = hardLimit * math.sign(constraint);
                x1 -= (inv_mass1 * c / sum_mass) * direction;
                x2 += (inv_mass2 * c / sum_mass) * direction;
                constraint = maxSeparation * math.sign(constraint);
            }

            compliance /= dt * dt;    // a~
            var dlambda = (-constraint - compliance * lambda) / (sum_mass + compliance); // eq.18
            var correction_vector = dlambda * direction;                    // eq.17
            lambda += dlambda;
            x1 += +inv_mass1 * correction_vector;
            x2 += -inv_mass2 * correction_vector;
        }

        public static void SolveAttachedBody(ref float3 x1, float inv_mass1, ref RigidTransform bodyTransform, float attachedInvM, lt3x3 attachedInvI, float3 anchor,
            ref float lambda, float compliance, float dt, float maxSeparation)
        {
            var connectionDir = math.mul(bodyTransform.rot, anchor);
            var connectionPos = bodyTransform.pos + connectionDir;
            var delta = x1 - connectionPos;
            var direction = math.normalizesafe(delta, float3.zero);
            var connectionCrossProduct = math.cross(connectionDir, direction);
            var bodyInvMass = 0f;
            var bodyInvI = lt3x3.zero;
            if (attachedInvM > 0f)
            {
                bodyInvI = attachedInvI;
                bodyInvMass = attachedInvM + math.dot(connectionCrossProduct * bodyInvI, connectionCrossProduct);
            }

            var sum_mass = inv_mass1 + bodyInvMass;
            var constraint = math.length(delta);

            var hardLimit = math.clamp(constraint - maxSeparation, 0f, float.MaxValue);
            if (hardLimit > 0)
            {
                x1 -= (inv_mass1 * hardLimit / sum_mass) * direction;
                MoveAttachedBody(ref bodyTransform, connectionDir, hardLimit * direction, attachedInvM, attachedInvI);
                constraint = maxSeparation;
            }

            var aCompliance = compliance / (dt * dt); // a~
            var deltaLambda = (-constraint - aCompliance * lambda) / (sum_mass + aCompliance);
            lambda += deltaLambda;

            var impulse = deltaLambda * direction;
            x1 += impulse * inv_mass1;
            MoveAttachedBody(ref bodyTransform, connectionDir, impulse, attachedInvM, bodyInvI);
        }

        static void MoveAttachedBody(ref RigidTransform bodyTransform, float3 worldAnchor, float3 impulse, float invM, lt3x3 invI)
        {
            bodyTransform.pos -= impulse * invM;
            var rot = bodyTransform.rot;
            rot.value -= math.mul(new quaternion(0.5f * new float4(invI * math.cross(worldAnchor, impulse), 0f)), rot).value;
            rot = math.normalize(rot);
            bodyTransform.rot = rot;
        }

        public static void SolveBend(ref float3 start, float inv_mass1, ref float3 center, float inv_massc, ref float3 end, float inv_mass2, ref float lambda, float compliance, float angle_limit, float dt)
        {

            var startLine = center - start;
            var endLine = end - center;
            var startLineLength = math.length(startLine);
            var endLineLength = math.length(endLine);
            var d = math.dot(startLine, endLine) / startLineLength / endLineLength;

            if (d < -1f + FLT_EPSILON)
            {
                // Rope segment penetrated into another segment in a straight line.
                return;
            }

            float constraint;
            float3 dC1, dC2, dCc;

            if (d > 1f - FLT_EPSILON) // close to straight use approximations
            {
                // Limit less than 5 degrees
                if (angle_limit > math.PI / 36) return;
                var c_to_projection = math.lerp(start, end, startLineLength / (startLineLength + endLineLength)) - center;
                var cpl = math.length(c_to_projection);
                if (cpl == 0) return;

                var n = -c_to_projection / cpl;
                dC1 = -n / startLineLength; // could also use nominal length
                dC2 = -n / endLineLength;
                dCc = -dC1 - dC2;

                constraint = cpl / startLineLength + cpl / endLineLength; // from small angle sine approximation
            }
            else
            {
                constraint = math.acos(d);
                constraint -= angle_limit;
                if (constraint <= 0)
                    return;

                var correction_axis = math.normalizesafe(math.cross(startLine, endLine));
                var n1 = math.normalizesafe(math.cross(startLine, correction_axis));
                var n2 = math.normalizesafe(math.cross(endLine, correction_axis));

                // derivative of constraint with respect to each  point motion
                dC1 = -n1 / startLineLength; // could also use nominal length
                dC2 = -n2 / endLineLength;
                dCc = -dC1 - dC2;


            }

            var sum_mass = inv_mass1 * math.lengthsq(dC1) + inv_mass2 * math.lengthsq(dC2) + inv_massc * math.lengthsq(dCc);

            compliance /= dt * dt;    // a~
            var dlambda = (-constraint - compliance * lambda) / (sum_mass + compliance); // eq.18
            var correction_angle = dlambda;

            lambda += dlambda;


            start += inv_mass1 * correction_angle * dC1;
            end += inv_mass2 * correction_angle * dC2;
            center += inv_massc * correction_angle * dCc;

        }
    }
}
