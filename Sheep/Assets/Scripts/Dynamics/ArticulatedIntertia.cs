using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MR = ModernRobotics;
public struct lt3x3
{
    public float m00;
    public float m10, m11;
    public float m20, m21, m22;

    static readonly lt3x3 zeroMatrix = new lt3x3(0,0,0,0,0,0);

    // Returns a matrix with all elements set to zero (RO).
    public static lt3x3 zero { get { return zeroMatrix; } }

    public static lt3x3 Diagonal(float v)
    {
        return new lt3x3(v,0,v,0,0,v);
    }

    public static lt3x3 Diagonal(float m00,float m11,float m22)
    {
        return new lt3x3(m00,0,m11,0,0,m22);
    }


    public lt3x3(
        float m00,
        float m10, float m11,
        float m20, float m21, float m22)
    {
        this.m00 = m00;
        this.m10 = m10; this.m11 = m11;
        this.m20 = m20; this.m21 = m21; this.m22 = m22;
    }
    public lt3x3(Matrix3x3 m)
    {
        this.m00 = m.m00;
        this.m10 = m.m10; this.m11 = m.m11;
        this.m20 = m.m20; this.m21 = m.m21; this.m22 = m.m22;
    }
    public static Matrix3x3 operator - (Matrix3x3 lhs, lt3x3 rhs)
    {
        return new Matrix3x3(
            lhs.m00 - rhs.m00, lhs.m01 - rhs.m10, lhs.m02 - rhs.m20,
            lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m12 - rhs.m21,
            lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22);
    }
    public static Matrix3x3 operator -( lt3x3 lhs, Matrix3x3 rhs)
    {
        return new Matrix3x3(
            lhs.m00 - rhs.m00, lhs.m10 - rhs.m01, lhs.m20 - rhs.m02,
            lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m21 - rhs.m12,
            lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22);
    }
     public static Matrix3x3 operator + (Matrix3x3 lhs, lt3x3 rhs)
    {
        return new Matrix3x3(
            lhs.m00 + rhs.m00, lhs.m01 + rhs.m10, lhs.m02 + rhs.m20,
            lhs.m10 + rhs.m10, lhs.m11 + rhs.m11, lhs.m12 + rhs.m21,
            lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22);
    }
    public static Matrix3x3 operator +( lt3x3 lhs, Matrix3x3 rhs)
    {
        return new Matrix3x3(
            lhs.m00 + rhs.m00, lhs.m10 + rhs.m01, lhs.m20 + rhs.m02,
            lhs.m10 + rhs.m10, lhs.m11 + rhs.m11, lhs.m21 + rhs.m12,
            lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22);
    }
    public static lt3x3 operator +(lt3x3 lhs, lt3x3 rhs)
    {
        return new lt3x3(
            lhs.m00 + rhs.m00,
            lhs.m10 + rhs.m10, lhs.m11 + rhs.m11,
            lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22);

    }

