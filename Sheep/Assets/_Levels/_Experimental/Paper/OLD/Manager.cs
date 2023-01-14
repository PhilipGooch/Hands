using UnityEditor;
using UnityEngine;
using NBG.Core;

[ExecuteInEditMode]
public class Manager : MonoBehaviour, ISerializationCallbackReceiver
{
    public static Vector3 LeftPoint;
    public static Vector3 RightPoint;
    public static Vector3 CenterPoint;
    public static Vector3 IntersectionPoint;
    public static float LeftMagnitude;
    public static float RightMagnitude;
    public static float CenterMagnitude;

    //public GameObject leftSphere;
    //public GameObject rightSphere;
    //public GameObject centerSphere;
    //public GameObject tragectoryCircle;

    [SerializeField]
    [Range(1, 179)]
    float theta;

    [SerializeField]
    bool v;

    public static float Theta;

    [SerializeField]
    bool refresh;

    bool repainted;

    float w1 = 6;
    float h1 = 4;
    float w2 = 2;
    float h2 = 8;
    float phi;
    float a1, b1, a2, b2;
    float x, y, d, dSqrd;
    Vector2 a1b1;
    Vector2 a2b2;
    Vector2 a2b2a1b1Norm;
    Vector2 a2b2a1b1PerpNorm;
    Vector2 xy;
    Vector2 XY;
    public static Vector3 a1b13 = Vector3.zero;
    public static Vector3 a2b23 = Vector3.zero;
    public static Vector3 xy3 = Vector3.zero;
    public static Vector3 XY3 = Vector3.zero;

    public Vector3 l;
    public Vector3 r;
    public Vector3 c;
    public float lm;
    public float rm;
    public float cm;
    public float h;
    Vector3 p = Vector3.zero;
    bool collision;

    public Transform L;

    public void CalculateParallelPoints()
    {
        phi = Mathf.PI - (Theta * Mathf.Deg2Rad);
        a1 = Mathf.Cos(Mathf.PI - phi) * w1;
        b1 = Mathf.Sin(Mathf.PI - phi) * w1;
        a2 = Mathf.Cos(0) * w2;
        b2 = Mathf.Sin(0) * w2;
        a1b1 = new Vector2(a1, b1);
        a2b2 = new Vector2(a2, b2);
        a2b2a1b1Norm = (a1b1 - a2b2).normalized;
        dSqrd = Mathf.Pow(a1 - a2, 2) + Mathf.Pow(b1 - b2, 2);
        d = Mathf.Sqrt(dSqrd);
        x = (dSqrd + Mathf.Pow(h2, 2) - Mathf.Pow(h1, 2)) / (2 * d);
        xy = a2b2 + (a2b2a1b1Norm * x);
        y = (1 / (2 * d)) * Mathf.Sqrt((-d + h1 - h2) * (-d - h1 + h2) * (-d + h1 + h2) * (d + h1 + h2));
        a2b2a1b1PerpNorm = -Vector2.Perpendicular(a2b2a1b1Norm).normalized;
        XY = xy + a2b2a1b1PerpNorm * y;
        a1b13 = new Vector3(a1b1.x, a1b1.y, 0);
        a2b23 = new Vector3(a2b2.x, a2b2.y, 0);
        xy3 = new Vector3(xy.x, xy.y, 0);
        XY3 = new Vector3(XY.x, XY.y, 0);
    }

