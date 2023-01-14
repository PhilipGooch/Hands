using NBG.Core;
using System.Collections.Generic;
using UnityEngine;

public class ThreatFromActivation : ActivatableNode
{
    [SerializeField]
    private List<Transform> threatOrigins;
    [SerializeField]
    private float threatStrength = 5f;
    [SerializeField]
    private float threatRange = 0.3f;
    [Tooltip("Generate threat from this activation.")]
    [SerializeField]
    private float activationThreshold = 0.1f;
    [Tooltip("Threat is created by casting a ray to set direction and creating a threat from threatOrigin to hit object")]
    [SerializeField]
    private Vector3 threatCreationDirection = Vector3.down;
    [Tooltip("Raycast max range")]
    [SerializeField]
    private float threatMaxDetectionRange = 3;

    private List<BoxBounds> threatBounds = new List<BoxBounds>();

    private void Start()
    {
        threatBounds = GetThreatBounds();
    }

    private void FixedUpdate()
    {
        if (ActivationValue >= activationThreshold)
            CreateThreat();
    }

    private void CreateThreat()
    {
        foreach (var bounds in threatBounds)
        {
            Threat.AddRegularThreat(new BoxBoundThreat() { bounds = bounds, Range = threatRange, Strength = threatStrength });
        }
    }

    private List<BoxBounds> GetThreatBounds()
    {
        List<BoxBounds> threatBounds = new List<BoxBounds>();
        RaycastHit hit;

        foreach (var origin in threatOrigins)
        {
            if (Physics.Raycast(origin.position, origin.rotation * threatCreationDirection, out hit, threatMaxDetectionRange))
            {
                BoxBounds bounds = new BoxBounds((hit.point + origin.position) / 2, new Vector3(1, hit.distance, 1), origin.rotation);
                threatBounds.Add(bounds);
            }
        }

        return threatBounds;
    }

    private void OnDrawGizmos()
    {
        List<BoxBounds> threatBounds = GetThreatBounds();

        var oldMatrix = Gizmos.matrix;

        foreach (var bounds in threatBounds)
        {
            Gizmos.matrix = Matrix4x4.TRS(bounds.center, bounds.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, bounds.size);
        }

        Gizmos.matrix = oldMatrix;
    }
}