    public static lt3x3 operator -(lt3x3 lhs, lt3x3 rhs)
    {
        return new lt3x3(
            lhs.m00 - rhs.m00,
            lhs.m10 - rhs.m10, lhs.m11 - rhs.m11,
            lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22);
    }
    public static Vector3 operator *(lt3x3 lhs, Vector3 vector)
    {
        Vector3 res;
        res.x = lhs.m00 * vector.x + lhs.m10 * vector.y + lhs.m20 * vector.z;
        res.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m21 * vector.z;
        res.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z;
        return res;
    }
    public static Matrix3x3 operator *(Matrix3x3 lhs, lt3x3 rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
        res.m01 = lhs.m00 * rhs.m10 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
        res.m02 = lhs.m00 * rhs.m20 + lhs.m01 * rhs.m21 + lhs.m02 * rhs.m22;

        res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
        res.m11 = lhs.m10 * rhs.m10 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
        res.m12 = lhs.m10 * rhs.m20 + lhs.m11 * rhs.m21 + lhs.m12 * rhs.m22;

        res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
        res.m21 = lhs.m20 * rhs.m10 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
        res.m22 = lhs.m20 * rhs.m20 + lhs.m21 * rhs.m21 + lhs.m22 * rhs.m22;

        return res;
    }
     public static lt3x3 operator * (float lhs, lt3x3 rhs)
    {
    return new lt3x3(
            lhs * rhs.m00,
            lhs * rhs.m10, lhs * rhs.m11,
            lhs * rhs.m20, lhs * rhs.m21, lhs * rhs.m22);
    }
    public lt3x3 inverse
    {
        get
        {
            var DET = m00 * (m22 * m11 - m21 * m21)
             - m10 * (m22 * m10 - m21 * m20)
             + m20 * (m21 * m10 - m11 * m20);

            var e = 0.0000001f;
            if (-e<DET && DET < e)
            {
                throw new System.InvalidOperationException("Can't calculate inverse for matrix with 0 D");

            }
            return new lt3x3(
                (m22 * m11 - m21 * m21) / DET,  
                -(m22 * m10 - m20 * m21) / DET, (m22 * m00 - m20 * m20) / DET,
                (m21 * m10 - m20 * m11) / DET, -(m21 * m00 - m20 * m10) / DET, (m11 * m00 - m10 * m10) / DET);

        }
    }

    // Get a column of the matrix.
    public Vector3 GetColumn(int index)
    {
        switch (index)
        {
            case 0: return new Vector3(m00, m10, m20);
            case 1: return new Vector3(m10, m11, m21);
            case 2: return new Vector3(m20, m21, m22);
            default:
                throw new IndexOutOfRangeException("Invalid column index!");
        }
    }


    // Returns a row of the matrix.
    public Vector3 GetRow(int index)
    {
        switch (index)
        {
            case 0: return new Vector3(m00, m10, m20);
            case 1: return new Vector3(m10, m11, m21);
            case 2: return new Vector3(m20, m21, m22);
            default:
                throw new IndexOutOfRangeException("Invalid row index!");
        }
    }

     public override string ToString()
    {
        return String.Format("{0:F5}\t{1:F5}\t{2:F5}\n{3:F5}\t{4:F5}\t{5:F5}\n{6:F5}\t{7:F5}\t{8:F5}\n", m00, m10, m20, m10, m11, m21, m20, m21, m22);
    }

}

public static class Vector3Extensions
{

 
    public static lt3x3 LtPreCross(this Vector3 v, Matrix3x3 M)
    {
        var res = new lt3x3();
        res.m00 = M.m00 * 0000 + M.m01 * +v.z + M.m02 * -v.y;
        // res.m01 = M.m00 * -v.z + M.m01 * 0000 + M.m02 * +v.x;
        // res.m02 = M.m00 * +v.y + M.m01 * -v.x + M.m02 * 0000;

        res.m10 = M.m10 * 0000 + M.m11 * +v.z + M.m12 * -v.y;
        res.m11 = M.m10 * -v.z + M.m11 * 0000 + M.m12 * +v.x;
        // res.m12 = M.m10 * +v.y + M.m11 * -v.x + M.m12 * 0000;

        res.m20 = M.m20 * 0000 + M.m21 * +v.z + M.m22 * -v.y;
        res.m21 = M.m20 * -v.z + M.m21 * 0000 + M.m22 * +v.x;
        res.m22 = M.m20 * +v.y + M.m21 * -v.x + M.m22 * 0000;

        return res;
    }

