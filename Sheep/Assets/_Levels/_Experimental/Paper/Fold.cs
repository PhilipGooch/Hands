using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Fold : MonoBehaviour
{
    [SerializeField]
    protected Transform leftPivot, rightPivot;

    [SerializeField]
    [HideInInspector]
    protected PaperMesh leftMesh, rightMesh;
    [SerializeField]
    [HideInInspector]
    protected MeshFilter leftMeshFilter, rightMeshFilter;
    [SerializeField]
    [HideInInspector]
    protected MeshRenderer leftMeshRenderer, rightMeshRenderer;

    protected readonly List<Vector3> leftVertices = new List<Vector3>();
    protected readonly List<Vector3> rightVertices = new List<Vector3>();

    protected virtual void OnEnable()
    {
        if (Book.Instance != null)
        {
            RefreshBook();
        }
    }

    void OnDisable()
    {
        if (Book.Instance != null)
        {
            RefreshBook();
        }
        else
        {
            Book.ClearConsole();
        }
    }

    protected void RefreshBook() // <--- make event.
    {
        if (Book.Instance != null)
        {
            Book.Instance.Refresh();
        }
    }

    protected bool Error()
    {
        bool error = false;
        if (Book.Instance == null)
        {
            Debug.Log("Add book to scene.");
            error = true;
        }
        if (leftPivot == null || rightPivot == null)
        {
            Debug.Log("Set pivot transforms in " + gameObject.name + ".");
            error = true;
        }
        return error;
    }

    protected void ShowMesh(bool showMesh)
    {
        if (leftMeshRenderer != null && rightMeshRenderer != null)
        {
            leftMeshRenderer.enabled = showMesh;
            rightMeshRenderer.enabled = showMesh;
        }
    }

    protected void UpdateMesh()
    {
        leftMesh.Refresh(leftVertices);
        rightMesh.Refresh(rightVertices);
    }

    protected void SetVertices()
    {
        if (leftMeshFilter != null && rightMeshFilter != null)
        {
            leftMeshFilter.sharedMesh.SetVertices(leftVertices);
            rightMeshFilter.sharedMesh.SetVertices(rightVertices);
            leftMeshFilter.sharedMesh.RecalculateBounds();
            rightMeshFilter.sharedMesh.RecalculateBounds();
        }
    }

    protected static void DrawTransformGizmo(Transform transform)
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, Vector3.up);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, Vector3.right);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Vector3.forward);
        Gizmos.matrix = Matrix4x4.identity;
    }

    protected static void DrawFold(Transform transform, float size)
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = Color.black;
        Gizmos.DrawLine(Vector3.zero, new Vector3(0, size));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