    Vector3 Trilaterate(Vector3 p1, Vector3 p2, Vector3 p3, float r1, float r2, float r3, out bool collision)
    {
        Vector3 temp1 = p2 - p1;
        Vector3 e_x = temp1 / temp1.magnitude;
        Vector3 temp2 = p3 - p1;
        float i = Vector3.Dot(e_x, temp2);
        Vector3 temp3 = temp2 - i * e_x;
        Vector3 e_y = temp3 / temp3.magnitude;
        Vector3 e_z = Vector3.Cross(e_x, e_y);
        float d = (p2 - p1).magnitude;
        float j = Vector3.Dot(e_y, temp2);
        float x = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
        float y = (r1 * r1 - r3 * r3 - 2 * i * x + i * i + j * j) / (2 * j);
        float temp4 = r1 * r1 - x * x - y * y;
        if (temp4 < 0)
        {
            collision = false;
            return Vector3.zero;
        }
        float z = Mathf.Sqrt(temp4);
        Vector3 p_12_a = p1 + x * e_x + y * e_y + z * e_z;
        Vector3 p_12_b = p1 + x * e_x + y * e_y - z * e_z;
        collision = true;
        return p_12_a; // p_12_b
    }

    void Refresh()
    {
        refresh = false;
    }

    void Repaint()
    {
        if (Theta == 0 || Theta == 180)
        {
            if (!repainted)
            {
                SceneView.RepaintAll();
                repainted = true;
            }
        }
        else
        {
            repainted = false;
        }
    }

    void Update()
    {
        
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        return;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        refresh = true;
    }

    void OnDrawGizmos()
    {
        Theta = theta;
        if (v)
        {
            p = Trilaterate(L.TransformVector(l), r, c, lm, rm, cm, out collision);
        }
        else
        {
            CalculateParallelPoints();
        }
        if (refresh)
        {
            Refresh();
        }
        Repaint();
        //Debug.Log("LeftPoint: " + LeftPoint.x + ", " + LeftPoint.y + ", " + LeftPoint.z);
        //Debug.Log("RightPoint: " + RightPoint.x + ", " + RightPoint.y + ", " + RightPoint.z);
        //Debug.Log("CenterPoint: " + CenterPoint.x + ", " + CenterPoint.y + ", " + CenterPoint.z);
        ////Debug.Log("IntersectionPoint: " + IntersectionPoint);
        //Debug.Log("LeftMagnitude: " + LeftMagnitude);
        //Debug.Log("RightMagnitude: " + RightMagnitude);
        //Debug.Log("CenterMagnitude: " + CenterMagnitude);

        //leftSphere.transform.position = LeftPoint;
        //leftSphere.transform.localScale = Vector3.one * (LeftMagnitude * 2);
        //rightSphere.transform.position = RightPoint;
        //rightSphere.transform.localScale = Vector3.one * (RightMagnitude * 2);
        //centerSphere.transform.position = CenterPoint;
        //centerSphere.transform.localScale = Vector3.one * (CenterMagnitude * 2);

        if (v)
        {
            //Gizmos.color = new Color(0, 0, 1, 0.3f);
            //Gizmos.DrawSphere(L.TransformVector(l), lm);
            //Gizmos.DrawSphere(r, rm);
            //Gizmos.DrawSphere(c, cm);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(L.TransformVector(l), c);
            Gizmos.DrawLine(r, c);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(L.TransformVector(l), p);
            Gizmos.DrawLine(r, p);
            Gizmos.DrawLine(c, p);
            //if (collision)
            //{
            //    Gizmos.color = Color.blue;
            //    Gizmos.DrawSphere(p, 0.1f);
            //}
            //else
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawSphere(p, 0.1f);
            //}
        }
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, a1b13);
            Gizmos.DrawLine(Vector3.zero, a2b23);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(a2b23, xy3);
            Gizmos.DrawLine(xy3, XY3);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(a1b13, XY3);
            Gizmos.DrawLine(a2b23, XY3);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Vector3.zero, new Vector3(a1b13.x, 0, 0));
            Gizmos.DrawLine(new Vector3(a1b13.x, 0, 0), new Vector3(a1b13.x, a1b13.y, 0));
            DebugExtension.DrawCircle(a1b13, Vector3.forward, Color.black, h1);
            DebugExtension.DrawCircle(a2b23, Vector3.forward, Color.black, h2);
        }
    }
}