    public static Matrix3x3 PreCross(this Vector3 v, Matrix3x3 M)
    {
        Matrix3x3 res;
        res.m00 = M.m00 * 0000 + M.m01 * +v.z + M.m02 * -v.y;
        res.m01 = M.m00 * -v.z + M.m01 * 0000 + M.m02 * +v.x;
        res.m02 = M.m00 * +v.y + M.m01 * -v.x + M.m02 * 0000;

        res.m10 = M.m10 * 0000 + M.m11 * +v.z + M.m12 * -v.y;
        res.m11 = M.m10 * -v.z + M.m11 * 0000 + M.m12 * +v.x;
        res.m12 = M.m10 * +v.y + M.m11 * -v.x + M.m12 * 0000;

        res.m20 = M.m20 * 0000 + M.m21 * +v.z + M.m22 * -v.y;
        res.m21 = M.m20 * -v.z + M.m21 * 0000 + M.m22 * +v.x;
        res.m22 = M.m20 * +v.y + M.m21 * -v.x + M.m22 * 0000;
        return res;

    }
    public static Matrix3x3 Cross(this Vector3 v, Matrix3x3 M)
    {
        Matrix3x3 res;
        res.m00 = 0 * M.m00 - v.z * M.m10 + v.y * M.m20;
        res.m01 = 0 * M.m01 - v.z * M.m11 + v.y * M.m21;
        res.m02 = 0 * M.m02 - v.z * M.m12 + v.y * M.m22;

        res.m10 = v.z * M.m00 + 0 * M.m10 - v.x * M.m20;
        res.m11 = v.z * M.m01 + 0 * M.m11 - v.x * M.m21;
        res.m12 = v.z * M.m02 + 0 * M.m12 - v.x * M.m22;

        res.m20 = -v.y * M.m00 + v.x * M.m10 + 0 * M.m20;
        res.m21 = -v.y * M.m01 + v.x * M.m11 + 0 * M.m21;
        res.m22 = -v.y * M.m02 + v.x * M.m12 + 0 * M.m22;

        return res;
    }
    public static Matrix3x3 Cross(this Vector3 v, lt3x3 M)
    {
        Matrix3x3 res;
        res.m00 = 0 * M.m00 - v.z * M.m10 + v.y * M.m20;
        res.m01 = 0 * M.m10 - v.z * M.m11 + v.y * M.m21;
        res.m02 = 0 * M.m20 - v.z * M.m21 + v.y * M.m22;

        res.m10 = v.z * M.m00 + 0 * M.m10 - v.x * M.m20;
        res.m11 = v.z * M.m10 + 0 * M.m11 - v.x * M.m21;
        res.m12 = v.z * M.m20 + 0 * M.m21 - v.x * M.m22;

        res.m20 = -v.y * M.m00 + v.x * M.m10 + 0 * M.m20;
        res.m21 = -v.y * M.m10 + v.x * M.m11 + 0 * M.m21;
        res.m22 = -v.y * M.m20 + v.x * M.m21 + 0 * M.m22;

        return res;
    }

    public static lt3x3 LtCross(this Vector3 v, lt3x3 M)
    {
        lt3x3 res;
        res.m00 = 0 * M.m00 - v.z * M.m10 + v.y * M.m20;
        //res.m01 = 0 * M.m10 - v.z * M.m11 + v.y * M.m21;
        //res.m02 = 0 * M.m20 - v.z * M.m21 + v.y * M.m22;

        res.m10 = v.z * M.m00 + 0 * M.m10 - v.x * M.m20;
        res.m11 = v.z * M.m10 + 0 * M.m11 - v.x * M.m21;
        //res.m12 = v.z * M.m20 + 0 * M.m21 - v.x * M.m22;

        res.m20 = -v.y * M.m00 + v.x * M.m10 + 0 * M.m20;
        res.m21 = -v.y * M.m10 + v.x * M.m11 + 0 * M.m21;
        res.m22 = -v.y * M.m20 + v.x * M.m21 + 0 * M.m22;

        return res;
    }
    public static Matrix3x3 CrossT(this Vector3 v, Matrix3x3 M)
    {
        Matrix3x3 res;
        res.m00 = 0 * M.m00 - v.z * M.m01 + v.y * M.m02;
        res.m01 = 0 * M.m10 - v.z * M.m11 + v.y * M.m12;
        res.m02 = 0 * M.m20 - v.z * M.m21 + v.y * M.m22;

        res.m10 = v.z * M.m00 + 0 * M.m01 - v.x * M.m02;
        res.m11 = v.z * M.m10 + 0 * M.m11 - v.x * M.m12;
        res.m12 = v.z * M.m20 + 0 * M.m21 - v.x * M.m22;

        res.m20 = -v.y * M.m00 + v.x * M.m01 + 0 * M.m02;
        res.m21 = -v.y * M.m10 + v.x * M.m11 + 0 * M.m12;
        res.m22 = -v.y * M.m20 + v.x * M.m21 + 0 * M.m22;

        return res;
    }
    public static lt3x3 LtCrossT(this Vector3 v, Matrix3x3 M)
    {
        lt3x3 res;
        res.m00 = 0 * M.m00 - v.z * M.m01 + v.y * M.m02;
        //res.m01 = 0 * M.m10 - v.z * M.m11 + v.y * M.m12;
        //res.m02 = 0 * M.m20 - v.z * M.m21 + v.y * M.m22;

        res.m10 = v.z * M.m00 + 0 * M.m01 - v.x * M.m02;
        res.m11 = v.z * M.m10 + 0 * M.m11 - v.x * M.m12;
        //res.m12 = v.z * M.m20 + 0 * M.m21 - v.x * M.m22;

        res.m20 = -v.y * M.m00 + v.x * M.m01 + 0 * M.m02;
        res.m21 = -v.y * M.m10 + v.x * M.m11 + 0 * M.m12;
        res.m22 = -v.y * M.m20 + v.x * M.m21 + 0 * M.m22;

        return res;
    }

