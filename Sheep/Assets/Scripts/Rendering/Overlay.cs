using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public abstract class Overlay : MonoBehaviour
{
    [SerializeField]
    Material blitMaterial = null;

    protected GameObject fullscreenQuad;
    protected Camera targetCamera;

    protected Material materialInstance;

    float GetDistanceFromCamera()
    {
        return targetCamera.nearClipPlane + 0.01f;
    }

    protected virtual void Awake()
    {
        targetCamera = GetComponent<Camera>();
        materialInstance = Instantiate(blitMaterial);
        SetupQuad();
    }

    protected abstract string QuadName { get; }

    void SetupQuad()
    {
        fullscreenQuad = new GameObject(QuadName);
        fullscreenQuad.AddComponent<MeshFilter>().mesh = GameParameters.Instance.quadMesh;
        fullscreenQuad.AddComponent<MeshRenderer>().material = materialInstance;
        fullscreenQuad.transform.SetParent(targetCamera.transform);
        fullscreenQuad.transform.localPosition = new Vector3(0, 0, GetDistanceFromCamera());
        fullscreenQuad.transform.localRotation = Quaternion.identity;
        fullscreenQuad.SetActive(false);
    }
}
