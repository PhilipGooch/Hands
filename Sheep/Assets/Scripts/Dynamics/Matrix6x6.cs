using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Matrix6x6
{
    public float m00;
    public float m10;
    public float m20;
    public float m30;
    public float m40;
    public float m50;

    public float m01;
    public float m11;
    public float m21;
    public float m31;
    public float m41;
    public float m51;

    public float m02;
    public float m12;
    public float m22;
    public float m32;
    public float m42;
    public float m52;

    public float m03;
    public float m13;
    public float m23;
    public float m33;
    public float m43;
    public float m53;

    public float m04;
    public float m14;
    public float m24;
    public float m34;
    public float m44;
    public float m54;

    public float m05;
    public float m15;
    public float m25;
    public float m35;
    public float m45;
    public float m55;

    public Matrix6x6(
        float m00, float m01, float m02, float m03, float m04, float m05,
        float m10, float m11, float m12, float m13, float m14, float m15,
        float m20, float m21, float m22, float m23, float m24, float m25,
        float m30, float m31, float m32, float m33, float m34, float m35,
        float m40, float m41, float m42, float m43, float m44, float m45,
        float m50, float m51, float m52, float m53, float m54, float m55
        )
    {
        this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03; this.m04 = m04; this.m05 = m05;
        this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13; this.m14 = m14; this.m15 = m15; 
        this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23; this.m24 = m24; this.m25 = m25; 
        this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33; this.m34 = m34; this.m35 = m35; 
        this.m40 = m40; this.m41 = m41; this.m42 = m42; this.m43 = m43; this.m44 = m44; this.m45 = m45; 
        this.m50 = m50; this.m51 = m51; this.m52 = m52; this.m53 = m53; this.m54 = m54; this.m55 = m55; 
    }

    public Matrix6x6(
        Matrix3x3 M00, Matrix3x3 M01,
        Matrix3x3 M10, Matrix3x3 M11)
    {
        this.m00 = M00.m00; this.m01 = M00.m01; this.m02 = M00.m02; this.m03 = M01.m00; this.m04 = M01.m01; this.m05 = M01.m02;
        this.m10 = M00.m10; this.m11 = M00.m11; this.m12 = M00.m12; this.m13 = M01.m10; this.m14 = M01.m11; this.m15 = M01.m12;
        this.m20 = M00.m20; this.m21 = M00.m21; this.m22 = M00.m22; this.m23 = M01.m20; this.m24 = M01.m21; this.m25 = M01.m22;
        this.m30 = M10.m00; this.m31 = M10.m01; this.m32 = M10.m02; this.m33 = M11.m00; this.m34 = M11.m01; this.m35 = M11.m02;
        this.m40 = M10.m10; this.m41 = M10.m11; this.m42 = M10.m12; this.m43 = M11.m10; this.m44 = M11.m11; this.m45 = M11.m12;
        this.m50 = M10.m20; this.m51 = M10.m21; this.m52 = M10.m22; this.m53 = M11.m20; this.m54 = M11.m21; this.m55 = M11.m22;
    }
    public Matrix6x6 transposed
    {
        get
        {
            return new Matrix6x6(
                m00, m10, m20, m30, m40, m50,
                m01, m11, m21, m31, m41, m51,
                m02, m12, m22, m32, m42, m52,
                m03, m13, m23, m33, m43, m53,
                m04, m14, m24, m34, m44, m54,
                m05, m15, m25, m35, m45, m55
            );
        }
    }
    public static Matrix6x6 Diagonal(float v0, float v1, float v2, float v3, float v4, float v5)
    {
        return new Matrix6x6(
            v0, 0, 0, 0, 0, 0,
            0, v1, 0, 0, 0, 0,
            0, 0, v2, 0, 0, 0,
            0, 0, 0, v3, 0, 0,
            0, 0, 0, 0, v4, 0,
            0, 0, 0, 0, 0, v5
            );

    }
    public static Matrix6x6 operator -(Matrix6x6 rhs)
    {
        return new Matrix6x6(
                    -rhs.m00, -rhs.m01, -rhs.m02, -rhs.m03, -rhs.m04, -rhs.m05,
                    -rhs.m10, -rhs.m11, -rhs.m12, -rhs.m13, -rhs.m14, -rhs.m15,
                    -rhs.m20, -rhs.m21, -rhs.m22, -rhs.m23, -rhs.m24, -rhs.m25,
                    -rhs.m30, -rhs.m31, -rhs.m32, -rhs.m33, -rhs.m34, -rhs.m35,
                    -rhs.m40, -rhs.m41, -rhs.m42, -rhs.m43, -rhs.m44, -rhs.m45,
                    -rhs.m50, -rhs.m51, -rhs.m52, -rhs.m53, -rhs.m54, -rhs.m55
            );
    }
    // Subtract two matrices.
    public static Matrix6x6 operator -(Matrix6x6 lhs, Matrix6x6 rhs)
    {
        return new Matrix6x6(
                    lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m02 - rhs.m02, lhs.m03 - rhs.m03, lhs.m04 - rhs.m04, lhs.m05 - rhs.m05,
                    lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m12 - rhs.m12, lhs.m13 - rhs.m13, lhs.m14 - rhs.m14, lhs.m15 - rhs.m15,
                    lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22, lhs.m23 - rhs.m23, lhs.m24 - rhs.m24, lhs.m25 - rhs.m25,
                    lhs.m30 - rhs.m30, lhs.m31 - rhs.m31, lhs.m32 - rhs.m32, lhs.m33 - rhs.m33, lhs.m34 - rhs.m34, lhs.m35 - rhs.m35,
                    lhs.m40 - rhs.m40, lhs.m41 - rhs.m41, lhs.m42 - rhs.m42, lhs.m43 - rhs.m43, lhs.m44 - rhs.m44, lhs.m45 - rhs.m45,
                    lhs.m50 - rhs.m50, lhs.m51 - rhs.m51, lhs.m52 - rhs.m52, lhs.m53 - rhs.m53, lhs.m54 - rhs.m54, lhs.m55 - rhs.m55
            );
    }
    public static Matrix6x6 operator +(Matrix6x6 lhs, Matrix6x6 rhs)
    {
        return new Matrix6x6(
                    lhs.m00 * rhs.m00, lhs.m01 * rhs.m01, lhs.m02 * rhs.m02, lhs.m03 * rhs.m03, lhs.m04 * rhs.m04, lhs.m05 * rhs.m05,
                    lhs.m10 * rhs.m10, lhs.m11 * rhs.m11, lhs.m12 * rhs.m12, lhs.m13 * rhs.m13, lhs.m14 * rhs.m14, lhs.m15 * rhs.m15,
                    lhs.m20 * rhs.m20, lhs.m21 * rhs.m21, lhs.m22 * rhs.m22, lhs.m23 * rhs.m23, lhs.m24 * rhs.m24, lhs.m25 * rhs.m25,
                    lhs.m30 * rhs.m30, lhs.m31 * rhs.m31, lhs.m32 * rhs.m32, lhs.m33 * rhs.m33, lhs.m34 * rhs.m34, lhs.m35 * rhs.m35,
                    lhs.m40 * rhs.m40, lhs.m41 * rhs.m41, lhs.m42 * rhs.m42, lhs.m43 * rhs.m43, lhs.m44 * rhs.m44, lhs.m45 * rhs.m45,
                    lhs.m50 * rhs.m50, lhs.m51 * rhs.m51, lhs.m52 * rhs.m52, lhs.m53 * rhs.m53, lhs.m54 * rhs.m54, lhs.m55 * rhs.m55
            );
    }
    public static Matrix6x6 operator *(Matrix6x6 lhs, Matrix6x6 rhs)
    {
        Matrix6x6 res;
        res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30 + lhs.m04 * rhs.m40 + lhs.m05 * rhs.m50;
        res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31 + lhs.m04 * rhs.m41 + lhs.m05 * rhs.m51;
        res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32 + lhs.m04 * rhs.m42 + lhs.m05 * rhs.m52;
        res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33 + lhs.m04 * rhs.m43 + lhs.m05 * rhs.m53;
        res.m04 = lhs.m00 * rhs.m04 + lhs.m01 * rhs.m14 + lhs.m02 * rhs.m24 + lhs.m03 * rhs.m34 + lhs.m04 * rhs.m44 + lhs.m05 * rhs.m54;
        res.m05 = lhs.m00 * rhs.m05 + lhs.m01 * rhs.m15 + lhs.m02 * rhs.m25 + lhs.m03 * rhs.m35 + lhs.m04 * rhs.m45 + lhs.m05 * rhs.m55;

        res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30 + lhs.m14 * rhs.m40 + lhs.m15 * rhs.m50;
        res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31 + lhs.m14 * rhs.m41 + lhs.m15 * rhs.m51;
        res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32 + lhs.m14 * rhs.m42 + lhs.m15 * rhs.m52;
        res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33 + lhs.m14 * rhs.m43 + lhs.m15 * rhs.m53;
        res.m14 = lhs.m10 * rhs.m04 + lhs.m11 * rhs.m14 + lhs.m12 * rhs.m24 + lhs.m13 * rhs.m34 + lhs.m14 * rhs.m44 + lhs.m15 * rhs.m54;
        res.m15 = lhs.m10 * rhs.m05 + lhs.m11 * rhs.m15 + lhs.m12 * rhs.m25 + lhs.m13 * rhs.m35 + lhs.m14 * rhs.m45 + lhs.m15 * rhs.m55;

        res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30 + lhs.m24 * rhs.m40 + lhs.m25 * rhs.m50;
        res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31 + lhs.m24 * rhs.m41 + lhs.m25 * rhs.m51;
        res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32 + lhs.m24 * rhs.m42 + lhs.m25 * rhs.m52;
        res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33 + lhs.m24 * rhs.m43 + lhs.m25 * rhs.m53;
        res.m24 = lhs.m20 * rhs.m04 + lhs.m21 * rhs.m14 + lhs.m22 * rhs.m24 + lhs.m23 * rhs.m34 + lhs.m24 * rhs.m44 + lhs.m25 * rhs.m54;
        res.m25 = lhs.m20 * rhs.m05 + lhs.m21 * rhs.m15 + lhs.m22 * rhs.m25 + lhs.m23 * rhs.m35 + lhs.m24 * rhs.m45 + lhs.m25 * rhs.m55;

        res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30 + lhs.m34 * rhs.m40 + lhs.m35 * rhs.m50;
        res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31 + lhs.m34 * rhs.m41 + lhs.m35 * rhs.m51;
        res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32 + lhs.m34 * rhs.m42 + lhs.m35 * rhs.m52;
        res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33 + lhs.m34 * rhs.m43 + lhs.m35 * rhs.m53;
        res.m34 = lhs.m30 * rhs.m04 + lhs.m31 * rhs.m14 + lhs.m32 * rhs.m24 + lhs.m33 * rhs.m34 + lhs.m34 * rhs.m44 + lhs.m35 * rhs.m54;
        res.m35 = lhs.m30 * rhs.m05 + lhs.m31 * rhs.m15 + lhs.m32 * rhs.m25 + lhs.m33 * rhs.m35 + lhs.m34 * rhs.m45 + lhs.m35 * rhs.m55;

        res.m40 = lhs.m40 * rhs.m00 + lhs.m41 * rhs.m10 + lhs.m42 * rhs.m20 + lhs.m43 * rhs.m30 + lhs.m44 * rhs.m40 + lhs.m45 * rhs.m50;
        res.m41 = lhs.m40 * rhs.m01 + lhs.m41 * rhs.m11 + lhs.m42 * rhs.m21 + lhs.m43 * rhs.m31 + lhs.m44 * rhs.m41 + lhs.m45 * rhs.m51;
        res.m42 = lhs.m40 * rhs.m02 + lhs.m41 * rhs.m12 + lhs.m42 * rhs.m22 + lhs.m43 * rhs.m32 + lhs.m44 * rhs.m42 + lhs.m45 * rhs.m52;
        res.m43 = lhs.m40 * rhs.m03 + lhs.m41 * rhs.m13 + lhs.m42 * rhs.m23 + lhs.m43 * rhs.m33 + lhs.m44 * rhs.m43 + lhs.m45 * rhs.m53;
        res.m44 = lhs.m40 * rhs.m04 + lhs.m41 * rhs.m14 + lhs.m42 * rhs.m24 + lhs.m43 * rhs.m34 + lhs.m44 * rhs.m44 + lhs.m45 * rhs.m54;
        res.m45 = lhs.m40 * rhs.m05 + lhs.m41 * rhs.m15 + lhs.m42 * rhs.m25 + lhs.m43 * rhs.m35 + lhs.m44 * rhs.m45 + lhs.m45 * rhs.m55;

        res.m50 = lhs.m50 * rhs.m00 + lhs.m51 * rhs.m10 + lhs.m52 * rhs.m20 + lhs.m53 * rhs.m30 + lhs.m54 * rhs.m40 + lhs.m55 * rhs.m50;
        res.m51 = lhs.m50 * rhs.m01 + lhs.m51 * rhs.m11 + lhs.m52 * rhs.m21 + lhs.m53 * rhs.m31 + lhs.m54 * rhs.m41 + lhs.m55 * rhs.m51;
        res.m52 = lhs.m50 * rhs.m02 + lhs.m51 * rhs.m12 + lhs.m52 * rhs.m22 + lhs.m53 * rhs.m32 + lhs.m54 * rhs.m42 + lhs.m55 * rhs.m52;
        res.m53 = lhs.m50 * rhs.m03 + lhs.m51 * rhs.m13 + lhs.m52 * rhs.m23 + lhs.m53 * rhs.m33 + lhs.m54 * rhs.m43 + lhs.m55 * rhs.m53;
        res.m54 = lhs.m50 * rhs.m04 + lhs.m51 * rhs.m14 + lhs.m52 * rhs.m24 + lhs.m53 * rhs.m34 + lhs.m54 * rhs.m44 + lhs.m55 * rhs.m54;
        res.m55 = lhs.m50 * rhs.m05 + lhs.m51 * rhs.m15 + lhs.m52 * rhs.m25 + lhs.m53 * rhs.m35 + lhs.m54 * rhs.m45 + lhs.m55 * rhs.m55;

        return res;
    }

    public float magnitude
    {
        get
        {
            return Mathf.Sqrt(sqrMagnitude);
        }
    }
    public float sqrMagnitude

    {
        get
        {
            return
                        m00 * m00 + m01 * m01 + m02 * m02 + m03 * m03 + m04 * m04 + m05 * m05 +
                        m10 * m10 + m11 * m11 + m12 * m12 + m13 * m13 + m14 * m14 + m15 * m15 +
                        m20 * m20 + m21 * m21 + m22 * m22 + m23 * m23 + m24 * m24 + m25 * m25 +
                        m30 * m30 + m31 * m31 + m32 * m32 + m33 * m33 + m34 * m34 + m35 * m35 +
                        m40 * m40 + m41 * m41 + m42 * m42 + m43 * m43 + m44 * m44 + m45 * m45 +
                        m50 * m50 + m51 * m51 + m52 * m52 + m53 * m53 + m54 * m54 + m55 * m55;

        }
    }
    // Transforms a [[Vector3]] by a matrix.
    public static Vector6 operator *(Matrix6x6 lhs, Vector6 vector)
    {
        Vector6 res;
        res.v0 = lhs.m00 * vector.v0 + lhs.m01 * vector.v1 + lhs.m02 * vector.v2 + lhs.m03 * vector.v3 + lhs.m04 * vector.v4 + lhs.m05 * vector.v5;
        res.v1 = lhs.m10 * vector.v0 + lhs.m11 * vector.v1 + lhs.m12 * vector.v2 + lhs.m13 * vector.v3 + lhs.m14 * vector.v4 + lhs.m15 * vector.v5;
        res.v2 = lhs.m20 * vector.v0 + lhs.m21 * vector.v1 + lhs.m22 * vector.v2 + lhs.m23 * vector.v3 + lhs.m24 * vector.v4 + lhs.m25 * vector.v5;
        res.v3 = lhs.m30 * vector.v0 + lhs.m31 * vector.v1 + lhs.m32 * vector.v2 + lhs.m33 * vector.v3 + lhs.m34 * vector.v4 + lhs.m35 * vector.v5;
        res.v4 = lhs.m40 * vector.v0 + lhs.m41 * vector.v1 + lhs.m42 * vector.v2 + lhs.m43 * vector.v3 + lhs.m44 * vector.v4 + lhs.m45 * vector.v5;
        res.v5 = lhs.m50 * vector.v0 + lhs.m51 * vector.v1 + lhs.m52 * vector.v2 + lhs.m53 * vector.v3 + lhs.m54 * vector.v4 + lhs.m55 * vector.v5;
        return res;
    }
    public override string ToString()
    {
        return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n",
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m00, m01, m02, m03, m04, m05),
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m10, m11, m12, m13, m14, m15),
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m20, m21, m22, m23, m24, m25),
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m30, m31, m32, m33, m34, m35),
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m40, m41, m42, m43, m44, m45),
            String.Format("{0:F5}\t{1:F5}\t{2:F5}\t{3:F5}\t{4:F5}\t{5:F5}", m50, m51, m52, m53, m54, m55));
    }

    //public override string ToString()
    //{
    //    return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n",
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m00, m01, m02, m03, m04, m05),
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m10, m11, m12, m13, m14, m15),
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m20, m21, m22, m23, m24, m25),
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m30, m31, m32, m33, m34, m35),
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m40, m41, m42, m43, m44, m45),
    //        String.Format("{0:E}\t{1:E}\t{2:E}\t{3:E}\t{4:E}\t{5:E}", m50, m51, m52, m53, m54, m55));
    //}
}
