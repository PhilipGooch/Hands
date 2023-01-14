using UnityEngine;

[ExecuteInEditMode]
public class BaseFold : Fold
{
    [SerializeField]
    float size;
    [SerializeField]
    protected bool showMesh, showFold;
    [SerializeField]
    bool showLeftPivot, showRightPivot;

    protected override void OnEnable()
    {
        base.OnEnable();
        CalculateVertices();
        UpdateMesh();
    }

    void Update()
    {
        if (Error()) return;

        CalculateVertices();
        SetVertices();
        if (leftPivot != null && rightPivot != null)
        {
            leftPivot.localRotation = Quaternion.AngleAxis(-Book.Theta, Vector3.up);
            rightPivot.localRotation = Quaternion.AngleAxis(Book.Theta, Vector3.up);
        }
        ShowMesh(showMesh);
    }

    void OnDrawGizmos()
    {
        if (showFold) DrawFold(transform, size);
        if (showLeftPivot) DrawTransformGizmo(leftPivot);
        if (showRightPivot) DrawTransformGizmo(rightPivot);
    }

    void CalculateVertices()
    {
        leftVertices.Clear();
        rightVertices.Clear();
        leftVertices.Add(new Vector3(-size, size));
        leftVertices.Add(new Vector3(-size, 0));
        leftVertices.Add(new Vector3(0, 0));
        leftVertices.Add(new Vector3(0, size));
        rightVertices.Add(new Vector3(0, size));
        rightVertices.Add(new Vector3(0, 0));
        rightVertices.Add(new Vector3(size, 0));
        rightVertices.Add(new Vector3(size, size));
    }
}
