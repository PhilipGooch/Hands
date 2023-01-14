using UnityEngine;

public class Boid : MonoBehaviour
{
    public Vector3 Position { get; set; }
    public Vector3 Forward { get; set; }
    public Vector3 FlockHeading { get; set; }
    public Vector3 SeperationHeading { get; set; }
    public Vector3 FlockCenter { get; set; }
    public int NumFlockmates { get; set; }

    BoidSettings settings;
    Vector3 velocity;
    Vector3 spawnPosition;
    float lastSpeed;

    public void Initialize(BoidSettings settings, Vector3 spawnPosition)
    {
        this.settings = settings;
        this.spawnPosition = spawnPosition;
        Position = transform.position;
        Forward = transform.forward;
        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;
    }

    Vector3 CalculateThreatDirection()
    {
        Vector3 threatDirection = Vector3.zero;
        Vector3 leftHandThreat = Player.Instance.leftHand.pos;
        Vector3 rightHandThreat = Player.Instance.rightHand.pos;
        if (IsThreatened(leftHandThreat))
        {
            Vector3 offset = transform.position - leftHandThreat;
            threatDirection += offset / offset.sqrMagnitude;
        }
        if (IsThreatened(rightHandThreat))
        {
            Vector3 offset = transform.position - rightHandThreat;
            threatDirection += offset / offset.sqrMagnitude;
        }
        threatDirection.Normalize();
        return threatDirection;
    }

    public void UpdateBoid()
    {
        Vector3 acceleration = Vector3.zero;
        if (NumFlockmates > 0)
        {
            FlockCenter /= NumFlockmates;
            Vector3 offsetToFlockmatesCentre = (FlockCenter - Position);
            var alignmentForce = SteerTowards(FlockHeading.normalized, settings.maxSpeed) * settings.alignmentWeight;
            var cohesionForce = SteerTowards(offsetToFlockmatesCentre.normalized, settings.maxSpeed) * settings.cohesionWeight;
            var seperationForce = SteerTowards(SeperationHeading.normalized, settings.maxSpeed) * settings.seperationWeight;
            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }
        Vector3 threatDirection = CalculateThreatDirection();
        bool threatened = threatDirection != Vector3.zero;
        if (RayCollisionInDirection(threatened ? threatDirection : Forward))
        {
            Quaternion forwardQuaternion = transform.rotation;
            if (threatened)
            {
                // This causes fish to prioritise a collision avoidance vector that is pointing away from the threat.
                Vector3 averageOfThreatAndForward = (Forward.normalized + threatDirection.normalized) / 2;
                forwardQuaternion = Quaternion.LookRotation(averageOfThreatAndForward);
            }
            Vector3 avoidCollisionDirection = ObstacleRays(forwardQuaternion);
            acceleration += SteerTowards(avoidCollisionDirection, settings.maxThreatenedSpeed) * settings.collisionWeight;
        }
        else if (threatened)
        {
            acceleration += SteerTowards(threatDirection, settings.maxThreatenedSpeed) * settings.threatWeight;
        }
        if (IsOutOfBounds())
        {
            Debug.Log(gameObject.name + " out of bounds. Try increasing \"raycastOffset\" and/or \"collisionDistance\" slightly.");
            Respawn();
        }
        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        if (threatened)
        {
            speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxThreatenedSpeed);
        }
        else
        {
            // Fish will continue to travel at their threatened speed until another force is acted on it.
            // Then their speed will be capped at their last speed or max speed, whichever is higher.
            // This has the effect of them coming to rest naturally and makes them feel more life like.
            speed = Mathf.Clamp(speed, settings.minSpeed, Mathf.Max(settings.maxSpeed, lastSpeed));
        }
        lastSpeed = speed;
        velocity = dir * speed;
        transform.position += velocity * Time.deltaTime;
        transform.forward = dir;
        Position = transform.position;
        Forward = dir;
    }

    void Respawn()
    {
        transform.position = spawnPosition; 
    }

    bool RayCollisionInDirection(Vector3 direction)
    {
        return Physics.SphereCast(Position - direction * settings.spherecastOffset, settings.spherecastRadius, direction, out _, settings.collisionDistance + settings.spherecastOffset, settings.obstacleMask);
    }

    Vector3 ObstacleRays(Quaternion rotation)
    {
        Vector3[] rayDirections = BoidHelper.Directions;
        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 dir = rotation * rayDirections[i];
            Ray ray = new Ray(Position - dir * settings.spherecastRadius, dir);
            if (!Physics.SphereCast(ray, settings.spherecastRadius, settings.collisionDistance + settings.spherecastRadius, settings.obstacleMask))
            {
                return dir;
            }
        }
        return -Forward;
    }

    Vector3 SteerTowards(Vector3 direction, float maxSpeed)
    {
        Vector3 v = direction.normalized * maxSpeed - velocity;
        return Vector3.ClampMagnitude(v, settings.maxSteerSpeed);
    }

    bool IsThreatened(Vector3 threatPosition) 
    {
        return Vector3.SqrMagnitude(Position - threatPosition) < Mathf.Pow(settings.threatDistance, 2);
    }

    bool IsOutOfBounds()
    {
        Vector2 position2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 environmentPosition2D = new Vector2(BoidManager.Instance.transform.position.x, BoidManager.Instance.transform.position.z);
        return Mathf.Abs(transform.position.y - BoidManager.Instance.transform.position.y) > BoidManager.Instance.EnvironmentExtent ||
               Vector2.SqrMagnitude(position2D - environmentPosition2D) > Mathf.Pow(BoidManager.Instance.EnvironmentRadius, 2);
    }
}
