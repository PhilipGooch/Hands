using UnityEngine;

[ExecuteInEditMode]
public class VFold : Fold
{
    [SerializeField]
    VFold parentVFold;

    [SerializeField]
    [HideInInspector]
    Transform CIL, CIR, CLL, CLR, CRL, CRR;

    [SerializeField]
    [Range(Book.Epsilon, 180 - Book.Epsilon)]
    float a = 45, b = 45;
    [SerializeField]
    [Range(45, 135)]
    float c = 90, d = 90;

    [SerializeField]
    [Range(0, 4)]
    float centerOffset;

    [SerializeField]
    [Range(Book.Epsilon, 4)]
    float leftLength = 2, rightLength = 2, height = 2;

    [SerializeField]
    bool inverted, opposite, trimmed;
    [SerializeField]
    public bool showMesh, showFold, showFoldLimits;
    [SerializeField]
    bool showCIL, showCIR, showCLL, showCLR, showCRL, showCRR;
    [SerializeField]
    bool showLeftSphere, showRightSphere, showCenterSphere;

    bool lastTrimmed;
    float maxA, maxB;
    
    Vector3 leftVertex, rightVertex, topVertex;
    Vector3 leftEulerAngles, rightEulerAngles;
    Vector3 invertedLeftEulerAngles, invertedRightEulerAngles;
    Quaternion rotationToLocalNormal;
    Vector3 leftPoint, rightPoint, centerPoint;
    float leftRadius, rightRadius, centerRadius;
    Vector3 intersectionPoint, lastIntersectionPoint;
    Vector3 leftProjectionPoint, rightProjectionPoint;
    Vector3 offset;

    protected override void OnEnable()
    {
        base.OnEnable();
        CalculateVertices();
        UpdateMesh();
    }

    void Update()
    {
        if (trimmed != lastTrimmed)
        {
            CalculateVertices();
            UpdateMesh();
        }
        lastTrimmed = trimmed;
    }

    void LateUpdate() // <--- need to update the transforms recursively.
    {
        if (Error()) return;

        maxA = d - Mathf.Max(Book.Epsilon, c + d - (180 - Book.Epsilon));
        maxB = c - Mathf.Max(Book.Epsilon, c + d - (180 - Book.Epsilon));
        if (c >= d)
        {
            a = Mathf.Clamp(a, Book.Epsilon, maxA);
            b = a + c - d;
        }
        else
        {
            b = Mathf.Clamp(b, Book.Epsilon, maxB);
            a = b + d - c;
        }
        CalculateVertices();
        SetVertices();
        leftVertex = trimmed ? leftVertices[0] : leftVertices[1];
        rightVertex = rightVertices[2];
        topVertex = rightVertices[0];
        leftEulerAngles = new Vector3(0, 90 - a, 90 - c);
        rightEulerAngles = new Vector3(0, b - 90, d - 90);
        rotationToLocalNormal = Quaternion.Euler(-90, 0, 0);
        offset = new Vector3(0, centerOffset, 0);
        leftPoint = leftPivot.TransformPoint(offset + rotationToLocalNormal * Quaternion.Euler(leftEulerAngles) * leftVertex);
        rightPoint = rightPivot.TransformPoint(offset + rotationToLocalNormal * Quaternion.Euler(rightEulerAngles) * rightVertex);
        centerPoint = leftPivot.TransformPoint(offset);
        leftRadius = Vector3.Distance(leftVertex, topVertex);
        rightRadius = Vector3.Distance(rightVertex, topVertex);
        centerRadius = height;
        lastIntersectionPoint = intersectionPoint;
        intersectionPoint = Trilaterate(leftPoint, rightPoint, centerPoint, leftRadius, rightRadius, centerRadius, (parentVFold != null && parentVFold.inverted) || opposite);
        intersectionPoint = intersectionPoint == Vector3.zero ? lastIntersectionPoint : intersectionPoint;
        leftProjectionPoint = centerPoint + Vector3.Project(intersectionPoint - centerPoint, leftPoint - centerPoint);
        rightProjectionPoint = centerPoint + Vector3.Project(intersectionPoint - centerPoint, rightPoint - centerPoint);
        leftEulerAngles.x = Vector3.SignedAngle(leftPivot.rotation * Vector3.back, intersectionPoint - leftProjectionPoint, centerPoint - leftPoint);
        rightEulerAngles.x = Vector3.SignedAngle(rightPivot.rotation * Vector3.back, intersectionPoint - rightProjectionPoint, rightPoint - centerPoint);
        invertedLeftEulerAngles = new Vector3(-leftEulerAngles.x, -leftEulerAngles.y, leftEulerAngles.z);
        invertedRightEulerAngles = new Vector3(-rightEulerAngles.x, -rightEulerAngles.y, rightEulerAngles.z);
        CIL.localPosition = centerPoint;
        CIR.localPosition = centerPoint;
        if (inverted)                               
        {
            CIL.rotation = leftPivot.rotation * rotationToLocalNormal * Quaternion.Euler(invertedLeftEulerAngles);
            CIR.rotation = rightPivot.rotation * rotationToLocalNormal * Quaternion.Euler(invertedRightEulerAngles);
            CLL.localRotation = Quaternion.AngleAxis(90 - (90 - c), Vector3.forward) * Quaternion.AngleAxis(90 - leftEulerAngles.x, Vector3.up);
            CLR.localRotation = Quaternion.AngleAxis(90 - (90 - c), Vector3.forward);
            CRL.localRotation = Quaternion.AngleAxis(-d, Vector3.forward);
            CRR.localRotation = Quaternion.AngleAxis(-d, Vector3.forward) * Quaternion.AngleAxis(rightEulerAngles.x - 90, Vector3.up);
        }
        else
        {
            CIL.rotation = leftPivot.rotation * rotationToLocalNormal * Quaternion.Euler(leftEulerAngles);
            CIR.rotation = rightPivot.rotation * rotationToLocalNormal * Quaternion.Euler(rightEulerAngles);
            CLL.localRotation = Quaternion.AngleAxis(90 - (90 - c), Vector3.forward) * Quaternion.AngleAxis(leftEulerAngles.x - 90, Vector3.up);
            CLR.localRotation = Quaternion.AngleAxis(90 - (90 - c), Vector3.forward);
            CRL.localRotation = Quaternion.AngleAxis(-d, Vector3.forward);
            CRR.localRotation = Quaternion.AngleAxis(-d, Vector3.forward) * Quaternion.AngleAxis(90 - rightEulerAngles.x, Vector3.up);
        }
        ShowMesh(showMesh);
        ShowSpheres();
    }

