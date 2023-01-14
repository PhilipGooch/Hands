using UnityEngine;

[CreateAssetMenu]
public class BoidSettings : ScriptableObject
{
    [Tooltip("Minimum speed of the fish.")]
    public float minSpeed = .4f;
    [Tooltip("Maximum speed of the fish.")]
    public float maxSpeed = 1.5f;
    [Tooltip("Maximum speed of the fish while threatened.")]
    public float maxThreatenedSpeed = 3.3f;
    [Tooltip("The distance that fish can perceive other fish.")]
    public float perceptionRadius = 4;
    [Tooltip("The distance that fish will start to avoid other fish from. (Should be <= Perception Radius.)")]
    public float avoidanceRadius = 1;
    [Tooltip("All force magnitudes are clamped to this value.")]
    public float maxSteerSpeed = 1.53f;
    [Tooltip("How much weighting is put on fish aligning with their neighboring fish.")]
    public float alignmentWeight = .45f;
    [Tooltip("How much weighting is put on fish trying to get to the center position of their neighbouing fish.")]
    public float cohesionWeight = .75f;
    [Tooltip("How much weighting is put on fish separating their neighbouing fish.")]
    public float seperationWeight = .75f;
    [Header("Collisions")]
    [Tooltip("The layers that will be hit by sphere casts for collision detection.")]
    public LayerMask obstacleMask;
    [Tooltip("How much weighting is put on fish avoiding collisions. (Should be significantly higher than Separation, Alignment and Chohesion weights.)")]
    public float collisionWeight = 3;
    [Tooltip("The distance that fish will start avoiding collisions from.")]
    public float collisionDistance = .9f;
    [Tooltip("The radius of the sphere casts used for collision detection. " +
             "Thicker spheres will be more accurate, but can limit the size of gaps fish can swim through." +
             "Smaller spheres will be allow fish to swim through smaller gaps, but can cause fish to swim through obstacles, depending on other variables.")]
    public float spherecastRadius = .19f;
    [Tooltip("The distance behind the fish the collision shere casts will start from.")]
    public float spherecastOffset = .6f;
    [Header("Threat")]
    [Tooltip("How much weighting is put on fish avoiding threats (like vr hands).")]
    public float threatWeight = 6;
    [Tooltip("The distance that fish will start avoiding threat from.")]
    public float threatDistance = 2.5f;
}
