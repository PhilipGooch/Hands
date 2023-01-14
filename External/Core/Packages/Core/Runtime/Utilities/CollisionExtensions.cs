using UnityEngine;

namespace NBG.Core
{
    public static class CollisionExtensions
    {
        public static void Analyze(this Collision collision,
            out Vector3 pos, out float impulse, out float normalVelocity, out float tangentVelocity,
            out PhysicMaterial mat1, out PhysicMaterial mat2, out float volume2, out float pitch2
            )
        {
            var collisionContacts = collision.contacts;
            volume2 = 1;
            pitch2 = 1;
            mat1 = mat2 = null;
            normalVelocity = 0f;
            tangentVelocity = 0f;
            pos = collision.GetContact(0).point;
            Collider otherCollider = null;
            var count = 0;
            var relativeVelocity = collision.relativeVelocity;
            for (int i = 0; i < collisionContacts.Length; i++)
            {
                var contact = collisionContacts[i];
                var normal = Vector3.Dot(contact.normal, relativeVelocity);
                var tangent = (relativeVelocity - contact.normal * normal).magnitude;
                if (normal > normalVelocity)
                {
                    normalVelocity = normal;
                    tangentVelocity = tangent;
                    pos = contact.point;
                    mat1 = contact.thisCollider.sharedMaterial;
                    mat2 = contact.otherCollider.sharedMaterial;
                    otherCollider = contact.otherCollider;
                }
                count++;
            }
            impulse = collision.impulse.magnitude;
        }

        public static Vector3 GetNormalTangentVelocitiesAndImpulse(this Collision collision, Rigidbody thisBody)
        {
            var collisionContacts = collision.contacts;

            var normalMax = 0f;
            var tangentMax = 0f;
            var count = 0;
            var relativeVelocity = collision.relativeVelocity;
            for (int i = 0; i < collisionContacts.Length; i++)
            {
                var contact = collisionContacts[i];
                var normal = Vector3.Dot(contact.normal, relativeVelocity);
                var tangent = (relativeVelocity - contact.normal * normal).magnitude;
                if (normal > normalMax)
                {
                    normalMax = normal;
                    tangentMax = tangent;
                }
                count++;
            }
            return new Vector3(normalMax, tangentMax, collision.impulse.magnitude);
        }

        public static Vector3 GetPoint(this Collision collision)
        {
            return collision.GetContact(0).point;
        }

        // fix for opposite impulse sign
        public static Vector3 GetImpulse(this Collision collision)
        {
            var imp = collision.impulse;
            if (Vector3.Dot(imp, collision.GetContact(0).normal) < 0)
                return -imp;
            else
                return imp;
        }
    }
}
