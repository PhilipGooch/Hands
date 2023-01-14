using Recoil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Trampoline : MonoBehaviour, CollisionListener
{
    [SerializeField]
    [Range(0f,2f)]
    float bounciness = 1f;
    [SerializeField]
    List<Collider> bouncyColliders = new List<Collider>();

    private void FixedUpdate()
    {
        bouncedBodies.Clear();
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (IsCollidingWithUs(collision))
        {
            var otherRB = collision.rigidbody;
            if (otherRB)
            {
                var sheep = collision.gameObject.GetComponentInParent<Sheep>();
                if (sheep != null)
                {
                    // Sheep are two rigidbodies connected via spring. 
                    // If we bounce just one body, the spring will eat a lot of the energy and the bounce will be small
                    // Therefore we bounce both bodies, based on their average relative velocity
                    BounceRigidbody(sheep.head, collision.relativeVelocity);
                    BounceRigidbody(sheep.tail, collision.relativeVelocity);
                }
                else
                {
                    BounceRigidbody(otherRB, collision.relativeVelocity);
                }
            }
        }
    }

    bool IsCollidingWithUs(Collision collision)
    {
        for(int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (bouncyColliders.Contains(contact.thisCollider))
            {
                return true;
            }
        }
        return false;
    }

    List<Rigidbody> bouncedBodies = new List<Rigidbody>();

    void BounceRigidbody(Rigidbody otherRB, Vector3 velocity)
    {
        if (bouncedBodies.Contains(otherRB))
        {
            return;
        }
        var velocityMagnitude = velocity.magnitude;
        if (velocityMagnitude > Physics.bounceThreshold)
        {
            var dot = Vector3.Dot(transform.up, velocity);
            if (dot < 0)
            {
                var reBody = new ReBody(otherRB);
                reBody.velocity = Vector3.Reflect(velocity, transform.up) * bounciness;
            }
            bouncedBodies.Add(otherRB);
        }
    }
}
