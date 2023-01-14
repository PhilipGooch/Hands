using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratePokeThreatsOnCollision : MonoBehaviour
{
    [SerializeField]
    float pokeThreatLifetime = 2f;
    [SerializeField]
    float pokeThreatRadius = 1f;

    const float speedToBecomeThreat = 1f;

    struct PokeThreat
    {
        public Vector3 position;
        public float timer;
        public float radius;

        public PokeThreat(Vector3 position, float timer, float radius)
        {
            this.position = position;
            this.timer = timer;
            this.radius = radius;
        }
    }

    List<PokeThreat> pokeThreats = new List<PokeThreat>();

    private void OnCollisionEnter(Collision collision)
    {
        GenerateThreat(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        GenerateThreat(collision);
    }

    void GenerateThreat(Collision collision)
    {
        if (collision.rigidbody != null && collision.rigidbody.velocity.sqrMagnitude > speedToBecomeThreat * speedToBecomeThreat)
        {
            var threateningObject = collision.collider.GetComponentInParent<ThreateningObject>();
            if (threateningObject != null)
            {
                pokeThreats.Add(new PokeThreat(collision.GetContact(0).point, pokeThreatLifetime, pokeThreatRadius));
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = pokeThreats.Count - 1; i >= 0; i--)
        {
            var targetThreat = pokeThreats[i];
            targetThreat.timer -= Time.fixedDeltaTime;
            if (targetThreat.timer < 0)
            {
                pokeThreats.RemoveAt(i);
            }
            else
            {
                Threat.AddPokeThreat(new BoxBoundThreat(targetThreat.position, targetThreat.radius));
                pokeThreats[i] = targetThreat;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (var t in pokeThreats)
        {
            Gizmos.DrawWireSphere(t.position, t.radius);
        }
    }
}
