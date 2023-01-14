using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectWeightTracker : MonoBehaviour
{
    [SerializeField]
    Vector3 down = -Vector3.up;

    Queue<float> aggregatedMasses = new Queue<float>();
    float objectWeight = 0f;
    float gravityMagnitude = 9.8f;
    const int massAggregationFrames = 6;

    void Start()
    {
        gravityMagnitude = Vector3.Project(Physics.gravity, down).magnitude;
    }

    public float GetWeight()
    {
        float massSum = 0;
        foreach (var mass in aggregatedMasses)
        {
            massSum += mass;
        }
        return (massSum / aggregatedMasses.Count);
    }

    private void FixedUpdate()
    {
        aggregatedMasses.Enqueue(objectWeight);
        objectWeight = 0f;

        if (aggregatedMasses.Count > massAggregationFrames)
        {
            aggregatedMasses.Dequeue();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var otherRig = collision.rigidbody;
        if (otherRig != null)
        {
            // Prevent sleeping to get correct weight readings with stationary objects
            otherRig.sleepThreshold = 0f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        var otherRig = collision.rigidbody;
        if (otherRig != null)
        {
            otherRig.sleepThreshold = Physics.sleepThreshold;
        }
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        var otherRig = collisionInfo.rigidbody;
        if (otherRig != null)
        {
            //Debug.Log("VEL: " + collisionInfo.relativeVelocity.magnitude);
            var impulse = collisionInfo.impulse;
            var downwardForce = Vector3.Project(impulse, down);
            // Get the force applied to the object this frame
            // Divide it by the gravity to obtain the presumed mass. If the object is moving the mass will appear higher, but that's fine
            objectWeight += downwardForce.magnitude / Time.fixedDeltaTime / gravityMagnitude;
        }
    }
}
