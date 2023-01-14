using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreateningObject : MonoBehaviour
{
    [System.Serializable]
    public struct ThreatSettings
    {
        public bool preventThrowing;
        public bool propagateThreat;
        public bool scareSheepWhenGrabbed;
        public bool receiveThreatFromOtherObjects;

        public ThreatSettings(bool preventThrowing, bool propagateThreat, bool receiveThreatFromOtherObjects, bool scareSheepWhenGrabbed = true)
        {
            this.preventThrowing = preventThrowing;
            this.propagateThreat = propagateThreat;
            this.scareSheepWhenGrabbed = scareSheepWhenGrabbed;
            this.receiveThreatFromOtherObjects = receiveThreatFromOtherObjects;
        }
    }

    public ThreatSettings threatSettings = new ThreatSettings(true, true, true);
    public Collider[] colliders;
    public new Rigidbody rigidbody;
    ReBody reBody;
    List<ThreateningObject> objectsTouched = new List<ThreateningObject>();
    float speedToBecomeThreat = 1f;


    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponentInParent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        speedToBecomeThreat = GameParameters.Instance.objectSpeedToBecomeThreat;
    }

    public void Initialize(Collider[] colliders, ThreatSettings settings)
    {
        this.colliders = colliders;
        threatSettings = settings;

        PropagateAditional();
    }

    void PropagateAditional()
    {
        PropagateThreat aditional = GetComponentInChildren<PropagateThreat>();
        if (aditional != null)
        {
            for (int i = 0; i < aditional.propagateTo.Count; i++)
            {
                RegisterOtherRig(aditional.propagateTo[i]);
            }
        }
    }

    private void OnDestroy()
    {
        foreach(var touchedObject in objectsTouched)
        {
            Destroy(touchedObject);
        }
    }

    public void GetAllThreateningColliders(List<Collider> list, int depth = 0)
    {
        if (depth > 32)
        {
            Debug.LogError("Infinite loop detected in threatening colliders!");
            return;
        }

        if (isActiveAndEnabled && threatSettings.scareSheepWhenGrabbed)
        {
            if (rigidbody != null)
            {
                if (reBody.velocity.magnitude > speedToBecomeThreat)
                {
                    foreach (var col in colliders)
                    {
                        if (col.enabled && col.gameObject.activeInHierarchy)
                        {
                            list.Add(col);
                        }
                    }
                }
            }
        }

        foreach (var touchedObject in objectsTouched)
        {
            if (touchedObject.isActiveAndEnabled)
            {
                touchedObject.GetAllThreateningColliders(list, depth + 1);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rigidbody != null && threatSettings.propagateThreat)
        {
            var otherRig = collision.rigidbody;
            if (otherRig != null)
            {
                RegisterOtherRig(otherRig);
            }
        }
    }

    void RegisterOtherRig(Rigidbody otherRig)
    {
        TryNotifySheep(otherRig);
        var existingThreat = otherRig.GetComponent<ThreateningObject>();
        if (existingThreat == null)
        {
            var colliders = otherRig.GetComponentsInChildren<Collider>();
            var threat = new ThreatSettings(false, true, true, threatSettings.scareSheepWhenGrabbed);
            if (otherRig.TryGetComponent<ThreatConfiguration>(out var threatConfig))
            {
                threat = threatConfig.threatSettings;
            }

            // Only add ThreateningObject if the target accepts threat propagation from other objects
            if (threat.receiveThreatFromOtherObjects)
            {
                var threateningObject = otherRig.gameObject.AddComponent<ThreateningObject>();
                threateningObject.Initialize(colliders, threat);
                objectsTouched.Add(threateningObject);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (rigidbody != null && collision.rigidbody != null)
        {
            TryNotifySheep(collision.rigidbody);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (rigidbody != null && threatSettings.propagateThreat)
        {
            var otherRig = collision.rigidbody;
            if (otherRig != null)
            {
                var threat = collision.rigidbody.GetComponent<ThreateningObject>();
                if (objectsTouched.Contains(threat))
                {
                    objectsTouched.Remove(threat);
                    Destroy(threat);
                }
            }
        }
    }

    void TryNotifySheep(Rigidbody rig)
    {
        if (threatSettings.preventThrowing)
        {
            var sheep = rig.GetComponentInParent<Sheep>();
            if (sheep != null)
            {
                sheep.NotifyAboutTouchedThreateningObject();
            }
        }
    }
}
