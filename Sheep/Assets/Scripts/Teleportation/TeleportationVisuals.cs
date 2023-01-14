using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportationVisuals : MonoBehaviour
{
    [SerializeField]
    Color colorValid;
    [SerializeField]
    Color colorInvalid;

    [SerializeField]
    TeleportationRing teleportationRingPrefab;

    TeleportationRing teleportationRing;

    [SerializeField]
    [Tooltip("How far the invalid ring hovers from meshes in the scene if the arc collides with them.")]
    float ringHover = 0.1f;

    public float arcThickness = 0.1f;

    private LineRenderer lineRenderer;
    private float prevThickness = 0.0f;

    List<Vector3> arcVertices = new List<Vector3>();

    private void Start()
    {
        teleportationRing = Instantiate(teleportationRingPrefab);
        teleportationRing.gameObject.SetActive(false);
        DontDestroyOnLoad(teleportationRing.gameObject);
        CreateLineRendererObjects();
    }

    void Update()
    {
        if (arcThickness != prevThickness)
        {
            prevThickness = arcThickness;
        }
    }

    public void DrawValidRing(Vector3 position)
    {
        teleportationRing.gameObject.SetActive(true);
        teleportationRing.SetColor(colorValid);
        teleportationRing.gameObject.transform.position = position;
        teleportationRing.gameObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        teleportationRing.gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    public void DrawInvalidRing(Vector3 position, Vector3 collisionNormal)
    {
        teleportationRing.gameObject.SetActive(true);
        teleportationRing.SetColor(colorInvalid);
        teleportationRing.gameObject.transform.position = position + (collisionNormal * ringHover);
        teleportationRing.gameObject.transform.rotation = Quaternion.LookRotation(collisionNormal);
        teleportationRing.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }

    public void HideRing()
    {
        teleportationRing.gameObject.SetActive(false);
    }

    private void CreateLineRendererObjects()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = Teleportation.Instance.arcSegmentCount + 1;
        lineRenderer.receiveShadows = false;
        lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = colorInvalid;
        lineRenderer.endColor = colorInvalid;
        lineRenderer.startWidth = arcThickness;
        lineRenderer.endWidth = arcThickness;
        lineRenderer.enabled = false;
    }

    public void HideArc()
    {
        lineRenderer.enabled = false;
    }

    public void SetArcColor(bool valid)
    {
        lineRenderer.startColor = valid ? colorValid : colorInvalid;
        lineRenderer.endColor = valid ? colorValid : colorInvalid;
    }

    public void DrawArc(float hitTime)
    {
        lineRenderer.enabled = true;
        float timeStep = Teleportation.Instance.arcDuration / Teleportation.Instance.arcSegmentCount;
        float arcTime = 0;
        arcVertices.Clear();
        for (int i = 0; i < Teleportation.Instance.arcSegmentCount; i++)
        {
            AddArcVertex(arcTime);
            arcTime += timeStep;
            if (arcTime >= Teleportation.Instance.arcDuration)
            {
                arcTime = Teleportation.Instance.arcDuration;
                break;
            }
            if (arcTime >= hitTime)
            {
                arcTime = hitTime;
                break;
            }
        }
        AddArcVertex(arcTime);
        lineRenderer.positionCount = arcVertices.Count;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            lineRenderer.SetPosition(i, arcVertices[i]);
        }
    }

    void AddArcVertex(float arcTime)
    {
        arcVertices.Add(Arc.GetArcPositionAtTime(arcTime, Teleportation.Instance.ArcStartPosition, Teleportation.Instance.ArcVelocity));
    }
}
