using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MR = ModernRobotics;



//https://github.com/dartsim/dart/blob/master/dart/dynamics/Inertia.hpp
public struct Inertia
{
    public float mass;
    /// <summary>
    /// Local center of mass
    /// </summary>
    public Vector3 centerOfMass;
    public lt3x3 I;

    public static Inertia zero
    {
        get
        {
            return new Inertia(0f, Vector3.zero, lt3x3.zero);
        }
    }

    public Inertia(
        float m, Vector3 h, lt3x3 I)
    {
        this.mass = m;
        this.centerOfMass = h;
        this.I = I;
    }

    public static Inertia FromRigidAtPoint(ReBody body, Vector3 pos)
    {
        return FromRigidAtCenter(body).Translate(pos - body.worldCenterOfMass);
    }

    public static Inertia FromRigidAtPoint(Rigidbody body, Vector3 pos)
    {
        return FromRigidAtPoint(new ReBody(body), pos);
    }

#if UNITY_EDITOR
    public static Inertia FromRigidAtPointEditor(Rigidbody body, Vector3 pos)
    {
        return Assemble(body.mass, body.inertiaTensor, body.rotation * body.inertiaTensorRotation).Translate(pos - body.worldCenterOfMass);
    }
#endif

    public static Inertia Assemble(float mass, Vector3 tensor, Quaternion tensorToWorld)
    {
        var rot = Matrix3x3.Rotate(tensorToWorld);
        var rotInv = Matrix3x3.Rotate(Quaternion.Inverse(tensorToWorld));
        var I = tensor;
        var m = mass;

        var Imatrix = new Matrix3x3(I.x, 0, 0, 0, I.y, 0, 0, 0, I.z);
        var Irotated = rot * Imatrix * rotInv;// rot.transposed;

        return new Inertia(m, Vector3.zero, new lt3x3(Irotated));

    }

    public static Inertia FromRigidAtCenter(ReBody body)
    {
        return Assemble(body.mass, body.inertiaTensor, body.rotation * body.inertiaTensorRotation);
    }

    public static Inertia FromRigidAtCenter(Rigidbody body)
    {
        return FromRigidAtCenter(new ReBody(body));
        //var rot = Matrix3x3.Rotate(body.rotation * body.inertiaTensorRotation);
        //var rotInv = Matrix3x3.Rotate(Quaternion.Inverse(body.rotation * body.inertiaTensorRotation));
        //var I = body.inertiaTensor;
        //var m = body.mass;

        //var Imatrix = new Matrix3x3(I.x, 0, 0, 0, I.y, 0, 0, 0, I.z);
        //var Irotated = rot * Imatrix * rotInv;// rot.transposed;

        //return new Inertia(m, Vector3.zero, new lt3x3(Irotated));
    }
    // return resulting angular acceleration if given linear acceleration is applied


    // //F p245
    // public Matrix6x6 ToMatrix6x6()
    // {
    //     //F2.63
    //     var hx = MR.VecToso3(h);

    //     return new Matrix6x6(
    //         new Matrix3x3(Ixx, Ixy, Ixz, Ixy, Iyy, Iyz, Ixz, Iyz, Izz),  //Ic+mCxCx^T
    //         hx,
    //         -hx, Matrix3x3.Diagonal(m));

    //     //TODO: unwrap

    // }
    //F p247
    public static Inertia operator +(Inertia lhs, Inertia rhs)
    {
        return new Inertia(lhs.mass + rhs.mass, lhs.centerOfMass + rhs.centerOfMass, lhs.I + rhs.I);
    }

    public static Inertia operator*(Inertia lhs, float rhs)
    {
        return new Inertia(lhs.mass *rhs, lhs.centerOfMass * rhs, rhs*lhs.I );
    }

    public Inertia Translate(Vector3 r)
    {

        // V2. optimized
        var HminusrxM = centerOfMass - r * mass;// H-r.Cross(M);
        return new Inertia(mass, HminusrxM, I + r.LtCrossCross(centerOfMass) + HminusrxM.LtCrossCross(r));

    }

