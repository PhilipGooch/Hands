using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSystem : MonoBehaviour
{
    [SerializeField]
    float glideRadius = 15f;
    [SerializeField]
    List<Bird> birds;
    [SerializeField]
    List<Transform> extraLandingLocations;

    List<Vector3> freeLandingSpots = new List<Vector3>();

    readonly Vector3 castExtents = new Vector3(0.5f, 0.25f, 0.5f);
    RaycastHit[] hits = new RaycastHit[8];
    private void Start()
    {
        foreach (var location in extraLandingLocations)
        {
            freeLandingSpots.Add(location.position);
        }
        foreach (var bird in birds)
        {
            bird.Initialize(this);
        }
    }


    public Vector3 GetClosestGlidePoint(Vector3 position, float randomValue)
    {
        var toPos = position - transform.position;
        toPos.y = 0;
        if (Mathf.Approximately(toPos.sqrMagnitude, 0f))
        {
            toPos = Vector3.right;
        }
        toPos = toPos.normalized;
        return GetGlidePositionInDirection(toPos, randomValue);
    }

    public bool TryGettingClosestNonBlockedLandingLocation(Vector3 position, ref Vector3 landingPos)
    {
        Vector3 closestPoint = Vector3.zero;
        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        for (int i = 0; i < freeLandingSpots.Count; i++)
        {
            bool blocked = IsPositionBlocked(freeLandingSpots[i]);
            if (!blocked)
            {
                var location = freeLandingSpots[i];
                var sqrDistance = (position - location).sqrMagnitude;
                if (sqrDistance < closestDistance)
                {
                    closestDistance = sqrDistance;
                    closestPoint = location;
                    closestIndex = i;
                }
            }
        }

        landingPos = closestPoint;

        if (closestIndex != -1)
        {
            freeLandingSpots.RemoveAt(closestIndex);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddFreeLandingLocation(Vector3 position)
    {
        freeLandingSpots.Add(position);
    }

    public Vector3 GetGlideDirection(Vector3 glidePosition, float randomValue)
    {
        var toPos = glidePosition - transform.position;
        var modifier = randomValue > 0.5f ? -1f : 1f;
        return Vector3.Cross(toPos, Vector3.up).normalized * modifier;
    }

    Vector3 GetGlidePositionForAngle(float angle)
    {
        var rad = angle * Mathf.Deg2Rad;
        return GetGlidePositionInDirection(new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)).normalized , 0f);
    }

    Vector3 GetGlidePositionInDirection(Vector3 direction, float randomValue)
    {
        direction.y = 0;
        var randomOffset = Mathf.Lerp(-3f,3f, randomValue);
        return transform.position + direction * glideRadius * randomOffset;

    }

    public bool LandingSpotAvailable()
    {
        return freeLandingSpots.Count > 0;
    }

    public bool IsPositionBlocked(Vector3 pos)
    {
        int count = Physics.BoxCastNonAlloc(pos, castExtents, Vector3.up, hits, Quaternion.identity, 0.5f);
        bool blocked = false;

        for (int i = 0; i < count; i++)
        {
            //blocked
            if (hits[i].collider.attachedRigidbody != null && !hits[i].collider.attachedRigidbody.isKinematic)
            {
                blocked = true;
                break;
            }
        }
        return blocked;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < 360; i++)
        {
            var startPos = GetGlidePositionForAngle(i);
            var endPos = GetGlidePositionForAngle(i + 1);
            Gizmos.DrawLine(startPos, endPos);
        }

        for (int i = 0; i < freeLandingSpots.Count; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(freeLandingSpots[i], 0.25f);
        }
    }

}
