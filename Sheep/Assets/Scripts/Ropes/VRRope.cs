using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.XPBDRope;

public class VRRope : MonoBehaviour, IRopeSegmentCreationListener, IRopeCreationListener
{
    [SerializeField]
    GrabParams segmentGrabParams;
    [SerializeField]
    PhysicalMaterial ropeMaterial;
    [SerializeField]
    [HideInInspector]
    List<InteractableEntity> entities = new List<InteractableEntity>();

    public void AfterRopeCreation(Rope target)
    {
        GrabEntityFromBody(target.BodyStartIsAttachedTo);
        GrabEntityFromBody(target.BodyEndIsAttachedTo);
    }

    void GrabEntityFromBody(Rigidbody target)
    {
        if (target != null)
        {
            var entity = target.GetComponent<InteractableEntity>();
            if (entity != null && !entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }
    }

    public void AfterSegmentCreation(RopeSegment target)
    {
        var binding = target.gameObject.AddComponent<GrabParamsBinding>();
        binding.grabParams = segmentGrabParams;
        var vrSegment = target.gameObject.AddComponent<VRRopeSegment>();
        vrSegment.SetupVRRopeSegment();
        var entity = target.gameObject.AddComponent<InteractableEntity>();
        entity.physicalMaterial = ropeMaterial;
        entities.Add(entity);
    }

    public void BeforeSegmentCreation(GameObject target)
    {
    }

    public void BeforeRopeCreation(Rope target)
    {
        entities.Clear();
    }
}
