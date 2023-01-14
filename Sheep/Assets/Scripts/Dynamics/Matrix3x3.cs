using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Matrix4x4Util
{
    public static Matrix4x4 Create(float m00, float m01, float m02, float m03,
        float m10, float m11, float m12, float m13,
        float m20, float m21, float m22, float m23,
        float m30, float m31, float m32, float m33)
    {
        var res = new Matrix4x4();
        res.m00 = m00; res.m01 = m01; res.m02 = m02; res.m03 = m03;
        res.m10 = m10; res.m11 = m11; res.m12 = m12; res.m13 = m13;
        res.m20 = m20; res.m21 = m21; res.m22 = m22; res.m23 = m23;
        res.m30 = m30; res.m31 = m31; res.m32 = m32; res.m33 = m33;
        return res;
    }
    public static Matrix4x4 Create(Matrix3x3 R, Vector3 p, float m30,float m31,float m32, float m33)
    {
        var res = new Matrix4x4();
        res.m00 = R.m00; res.m01 = R.m01; res.m02 = R.m02; res.m03 = p.x;
        res.m10 = R.m10; res.m11 = R.m11; res.m12 = R.m12; res.m13 = p.y;
        res.m20 = R.m20; res.m21 = R.m21; res.m22 = R.m22; res.m23 = p.z;
        res.m30 = m30; res.m31 = m31; res.m32 = m32; res.m33 = m33;
        return res;
    }
    public static Matrix4x4 Sub(Matrix4x4 lhs, Matrix4x4 rhs)
    {
        var res = new Matrix4x4();
        res.m00 = lhs.m00 - rhs.m00; res.m01 = lhs.m01 - rhs.m01; res.m02 = lhs.m02 - rhs.m02; res.m03 = lhs.m03 - rhs.m03;
        res.m10 = lhs.m10 - rhs.m10; res.m11 = lhs.m11 - rhs.m11; res.m12 = lhs.m12 - rhs.m12; res.m13 = lhs.m13 - rhs.m13;
        res.m20 = lhs.m20 - rhs.m20; res.m21 = lhs.m21 - rhs.m21; res.m22 = lhs.m22 - rhs.m22; res.m23 = lhs.m23 - rhs.m23;
        res.m30 = lhs.m30 - rhs.m30; res.m31 = lhs.m31 - rhs.m31; res.m32 = lhs.m32 - rhs.m32; res.m33 = lhs.m33 - rhs.m33;
        return res;
    }
    public static float Magnitude(this Matrix4x4 lhs)
    {
        return Mathf.Sqrt(SqrMagnitude(lhs));
    }
    public static float SqrMagnitude(this Matrix4x4 lhs)
    {
        return
         lhs.m00 * lhs.m00 + lhs.m01 * lhs.m01 + lhs.m02 * lhs.m02 + lhs.m03 * lhs.m03 +
         lhs.m10 * lhs.m10 + lhs.m11 * lhs.m11 + lhs.m12 * lhs.m12 + lhs.m13 * lhs.m13 +
         lhs.m20 * lhs.m20 + lhs.m21 * lhs.m21 + lhs.m22 * lhs.m22 + lhs.m23 * lhs.m23 +
         lhs.m30 * lhs.m30 + lhs.m31 * lhs.m31 + lhs.m32 * lhs.m32 + lhs.m33 * lhs.m33;
    }
    public static Matrix3x3 ToMatrix3x3(this Matrix4x4 m)
    {
        return new Matrix3x3(
            m.m00, m.m01, m.m02,
            m.m10, m.m11, m.m12,
            m.m20, m.m21, m.m22);
    }
    public static Vector3 Right(this Matrix4x4 m)
    {
        return new Vector3(m.m00,m.m01,m.m02);
    }
    public static Vector3 Up(this Matrix4x4 m)
    {
        return new Vector3(m.m10, m.m11, m.m12);
    }
    public static Vector3 Forward(this Matrix4x4 m)
    {
        return new Vector3(m.m20, m.m21, m.m22);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Matrix3x3
{
    public float m00;
    public float m10;
    public float m20;
    public float m01;
    public float m11;
    public float m21;
    public float m02;
    public float m12;
    public float m22;

    public Matrix3x3(float m00, float m01, float m02,
        float m10, float m11, float m12,
        float m20, float m21, float m22)
    {
        this.m00 = m00; this.m01 = m01; this.m02 = m02;
        this.m10 = m10; this.m11 = m11; this.m12 = m12;
        this.m20 = m20; this.m21 = m21; this.m22 = m22;
    }


    public Matrix3x3(Vector3 column0, Vector3 column1, Vector3 column2)
    {
        this.m00 = column0.x; this.m01 = column1.x; this.m02 = column2.x; 
        this.m10 = column0.y; this.m11 = column1.y; this.m12 = column2.y; 
        this.m20 = column0.z; this.m21 = column1.z; this.m22 = column2.z; 
    }

    // Access element at [row, column].
    public float this[int row, int column]
    {
        get
        {
            return this[row + column * 3];
        }


        set
        {
            this[row + column * 3] = value;
        }
    }

    internal static Matrix3x3 Diagonal(float v)
    {
        return new Matrix3x3(v, 0, 0, 0, v, 0, 0, 0, v);
        
    }

    

    // Access element at sequential index (0..8 inclusive).
    public float this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return m00;
                case 1: return m10;
                case 2: return m20;
                case 3: return m01;
                case 4: return m11;
                case 5: return m21;
                case 6: return m02;
                case 7: return m12;
                case 8: return m22;
                default:
                    throw new IndexOutOfRangeException("Invalid matrix index!");
            }
        }

        set
        {
            switch (index)
            {
                case 0: m00 = value; break;
                case 1: m10 = value; break;
                case 2: m20 = value; break;
                case 4: m01 = value; break;
                case 5: m11 = value; break;
                case 6: m21 = value; break;
                case 8: m02 = value; break;
                case 9: m12 = value; break;
                case 10: m22 = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid matrix index!");
            }
        }
    }

    public override int GetHashCode()
    {
        return GetColumn(0).GetHashCode() ^ (GetColumn(1).GetHashCode() << 2) ^ (GetColumn(2).GetHashCode() >> 2) ;
    }
    public override bool Equals(object other)
    {
        if (!(other is Matrix3x3)) return false;

        return Equals((Matrix3x3)other);
    }

    public bool Equals(Matrix3x3 other)
    {
        return GetColumn(0).Equals(other.GetColumn(0))
            && GetColumn(1).Equals(other.GetColumn(1))
            && GetColumn(2).Equals(other.GetColumn(2));
    }

    public Matrix3x3 transposed
    {
        get
        {
            return new Matrix3x3(
                m00, m10, m20,
                m01, m11, m21,
                m02, m12, m22
            );
        }
    }

    // Multiplies two matrices.
    public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
        res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
        res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22;

        res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
        res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
        res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22;

        res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
        res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
        res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22;

        return res;
    }
    // Add two matrices.
    public static Matrix3x3 operator +(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 + rhs.m00;
        res.m01 = lhs.m01 + rhs.m01;
        res.m02 = lhs.m02 + rhs.m02;

        res.m10 = lhs.m10 + rhs.m10;
        res.m11 = lhs.m11 + rhs.m11;
        res.m12 = lhs.m12 + rhs.m12;

        res.m20 = lhs.m20 + rhs.m20;
        res.m21 = lhs.m21 + rhs.m21;
        res.m22 = lhs.m22 + rhs.m22;

        return res;
    }
    // Subtract two matrices.
    public static Matrix3x3 operator -(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 - rhs.m00;
        res.m01 = lhs.m01 - rhs.m01;
        res.m02 = lhs.m02 - rhs.m02;

        res.m10 = lhs.m10 - rhs.m10;
        res.m11 = lhs.m11 - rhs.m11;
        res.m12 = lhs.m12 - rhs.m12;

        res.m20 = lhs.m20 - rhs.m20;
        res.m21 = lhs.m21 - rhs.m21;
        res.m22 = lhs.m22 - rhs.m22;

        return res;
    }
    // Subtract two matrices.
    public static Matrix3x3 operator -(Matrix3x3 rhs)
    {
        Matrix3x3 res;
        res.m00 = - rhs.m00;
        res.m01 = - rhs.m01;
        res.m02 = - rhs.m02;

        res.m10 = - rhs.m10;
        res.m11 = - rhs.m11;
        res.m12 = - rhs.m12;

        res.m20 = - rhs.m20;
        res.m21 = - rhs.m21;
        res.m22 = - rhs.m22;

        return res;
    }
    // Length of the matrix.
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
          + m00 * m00
          + m01 * m01
          + m02 * m02

          + m10 * m10
          + m11 * m11
          + m12 * m12

          + m20 * m20
          + m21 * m21
          + m22 * m22;

        }
    }
    // Transforms a [[Vector3]] by a matrix.
    public static Vector3 operator *(Matrix3x3 lhs, Vector3 vector)
    {
        Vector3 res;
        res.x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z;
        res.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z;
        res.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z;
        return res;
    }
    public static Matrix3x3 operator *( float rhs, Matrix3x3 lhs)
    {
        return lhs * rhs;
    }
    public static Matrix3x3 operator *(Matrix3x3 lhs, float rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 * rhs;
        res.m01 = lhs.m01 * rhs;
        res.m02 = lhs.m02 * rhs;

        res.m10 = lhs.m10 * rhs;
        res.m11 = lhs.m11 * rhs;
        res.m12 = lhs.m12 * rhs;

        res.m20 = lhs.m20 * rhs;
        res.m21 = lhs.m21 * rhs;
        res.m22 = lhs.m22 * rhs;
        return res;
    }
    public static Matrix3x3 operator /(Matrix3x3 lhs, float rhs)
    {
        Matrix3x3 res;
        res.m00 = lhs.m00 / rhs;
        res.m01 = lhs.m01 / rhs;
        res.m02 = lhs.m02 / rhs;

        res.m10 = lhs.m10 / rhs;
        res.m11 = lhs.m11 / rhs;
        res.m12 = lhs.m12 / rhs;

        res.m20 = lhs.m20 / rhs;
        res.m21 = lhs.m21 / rhs;
        res.m22 = lhs.m22 / rhs;
        return res;
    }
    public static bool operator ==(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        // Returns false in the presence of NaN values.
        return lhs.GetColumn(0) == rhs.GetColumn(0)
            && lhs.GetColumn(1) == rhs.GetColumn(1)
            && lhs.GetColumn(2) == rhs.GetColumn(2);
    }

    public static bool operator !=(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        // Returns true in the presence of NaN values.
        return !(lhs == rhs);
    }

    // Get a column of the matrix.
    public Vector3 GetColumn(int index)
    {
        switch (index)
        {
            case 0: return new Vector3(m00, m10, m20);
            case 1: return new Vector3(m01, m11, m21);
            case 2: return new Vector3(m02, m12, m22);
            default:
                throw new IndexOutOfRangeException("Invalid column index!");
        }
    }


    // Returns a row of the matrix.
    public Vector3 GetRow(int index)
    {
        switch (index)
        {
            case 0: return new Vector3(m00, m01, m02);
            case 1: return new Vector3(m10, m11, m12);
            case 2: return new Vector3(m20, m21, m22);
            default:
                throw new IndexOutOfRangeException("Invalid row index!");
        }
    }

    // Sets a column of the matrix.
    public void SetColumn(int index, Vector3 column)
    {
        this[0, index] = column.x;
        this[1, index] = column.y;
        this[2, index] = column.z;
    }

    // Sets a row of the matrix.
    public void SetRow(int index, Vector3 row)
    {
        this[index, 0] = row.x;
        this[index, 1] = row.y;
        this[index, 2] = row.z;
    }

    // Transforms a direction by this matrix.

    public Vector3 MultiplyVector(Vector3 vector)
    {
        Vector3 res;
        res.x = this.m00 * vector.x + this.m01 * vector.y + this.m02 * vector.z;
        res.y = this.m10 * vector.x + this.m11 * vector.y + this.m12 * vector.z;
        res.z = this.m20 * vector.x + this.m21 * vector.y + this.m22 * vector.z;
        return res;
    }

    // Creates a rotation matrix. Note: Assumes unit quaternion
    public static Matrix3x3 Rotate(Quaternion q)
    {
        // Precalculate coordinate products
        float x = q.x * 2.0F;
        float y = q.y * 2.0F;
        float z = q.z * 2.0F;
        float xx = q.x * x;
        float yy = q.y * y;
        float zz = q.z * z;
        float xy = q.x * y;
        float xz = q.x * z;
        float yz = q.y * z;
        float wx = q.w * x;
        float wy = q.w * y;
        float wz = q.w * z;

        // Calculate 3x3 matrix from orthonormal basis
        Matrix3x3 m;
        m.m00 = 1.0f - (yy + zz); m.m10 = xy + wz; m.m20 = xz - wy;
        m.m01 = xy - wz; m.m11 = 1.0f - (xx + zz); m.m21 = yz + wx;
        m.m02 = xz + wy; m.m12 = yz - wx; m.m22 = 1.0f - (xx + yy);
        return m;
    }

    public Matrix3x3 inverse
    {
         get
        {
            var DET = m00 * (m22 * m11 - m21 * m12)
                    - m10 * (m22 * m01 - m21 * m02)
                    + m20 * (m12 * m01 - m11 * m02);

            var e = 0.0000001f;
            if (-e<DET && DET < e)
                throw new System.InvalidOperationException("Can't calculate inverse for matrix with 0 D");
            var invD = 1/DET;

            return new Matrix3x3(
                (m11 * m22 - m12 * m21) * invD,-(m10 * m22 - m12 * m20) * invD, (m10 * m21 - m11 * m20) * invD,
               -(m01 * m22 - m02 * m21) * invD, (m00 * m22 - m02 * m20) * invD,-(m00 * m21 - m01 * m20) * invD,
                (m01 * m12 - m02 * m11) * invD,-(m00 * m12 - m02 * m10) * invD, (m00 * m11 - m01 * m10) * invD
            );

        }
    }

    // Matrix4x4.zero is of questionable usefulness considering C# sets everything to 0 by default, however:
    //  1. it's consistent with other Math structs in Unity such as Vector2, Vector3 and Vector4,
    //  2. "Matrix4x4.zero" is arguably more readable than "new Matrix4x4()",
    //  3. it's already in the API ..
    static readonly Matrix3x3 zeroMatrix = new Matrix3x3(new Vector3(0, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 0));

    // Returns a matrix with all elements set to zero (RO).
    public static Matrix3x3 zero { get { return zeroMatrix; } }

    static readonly Matrix3x3 identityMatrix = new Matrix3x3(new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1));

    // Returns the identity matrix (RO).
    public static Matrix3x3 identity { get { return identityMatrix; } }

    public override string ToString()
    {
        return String.Format("{0:F5}\t{1:F5}\t{2:F5}\n{3:F5}\t{4:F5}\t{5:F5}\n{6:F5}\t{7:F5}\t{8:F5}\n", m00, m01, m02, m10, m11, m12, m20, m21, m22);
    }

    public string ToString(string format)
    {
        return String.Format("{0}\t{1}\t{2}\n{3}\t{4}\t{5}\n{6}\t{7}\t{8}\n",
            m00.ToString(format), m01.ToString(format), m02.ToString(format),
            m10.ToString(format), m11.ToString(format), m12.ToString(format),
            m20.ToString(format), m21.ToString(format), m22.ToString(format));

    }
}
