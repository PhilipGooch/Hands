using UnityEngine;
using VR.System;

public class UILaserPointer : MonoBehaviour
{
    [SerializeField]
    Material laserMaterial;
    [SerializeField]
    Vector3 pointerOffsetDegrees = new Vector3(10, 0, 0);

    public new Camera camera;

#if UNITY_EDITOR
    bool editorControls => VRSystem.Instance.EditorSystemLoaded;
#endif

    Player player;
    PlayerUIManager uiManager;
    LineRenderer lineRenderer;
    Button3D draggedButton = null;

    float rayDistance = 100f;
    float lastHitDistance = 100f;
    float previousTriggerValue = 0f;

    int interactionCount = 0;
    const float kClickThreshold = 0.25f;

    Vector3[] rayPositions = new Vector3[2];

    void Awake()
    {
        player = GetComponent<Player>();
        uiManager = GetComponentInChildren<PlayerUIManager>();
        uiManager.onUIInteractionStarted += StartUIInteraction;
        uiManager.onUIInteractionEnded += EndUIInteraction;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.material = laserMaterial;
        enabled = false;
        // Need to call this since OnDisable will not get called if we disable this object on Awake
        lineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (uiManager)
        {
            uiManager.onUIInteractionStarted -= StartUIInteraction;
            uiManager.onUIInteractionEnded -= EndUIInteraction;
        }
    }

    private void OnDisable()
    {
        lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        lineRenderer.enabled = true;
    }

    private void Update()
    {
        RaycastHit hitInfo;

        var targetHand = player.mainHand;
        float currentTrigger = targetHand.Trigger;

        bool didHit;
#if UNITY_EDITOR
        if (editorControls)
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            didHit = Physics.Raycast(ray, out hitInfo, rayDistance, (int)Layers.UI);
        }
        else
#endif
        {
            didHit = Physics.Raycast(targetHand.HandPositions.PointingPosition, GetRayDirection(targetHand), out hitInfo, rayDistance, (int)Layers.UI);
        }

        if (didHit)
        {
            lastHitDistance = hitInfo.distance;
            var button = hitInfo.collider.GetComponentInParent<Button3D>();
            if (button != null)
            {
                button.Hover();

                if ((currentTrigger >= kClickThreshold && previousTriggerValue < kClickThreshold)
#if UNITY_EDITOR
                    || (editorControls && Input.GetMouseButtonDown(0))
#endif
                    )
                {
                    draggedButton = button;
                    button.Click(hitInfo.point);
                }
            }
        }
        else
        {
            lastHitDistance = rayDistance;
        }

        UpdateDrag();

        previousTriggerValue = currentTrigger;
    }

    void UpdateDrag()
    {
        if (draggedButton != null)
        {
            var targetHand = player.mainHand;
            if (targetHand.Trigger > kClickThreshold
#if UNITY_EDITOR
                || (editorControls && Input.GetMouseButton(0))
#endif
                )
            {
                var pointerDirection = GetRayDirection(targetHand);
                var ray =
#if UNITY_EDITOR
                    editorControls ? camera.ScreenPointToRay(Input.mousePosition) :
#endif
                    new Ray(targetHand.HandPositions.PointingPosition, pointerDirection);

                var plane = new Plane(draggedButton.transform.forward, draggedButton.transform.position);
                var hit = plane.Raycast(ray, out var distance);
                if (hit)
                {
                    var point = ray.GetPoint(distance);
                    draggedButton.Drag(point);
                }
                else
                {
                    draggedButton = null;
                }
            }
            else
            {
                draggedButton = null;
            }
        }
    }

    void LateUpdate()
    {

#if UNITY_EDITOR
        if (!editorControls)
#endif
        {
            DrawRayVR(player.mainHand);
        }
    }

    void StartUIInteraction()
    {
        enabled = true;
    }

    void EndUIInteraction()
    {
        enabled = false;
    }

    void DrawRayVR(Hand targetHand)
    {
        var pointingPosition = targetHand.HandPositions.PointingPosition;
        rayPositions[0] = pointingPosition;
        rayPositions[1] = pointingPosition + GetRayDirection(targetHand) * lastHitDistance;
        lineRenderer.SetPositions(rayPositions);
    }

    Vector3 GetRayDirection(Hand targetHand)
    {
        return (targetHand.transform.rotation * Quaternion.Euler(pointerOffsetDegrees) * Vector3.forward).normalized;
    }
}