    public static Matrix3x3 CrossCross(this Vector3 l, Vector3 r)
    {
        Matrix3x3 res;
        res.m00 = 0 * 0000 - l.z * +r.z + l.y * -r.y;
        res.m01 = 0 * -r.z - l.z * 0000 + l.y * +r.x;
        res.m02 = 0 * +r.y - l.z * -r.x + l.y * 0000;

        res.m10 = l.z * 0000 + 0 * +r.z - l.x * -r.y;
        res.m11 = l.z * -r.z + 0 * 0000 - l.x * +r.x;
        res.m12 = l.z * +r.y + 0 * -r.x - l.x * 0000;

        res.m20 = -l.y * 0000 + l.x * +r.z + 0 * -r.y;
        res.m21 = -l.y * -r.z + l.x * 0000 + 0 * +r.x;
        res.m22 = -l.y * +r.y + l.x * -r.x + 0 * 0000;

        return res;
    }
     public static lt3x3 LtCrossCross(this Vector3 l, Vector3 r)
    {
        lt3x3 res;
        res.m00 = 0 * 0000 - l.z * +r.z + l.y * -r.y;
        // res.m01 = 0 * -r.z - l.z * 0000 + l.y * +r.x;
        // res.m02 = 0 * +r.y - l.z * -r.x + l.y * 0000;

        res.m10 = l.z * 0000 + 0 * +r.z - l.x * -r.y;
        res.m11 = l.z * -r.z + 0 * 0000 - l.x * +r.x;
        // res.m12 = l.z * +r.y + 0 * -r.x - l.x * 0000;

        res.m20 = -l.y * 0000 + l.x * +r.z + 0 * -r.y;
        res.m21 = -l.y * -r.z + l.x * 0000 + 0 * +r.x;
        res.m22 = -l.y * +r.y + l.x * -r.x + 0 * 0000;



        return res;
    }

    public static Vector3 Clamp(this Vector3 vec, float maxDist)
    {
        return Vector3.ClampMagnitude(vec , maxDist);
    }
    public static Vector3 ClampToAnchor(this Vector3 vec, Vector3 anchor, float maxDist)
    {
        return anchor + Vector3.ClampMagnitude(vec - anchor, maxDist);
    }
}

public struct ArticulatedIntertia
{
    public lt3x3 M;
    public Matrix3x3 H;
    public lt3x3 I;
 public static ArticulatedIntertia zero
    {
        get
        {
            return new ArticulatedIntertia();
        }
    }
    public ArticulatedIntertia(lt3x3 M, Matrix3x3 H, lt3x3 I)
    {
        this.M = M;
        this.H = H;
        this.I = I;
    }

