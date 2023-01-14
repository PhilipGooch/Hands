using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Transformation
{
    public Vector3 pos;
    public Quaternion rot;

    public Transformation(Vector3 pos, Quaternion rot) 
    {
        this.pos = pos;
        this.rot = rot;
    }

    public Transformation inverse
    {
        get
        {
            var rotInv = Quaternion.Inverse(rot);
            return new Transformation(-(rotInv*pos), rotInv);
        }
    }

    static readonly Transformation identityTransformation = new Transformation(Vector3.zero, Quaternion.identity);
    public static Transformation identity { get { return identityTransformation; } }

    public static Transformation RotationAroundAxis(Vector3 origin, float angle, Vector3 axis)
    {
        var r = Quaternion.AngleAxis(angle, axis);
        return new Transformation(origin- (r * origin), r);
    }
    public static Transformation RotationAroundPoint(Vector3 origin, Quaternion r)
    {
        return new Transformation(origin - (r * origin), r);
    }
    public static Transformation Translation(Vector3 offset)
    {
        return new Transformation(offset,Quaternion.identity);
    }
    public static Transformation operator *(Transformation lhs, Transformation rhs)
    {
        return new Transformation(lhs.pos+lhs.rot*rhs.pos,lhs.rot*rhs.rot);
    }
    public static Vector3 operator *(Transformation lhs, Vector3 rhs)
    {
        return lhs.pos + lhs.rot * rhs;
    }
    public Transformation RotateAroundAxis(Vector3 origin, float angle, Vector3 axis)
    {
        var r = Quaternion.AngleAxis(angle, axis);
        return new Transformation(pos+origin-r*origin, r*rot);
    }
    public Transformation RotateAroundPoint(Vector3 origin, Quaternion r)
    {
        return new Transformation(pos + origin - r * origin, r * rot);
    }

}