using NBG.Core;
using NBG.Water;
using UnityEngine;

public class LevelWaterSystem : MonoBehaviour
{
    BodyOfWater[] waterBodies;

    private void Start()
    {
        waterBodies = GameObject.FindObjectsOfType<BodyOfWater>();

        foreach (var item in waterBodies)
        {
            item.OnColliderEnterWater += (collider) => { OnEnterWater(item, collider); };
        }
    }

    private void OnDestroy()
    {
        foreach (var item in waterBodies)
        {
            item.OnColliderEnterWater -= (collider) => { OnEnterWater(item, collider); };
        }
    }

    void OnEnterWater(BodyOfWater waterBody, Collider collider)
    {
        var nearestWaterPoint = waterBody.GlobalBox.ClosestPoint(collider.transform.position);
        var nearestObjectPoint = collider.ClosestPointSafe(nearestWaterPoint);
        var rigidbody = collider.attachedRigidbody;
        var scale = 0.25f;

        const float minSplash = 0.24f;
        if (rigidbody != null)
        {
            const float momentumForMaxSplash = 5000f;
            const float maxSplash = 1f;

            var momentum = rigidbody.velocity.magnitude * rigidbody.mass;
            var momentumScale = Mathf.Clamp01(Mathf.InverseLerp(0f, momentumForMaxSplash, momentum));
            var scaleLinear = Mathf.Lerp(0f, maxSplash, momentumScale);
            scale = 2 * scaleLinear - scaleLinear * scaleLinear;
        }

        if (scale >= minSplash)
        {
            var normal = waterBody.transform.up;
            CreateWaterParticlesAtPosition(waterBody, nearestObjectPoint, normal, scale);
        }
    }

    Vector3 CalculateWaterTopPosition(BodyOfWater waterBody, Vector3 normal)
    {
        var waterScale = Vector3.Project(waterBody.transform.lossyScale, normal);
        return waterBody.transform.position + waterScale * 0.5f;
    }

    void CreateWaterParticlesAtPosition(BodyOfWater waterBody, Vector3 position, Vector3 normal, float scale)
    {
        var waterDepth = Vector3.Project(CalculateWaterTopPosition(waterBody, normal), normal);
        var objectDepth = waterDepth - Vector3.Project(position, normal);
        if (objectDepth.magnitude < 0.5f)
        {
            var objectPositionWithoutDepth = Vector3.ProjectOnPlane(position, normal);
            GameParameters.Instance.waterParticles.Create(objectPositionWithoutDepth + waterDepth, Quaternion.identity, Vector3.one * scale);
        }
    }
}