    // p.247
    public static Vector6 operator *(Inertia I, Vector6 vec)
    {
        var hx = MR.VecToso3(I.centerOfMass);
        var w = vec.FirstVector3();
        var v = vec.SecondVector3();
        return new Vector6(I.I * w + hx * v, I.mass * v - hx * w);
    }

    // inverse inertia multiply by force
    public Vector6 InvMul(Vector6 force)
    {
        return inverse * force;
    }



    //https://www.dr-lex.be/random/matrix-inv.html

    public Inertia inverse
    {
        get
        {
            return new Inertia(1 / mass, -centerOfMass / mass / mass, I.inverse);


        }
    }
    public float InvMassOnAxis(Vector3 axis) // TODO optimize?
    {
        var r = centerOfMass / mass;
        var ICG = Translate(r);

        var invMass = 1 / mass;

        var invInertiaDiagLocal = ICG.I.inverse;
        var angularAxis = Vector3.Cross(r, axis);
        var m_aJ = angularAxis; // 
        var invAngularMass = Vector3.Dot(invInertiaDiagLocal * m_aJ, m_aJ);
        return invMass + invAngularMass;
    }

    public Vector3 LinearAccelerationToAngular(Vector3 acceleration)
    {
        //TODO: optimize
        var r = centerOfMass / mass;
        var f = acceleration / InvMassOnAxis(acceleration.normalized);
        var t = Vector3.Cross(f, r);
        var ICG = Translate(r);
        return ICG.I.inverse * t;
    }

    public Vector6 TimesAngularAcceleration(Vector3 w)
    {
        var hx = MR.VecToso3(centerOfMass);
        return new Vector6(I * w, -hx * w);
    }
    public Vector6 TimesLinearAcceleration(Vector3 v)
    {
        var hx = MR.VecToso3(centerOfMass);
        return new Vector6(hx * v, mass * v);
    }





    // at center of gravity
    public static Vector6 ForceToAccelerateBody(Rigidbody body, Vector6 vec)
    {
        var I = body.inertiaTensor;
        var m = body.mass;
        var w = vec.FirstVector3();
        var v = vec.SecondVector3();
        var rot = body.rotation;
        var rotInv = Quaternion.Inverse(rot);
        var torque = rot * (Vector3.Scale(I, rotInv * w));
        var force = m * v;

        return new Vector6(torque,force);
    }

    // at specific point
    public static Vector6 ForceToAccelerateBody(Rigidbody body, Vector6 vec, Vector3 pos)
    {
        var I = body.inertiaTensor;
        var m = body.mass;
        var r = pos - body.worldCenterOfMass;
        var w = vec.FirstVector3();
        var v = vec.SecondVector3();
        //PluckerTranslate.TransformVelocity(-r, ref w, ref v);
        v = v - Vector3.Cross(-r, w);

        var rot = body.rotation;
        var rotInv = Quaternion.Inverse(rot);
        var torque = rot * (Vector3.Scale(I, rotInv * w));
        var force = m * v;

        //PluckerTranslate.TransformForce(r, ref torque, ref force);
        torque = torque - Vector3.Cross(r, force);

        return new Vector6(torque, force);
    }

    // only linear at point
    public static Vector6 ForceToLinearAcceleration(Rigidbody body, Vector3 v, Vector3 pos)
    {
        var m = body.mass;
        var r = pos - body.worldCenterOfMass;

        var force = m * v;
        var torque = -Vector3.Cross(r, force);

        return new Vector6(torque, force);
    }

    // only angular at point
    public static Vector6 ForceToAngularAcceleration(Rigidbody body, Vector3 w, Vector3 pos)
    {
        var I = body.inertiaTensor;
        var m = body.mass;
        var r = pos - body.worldCenterOfMass;
        var v =  -Vector3.Cross(-r, w);

        var rot = body.rotation;
        var rotInv = Quaternion.Inverse(rot);
        var torque = rot * (Vector3.Scale(I, rotInv * w));
        var force = m * v;

        torque = torque - Vector3.Cross(r, force);

        return new Vector6(torque, force);
    }
}
