using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSignalActivator : ObjectActivator
{
    [SerializeField]
    bool startActivated = false;
    [SerializeField]
    Vector3 rotationAxis = Vector3.up;
    [SerializeField]
    float rotationDuration = 0.25f;
    [SerializeField]
    List<Collider> collidersThatTriggerSignal;

    float rotationTimer = 0f;

    new Rigidbody rigidbody;
    ReBody reBody;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.isKinematic = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        reBody = new ReBody(rigidbody);

        if (startActivated)
        {
            ActivationAmount = 1f;
        }
        InstantMoveIntoPosition();
    }

    void InstantMoveIntoPosition()
    {
        reBody.rotation = GetTargetRotation();
        rotationTimer = rotationDuration;
    }

    private void FixedUpdate()
    {
        if (rotationTimer < rotationDuration)
        {
            reBody.rotation = Quaternion.Slerp(reBody.rotation, GetTargetRotation(), rotationTimer / rotationDuration);
            rotationTimer += Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rotationTimer >= rotationDuration)
        {
            if (collision.rigidbody != null && LayerUtils.IsPartOfLayer(collision.rigidbody.gameObject.layer, Layers.Projectile))
            {
                for (int contact = 0; contact < collision.contactCount; contact++)
                {
                    if (collidersThatTriggerSignal.Contains(collision.GetContact(contact).thisCollider))
                    {
                        ToggleSwitch();
                        break;
                    }
                }
            }
        }
    }

    void ToggleSwitch()
    {
        rotationTimer = 0f;
        if (ActivationAmount > 0.5f)
        {
            ActivationAmount = 0f;
        }
        else
        {
            ActivationAmount = 1f;
        }
    }

    Quaternion GetTargetRotation()
    {
        return Quaternion.AngleAxis(GetTargetAngle(), rotationAxis);
    }

    float GetTargetAngle()
    {
        if (ActivationAmount > 0.5f)
        {
            return 180f;
        }
        else
        {
            return 0f;
        }
    }
}
