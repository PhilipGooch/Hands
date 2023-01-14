using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixWhenNoTrigger : MonoBehaviour
{
    public Rigidbody body;
    FixedJoint joint;
    private void OnEnable()
    {
        Fix();
    }
    private void OnDisable()
    {
        Release();
        beingDestroyed = true;
    }

    private void Fix()
    {

        if (joint == null)
            joint = body.gameObject.AddComponent<FixedJoint>();
    }

    bool beingDestroyed=false;

    private void Release()
    {
        if (joint != null)
            Destroy(joint);
        joint = null;
    }

    List<Collider> colliders = new List<Collider>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 0) return;
        if (beingDestroyed) return;
        
        
        if (other.transform.IsChildOf(body.transform)) return;
        colliders.Add(other);
        Release();
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != 0) return;

        colliders.Remove(other);
        for (int i = colliders.Count - 1; i >= 0; i--)
            if (colliders[i] == null)
                colliders.RemoveAt(i);
        if (beingDestroyed) return;
        if (colliders.Count == 0)
            Fix();
    }
}
