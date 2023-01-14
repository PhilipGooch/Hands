
using UnityEngine;
using NBG.Core;
using NBG.Audio;
using Recoil;

[DisallowMultipleComponent]
public class CollisionAudioSensor : MonoBehaviour
{
    public bool active = true;
    [SerializeField] float volume = 1;
    [SerializeField] float pitchVariation = 0;
    [Tooltip("Min relative velocity returned by collision engine to trigger sound collision")]
    [SerializeField] float minVelocity = .4f;
    [Tooltip("Min velocity magnitude this Rigidbody needs to have to trigger sound collision")]
    [SerializeField] float minVelocityRb = 0f;
    [SerializeField] float minImpulse = 4;
    [Range(0f, 1f)]
    [SerializeField]
    float underwaterPitchMulti = 0.4f;

    Rigidbody rb;
    ReBody reBody;
    float nextSoundTime;

    InteractableEntity entity;
    protected void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        reBody = new ReBody(rb);
        entity = GetComponentInParent<InteractableEntity>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!active) return;
        BroadcastCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!active) return;
        BroadcastCollision(collision);
    }

    void BroadcastCollision(Collision collision)
    {

        var time = Time.time;
        if (nextSoundTime > time) return;

        if (reBody.BodyExists && reBody.velocity.magnitude < minVelocityRb) return;
        if (collision.relativeVelocity.magnitude < minVelocity) return;
        if (collision.impulse.magnitude < minImpulse) return;

        Vector3 pos;
        float impulse, normalVelocity, tangentVelocity, pitch2, volume2;
        PhysicMaterial mat1, mat2;
        collision.Analyze(out pos, out impulse, out normalVelocity, out tangentVelocity, out mat1, out mat2, out volume2, out pitch2);

        SurfaceType surf1 = SurfaceTypes.Resolve(collision.GetContact(0).thisCollider.material);
        SurfaceType surf2 = SurfaceTypes.Resolve(collision.collider.material);

        float normalizedVolume = normalVelocity / 3;
        if (normalizedVolume < 0.01f) return;

        var collisionPoint = collision.GetContact(0).point;

        if (IsPointInsideWater(collisionPoint))
            pitch2 *= underwaterPitchMulti;

        if (CollisionAudioEngine.instance.ReportCollision(surf1, surf2, collisionPoint, pos, normalizedVolume * volume, pitch2 + Random.Range(-pitchVariation, pitchVariation)))
        {
            nextSoundTime = 0.01f;

        }
    }

    bool IsPointInsideWater(Vector3 point)
    {
        if (entity != null)
            return entity.IsPointInsideWater(point);
        else
            return false;
    }
}