    void OnDrawGizmos()
    {
        if (showFoldLimits)
        {
            Gizmos.color = Color.blue;
            if (c >= d)
            {
                Vector3 maxAPoint = leftPivot.TransformPoint(offset + rotationToLocalNormal * Quaternion.Euler(0, inverted ? maxA - 180 : -maxA, 0) * Vector3.forward * leftLength);
                Gizmos.DrawLine(centerPoint, maxAPoint);
            }
            else
            {
                Vector3 maxBPoint = rightPivot.TransformPoint(offset + rotationToLocalNormal * Quaternion.Euler(0, inverted ? 180 - maxB : maxB, 0) * Vector3.forward * rightLength);
                Gizmos.DrawLine(centerPoint, maxBPoint);
            }
        }
        if (showFold) DrawFold(CIL, height);
        if (showCIL) DrawTransformGizmo(CIL);
        if (showCIR) DrawTransformGizmo(CIR);
        if (showCLL) DrawTransformGizmo(CLL);
        if (showCLR) DrawTransformGizmo(CLR);
        if (showCRL) DrawTransformGizmo(CRL);
        if (showCRR) DrawTransformGizmo(CRR);
    }

    // https://stackoverflow.com/questions/1406375/finding-intersection-points-between-3-spheres
    static Vector3 Trilaterate(Vector3 p1, Vector3 p2, Vector3 p3, float r1, float r2, float r3, bool inverted)
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
        if (temp4 < 0) return Vector3.zero;
        float z = Mathf.Sqrt(temp4);
        Vector3 p_12_a = p1 + x * e_x + y * e_y + z * e_z;
        Vector3 p_12_b = p1 + x * e_x + y * e_y - z * e_z;
        return inverted ? p_12_b : p_12_a;
    }

    void CalculateVertices()
    {
        leftVertices.Clear();
        rightVertices.Clear();
        if (trimmed)
        {
            leftVertices.Add(new Vector3(-leftLength, Mathf.Tan((90 - c) * Mathf.Deg2Rad) * leftLength));
            leftVertices.Add(new Vector3(0, 0));
            leftVertices.Add(new Vector3(0, height));
            rightVertices.Add(new Vector3(0, height));
            rightVertices.Add(new Vector3(0, 0));
            rightVertices.Add(new Vector3(rightLength, Mathf.Tan((90 - d) * Mathf.Deg2Rad) * rightLength));
        }
        else
        {
            leftVertices.Add(new Vector3(-leftLength, height));
            leftVertices.Add(new Vector3(-leftLength, Mathf.Tan((90 - c) * Mathf.Deg2Rad) * leftLength));
            leftVertices.Add(new Vector3(0, 0));
            leftVertices.Add(new Vector3(0, height));
            rightVertices.Add(new Vector3(0, height));
            rightVertices.Add(new Vector3(0, 0));
            rightVertices.Add(new Vector3(rightLength, Mathf.Tan((90 - d) * Mathf.Deg2Rad) * rightLength));
            rightVertices.Add(new Vector3(rightLength, height));
        }
    }

    void ShowSpheres()
    {
        if (Book.Instance != null)
        {
            Book.Instance.LeftSphere.SetActive(showLeftSphere);
            Book.Instance.RightSphere.SetActive(showRightSphere);
            Book.Instance.CenterSphere.SetActive(showCenterSphere);
            Book.Instance.LeftSphere.transform.position = leftPoint;
            Book.Instance.LeftSphere.transform.localScale = Vector3.one * (leftRadius * 2);
            Book.Instance.RightSphere.transform.position = rightPoint;
            Book.Instance.RightSphere.transform.localScale = Vector3.one * (rightRadius * 2);
            Book.Instance.CenterSphere.transform.position = centerPoint;
            Book.Instance.CenterSphere.transform.localScale = Vector3.one * (centerRadius * 2);
        }
    }
}
