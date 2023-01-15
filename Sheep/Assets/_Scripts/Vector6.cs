using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vector6
{
    public float v0;
    public float v1;
    public float v2;
    public float v3;
    public float v4;
    public float v5;
    public Vector6(float v0, float v1, float v2, float v3, float v4, float v5)
    {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
        this.v4 = v4;
        this.v5 = v5;
    }
    public Vector6(Vector3 v0, Vector3 v1)
    {
        this.v0 = v0.x;
        this.v1 = v0.y;
        this.v2 = v0.z;
        this.v3 = v1.x;
        this.v4 = v1.y;
        this.v5 = v1.z;
    }
    public float this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return v0;
                case 1: return v1;
                case 2: return v2;
                case 3: return v3;
                case 4: return v4;
                case 5: return v5;

                default:
                    throw new IndexOutOfRangeException("Invalid Vector6 index!");
            }
        }

        set
        {
            switch (index)
            {
                case 0: this.v0 = value; break;
                case 1: this.v1 = value; break;
                case 2: this.v2 = value; break;
                case 3: this.v3 = value; break;
                case 4: this.v4 = value; break;
                case 5: this.v5 = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector6 index!");

            }

        }

    }

    public Vector6 zeroFirst
    {
        get
        {
            return new Vector6(0, 0, 0, v3, v4, v5);
        }
    }
    public Vector6 zeroSecond
    {
        get
        {
            return new Vector6(v0, v1, v2, 0, 0, 0);
        }
    }
    public static Vector6 zero
    {
        get
        {
            return new Vector6(0f, 0f, 0f, 0f, 0f, 0f);
        }
    }
    public static Vector6 one
    {
        get
        {
            return new Vector6(1f, 1f, 1f, 1f, 1f, 1f);
        }
    }

    public Vector3 FirstVector3()
    {
        return new Vector3(v0, v1, v2);
    }
    public Vector3 SecondVector3()
    {
        return new Vector3(v3, v4, v5);
    }
    public Vector3 angular
    {
        get
        {
            return new Vector3(v0, v1, v2);
        }
        set
        {
            v0 = value.x;
            v1 = value.y;
            v2 = value.z;
        }
    }

    public Vector3 linear
    {
        get
        {
            return new Vector3(v3, v4, v5);
        }
        set
        {
            v3 = value.x;
            v4 = value.y;
            v5 = value.z;
        }
    }

    public static Vector6 operator *(float rhs, Vector6 lhs)
    {
        return lhs * rhs;
    }
    public static Vector6 operator *(Vector6 lhs, float rhs)
    {
        return new Vector6(
            lhs.v0 * rhs,
            lhs.v1 * rhs,
            lhs.v2 * rhs,
            lhs.v3 * rhs,
            lhs.v4 * rhs,
            lhs.v5 * rhs
            );

    }
    public static Vector6 operator +(Vector6 lhs, Vector6 rhs)
    {
        return new Vector6(
            lhs.v0 + rhs.v0,
            lhs.v1 + rhs.v1,
            lhs.v2 + rhs.v2,
            lhs.v3 + rhs.v3,
            lhs.v4 + rhs.v4,
            lhs.v5 + rhs.v5
            );
    }
    public static Vector6 operator -(Vector6 lhs, Vector6 rhs)
    {
        return new Vector6(
            lhs.v0 - rhs.v0,
            lhs.v1 - rhs.v1,
            lhs.v2 - rhs.v2,
            lhs.v3 - rhs.v3,
            lhs.v4 - rhs.v4,
            lhs.v5 - rhs.v5
            );
    }
    public static float Dot(Vector6 lhs, Vector6 rhs)
    {
        return
            lhs.v0 * rhs.v0 +
            lhs.v1 * rhs.v1 +
            lhs.v2 * rhs.v2 +
            lhs.v3 * rhs.v3 +
            lhs.v4 * rhs.v4 +
            lhs.v5 * rhs.v5;
            
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
            v0 * v0 +
            v1 * v1 +
            v2 * v2 +
            v3 * v3 +
            v4 * v4 +
            v5 * v5;
        }
    }
    public override string ToString()
    {
        return String.Format("({0:F5}, {1:F5}, {2:F5}, {3:F5}, {4:F5}, {5:F5})", v0, v1, v2, v3, v4, v5);
    }

    public static Vector6 Lerp(Vector6 a, Vector6 b, float t)
    {
        return new Vector6(
            Mathf.Lerp(a.v0,b.v0,t),
            Mathf.Lerp(a.v1,b.v1,t),
            Mathf.Lerp(a.v2,b.v2,t),
            Mathf.Lerp(a.v3,b.v3,t),
            Mathf.Lerp(a.v4,b.v4,t),
            Mathf.Lerp(a.v5,b.v5,t));
    }

}

public static class ClampExtensions
{
    public static Vector6 ClampMagnitude(this Vector6 vec, float magnitude)
    {
        var actual = vec.magnitude;
        if (actual <= magnitude)
            return vec;
        return vec * (magnitude / actual);
    }
    public static Vector6 ClampMagnitude(this Vector6 vec, float mag1, float mag2)
    {
        return new Vector6(
            Vector3.ClampMagnitude(vec.FirstVector3(), mag1),
            Vector3.ClampMagnitude(vec.SecondVector3(), mag2));
    }

    public static Vector6 SoftClampMagnitude(this Vector6 vec, float max1, float max2, float power)
    {
        return new Vector6(
            vec.FirstVector3().SoftClampMagnitude(max1, power),
            vec.SecondVector3().SoftClampMagnitude(max2, power));
    }

    public static Vector3 SoftClampMagnitude(this Vector3 vec, float max, float power)
    {
        if (max == 0)
            return Vector3.zero;
        var sqrMag = vec.sqrMagnitude;
        if (sqrMag <= max*max)
            return vec;
        var mag = Mathf.Sqrt(sqrMag);
        return vec * (Mathf.Pow(mag/ max, power) *max /mag);
    }
    public static Vector3 SoftClampMagnitude(this Vector3 vec, float max, float power, out float scaling)
    {
        if (max == 0)
        {
            scaling = 1;
            return Vector3.zero;
        }
        var sqrMag = vec.sqrMagnitude;
        if (sqrMag <= max * max)
        {
            scaling = 1;
            return vec;
        }
        var mag = Mathf.Sqrt(sqrMag);
        scaling = (Mathf.Pow(mag / max, power) * max / mag);
        return vec * scaling;
    }
    public static float SoftClamp(float val, float max, float power)
    {
        if (max == 0)
            return 0;
        if (val >= -max && val <= max) return val;
        if(val>=0)
            return val * (Mathf.Pow(val / max, power) * max / val);
        else
            return val * (Mathf.Pow(-val / max, power) * max / -val);

    }
}