    public static ArticulatedIntertia operator * (PluckerTranslate t, ArticulatedIntertia I)
    {
        var H = I.H;
        var M = I.M;
        var r = t.r;

     
        // V2. optimized
        var HminusrxM = H-r.Cross(M);
        return new ArticulatedIntertia(M, HminusrxM, I.I-r.LtCrossT(H)+r.LtPreCross(HminusrxM));
            
    }
    public static Vector6 operator *(ArticulatedIntertia IA, Vector6 vec)
    {
        var w = vec.FirstVector3();
        var v = vec.SecondVector3();
        var M = IA.M;
        var H = IA.H;
        var I = IA.I;

        return new Vector6(I * w + H * v, M * v + H.transposed * w);
    }
    public Vector6 TimesAngularAcceleration(Vector3 w)
    {
        return new Vector6(I * w, H.transposed * w);
    }
    public Vector6 TimesLinearAcceleration(Vector3 v)
    {
        return new Vector6(H * v, M * v);
    }
    public static ArticulatedIntertia operator +(ArticulatedIntertia lhs, ArticulatedIntertia rhs)
    {
        return new ArticulatedIntertia(lhs.M + rhs.M, lhs.H + rhs.H, lhs.I + rhs.I);
    }

    public static ArticulatedIntertia FromRigidAtCenter(ReBody body)
    {
        var rot = Matrix3x3.Rotate(body.rotation * body.inertiaTensorRotation);
        var I = body.inertiaTensor;
        var m = body.mass;

        var Imatrix = new Matrix3x3(I.x, 0, 0, 0, I.y, 0, 0, 0, I.z);
        var Ir = rot * Imatrix * rot.transposed;

        return new ArticulatedIntertia(
            lt3x3.Diagonal(m),
            Matrix3x3.zero,
            new lt3x3(Ir));
    }
    public static ArticulatedIntertia FromRigidAtPoint(ReBody body, Vector3 pos)
    {

        var rot = Matrix3x3.Rotate(body.rotation * body.inertiaTensorRotation);
        var I = body.inertiaTensor;
        var m = body.mass;

        var Imatrix = new Matrix3x3(I.x, 0, 0, 0, I.y, 0, 0, 0, I.z);
        var Ir = rot * Imatrix * rot.transposed;

        // return new ArticualtedInertia(
        //     lt3x3.Diagonal(m),
        //     Matrix3x3.zero,
        //     new lt3x3(Ir));


        var r = pos-body.worldCenterOfMass;
        var HminusrxM = -r*m;
        return new ArticulatedIntertia(lt3x3.Diagonal(m), MR.VecToso3(HminusrxM), new lt3x3(Ir)+HminusrxM.LtCrossCross(r));


    }

  
}

//public struct ForceVector6
//{
//    public Vector3 v;
//    public Vector3 w;
//}

public struct PluckerTranslate
{
    public Vector3 r;
    public PluckerTranslate(Vector3 r)
    {
        this.r = r;
    }
    public Vector6 TransformForce(Vector6 vec)
    {
        var n = vec.FirstVector3();
        var f = vec.SecondVector3();

        return new Vector6(n - Vector3.Cross(r,f), f);
    }
    public Vector6 TransformVelocity(Vector6 vec)
    {
        var w = vec.FirstVector3();
        var v = vec.SecondVector3();

        return new Vector6(w,v - Vector3.Cross(r, w) );
    }

    public static void TransformForce(Vector3 r, ref Vector3 n, ref Vector3 f)
    {
        n = n - Vector3.Cross(r, f);
    }
    public static void TransformVelocity(Vector3 r, ref Vector3 w, ref Vector3 v)
    {
        v = v - Vector3.Cross(r, w);
    }
}

//public struct PluckerTransform
//{
//    public Matrix3x3 E;
//    public Vector3 r;

//    public PluckerTransform(Matrix3x3 E, Vector3 r)
//    {
//        this.E = E;
//        this.r = r;
//    }

//    //EME^T
//    public Symmetric3x3 EMET(Symmetric3x3 M)
//    {
//        throw new System.NotImplementedException();
//    }
//    public Matrix3x3 EMET(Matrix3x3 M)
//    {
//        throw new System.NotImplementedException();
//    }
//}
