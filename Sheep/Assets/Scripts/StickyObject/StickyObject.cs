using Recoil;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttenuatedAudioPool))]
[RequireComponent(typeof(Rigidbody))]
public class StickyObject : MonoBehaviour, IGrabNotifications
{
    [SerializeField]
    private bool stickyAllAround = true;
    [SerializeField]
    private bool sticksToASingleObject = false;
    [SerializeField]
    private Vector3 normal;

    [HideInInspector]
    [SerializeField]
    new private Rigidbody rigidbody;
    private ReBody reBody;
    public ReBody ReBody => reBody;
    [Range(-1, 0)]
    [SerializeField]
    private float stickTollerance = -0.6f;

    [SerializeField]
    private float forceToStick = 3000;
    [SerializeField]
    private float unstickForce = 100000;
    [SerializeField]
    private float unstickTorque = 100000;
    [Tooltip("Gets multiplied by mass to get how much force needs to be accumulated until stuck object gets unstuck")]
    [SerializeField]
    private float stuckObjAcumulatedForceMulti = 10000;

    [SerializeField]
    private bool shouldUnstickOnItsOwn = false;
    [Tooltip("How long to wait after unsticking before sticking again")]
    [SerializeField]
    private float stickinessCooldown = 0.5f;
    private float stickTimer;
    private bool StickingOnCooldown => stickTimer < stickinessCooldown && connections.Count == 0;

    [SerializeField]
    private LayerMask stickyLayers = (int)(Layers.Walls | Layers.Object);
    [SerializeField]
    private AudioClip onStickSound;
    [SerializeField]
    private AudioClip onUnstuckSound;
    //needed in case there are a lot of small objects stuck to this 
    [SerializeField]
    private float feedbackCooldown = 0.5f;
    private float feedbackTimer;

    [Tooltip("Minimum distance required from existing joints to register a new one")]
    [SerializeField]
    private float minimumDistanceBetweenContacts = 0.4f;

    [HideInInspector]
    [SerializeField]
    private AttenuatedAudioPool audioPool;

    private Dictionary<GameObject, StickyConnection> connections = new Dictionary<GameObject, StickyConnection>();
    private List<GameObject> toRemove = new List<GameObject>();

    private Vector3 WorldNormal => transform.TransformDirection(normal);
    private bool isGrabbed;

    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();

        if (audioPool == null)
            audioPool = GetComponent<AttenuatedAudioPool>();
    }

    private void Start()
    {
        reBody = new ReBody(rigidbody);
        stickTimer = stickinessCooldown;
    }

    private void FixedUpdate()
    {
        foreach (var connection in connections)
        {
            connection.Value.Update(WorldNormal, shouldUnstickOnItsOwn, isGrabbed);
        }

        RemoveEmptyConnections();

        stickTimer += Time.fixedDeltaTime;
        feedbackTimer += Time.fixedDeltaTime;
    }

    private void RemoveEmptyConnections()
    {
        toRemove.Clear();

        foreach (var connection in connections)
        {
            if (!connection.Value.HasSticked)
                toRemove.Add(connection.Key);
        }

        if (toRemove.Count > 0)
        {
            foreach (var item in toRemove)
            {
                connections.Remove(item);
            }

            if (connections.Count == 0)
                stickTimer = 0;
        }
    }

    //not using OnCollisionEnter, because due to speculative detection, objects detect collision one frame BEFORE actually hitting it.
    private void OnCollisionStay(Collision collision)
    {
        TrySticking(collision);
    }

    private void TrySticking(Collision collision)
    {
        GameObject key = collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;

        if (CouldStick(key, collision))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                var force = collision.impulse / Time.fixedDeltaTime;

                if (stickyAllAround)
                {
                    if (force.magnitude >= forceToStick)
                    {
                        Stick(key, collision, contact);
                    }
                }
                else
                {
                    var collNormal = contact.normal;
                    var forceProjection = Vector3.Project(force, WorldNormal);

                    if (forceProjection.magnitude >= forceToStick && (Vector3.Dot(collNormal, WorldNormal) < stickTollerance))
                    {
                        Stick(key, collision, contact);
                    }
                }
            }
        }
    }

    private bool CouldStick(GameObject key, Collision collision)
    {
        if (!LayerUtils.IsPartOfLayer(collision.gameObject.layer, stickyLayers))
            return false;

        //wait for cooldown
        if (StickingOnCooldown)
            return false;

        if (connections.ContainsKey(key) && connections[key].FullySnapped)
            return false;

        if (sticksToASingleObject && connections.Count > 0)
            return false;

        return true;
    }

    private void Stick(GameObject key, Collision collision, ContactPoint contactPoint)
    {
        if (!connections.ContainsKey(key))
        {
            var connection = new StickyConnection(gameObject, collision.gameObject, reBody.mass, stickyAllAround, unstickForce, unstickTorque, stuckObjAcumulatedForceMulti, minimumDistanceBetweenContacts);
            connection.onJointBroken += OnUnstuckFeedback;
            connection.onJointCreated += OnStickFeedback;

            connections.Add(key, connection);
        }

        connections[key].AddContact(contactPoint, normal);
    }

    private void OnJointBreak()
    {
        OnUnstuckFeedback();
    }

    private void OnUnstuckFeedback()
    {
        if (feedbackTimer >= feedbackCooldown)
        {
            if (onUnstuckSound != null)
                audioPool.PlayOneShot(transform, onUnstuckSound);

            feedbackTimer = 0;
        }
    }

    private void OnStickFeedback()
    {
        if (feedbackTimer >= feedbackCooldown)
        {
            if (onStickSound != null)
                audioPool.PlayOneShot(transform, onStickSound);

            feedbackTimer = 0;
        }
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            isGrabbed = true;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            isGrabbed = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!stickyAllAround)
            Gizmos.DrawRay(rigidbody.position, WorldNormal);
    }
}
