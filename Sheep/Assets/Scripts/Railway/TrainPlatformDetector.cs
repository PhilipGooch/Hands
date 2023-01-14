using UnityEngine;

public class TrainPlatformDetector : MonoBehaviour
{
    [SerializeField]
    private Transform platformDetectorFront;
    [SerializeField]
    private Transform platformDetectorBack;
    [HideInInspector]
    [SerializeField]
    private TrainBase trainBase;

    private RaycastHit[] raycastHits = new RaycastHit[16];
    [HideInInspector]
    private TrainRotatablePlatform frontActivePlatform;
    [HideInInspector]
    private TrainRotatablePlatform backActivePlatform;

    private void OnValidate()
    {
        if (trainBase == null)
            trainBase = GetComponent<TrainBase>();
    }

    private void FixedUpdate()
    {
        int hits = Physics.RaycastNonAlloc(platformDetectorFront.position, Vector3.down, raycastHits, 10f, (int)Layers.Walls);
        var platformFront = GetRotatablePlatform(hits);
        UpdatePlatform(ref platformFront, ref frontActivePlatform, platformDetectorFront);

        hits = Physics.RaycastNonAlloc(platformDetectorBack.position, Vector3.down, raycastHits, 10f, (int)Layers.Walls);
        var platformBack = GetRotatablePlatform(hits);
        UpdatePlatform(ref platformBack, ref backActivePlatform, platformDetectorBack);
    }

    private void UpdatePlatform(ref TrainRotatablePlatform newPlatform, ref TrainRotatablePlatform oldPlatform, Transform detector)
    {
        if (oldPlatform != null)
        {
            oldPlatform.RemoveConnection(trainBase, detector);
            oldPlatform = null;
        }

        if (newPlatform != null)
        {
            newPlatform.AddConnection(trainBase, detector);
            oldPlatform = newPlatform;
        }
    }

    private TrainRotatablePlatform GetRotatablePlatform(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (raycastHits[i].collider.attachedRigidbody == null)
                continue;

            TrainRotatablePlatform platform = raycastHits[i].collider.attachedRigidbody.GetComponentInChildren<TrainRotatablePlatform>();

            if (platform != null)
                return platform;
        }

        return null;
    }
}
