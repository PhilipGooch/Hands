using NBG.LogicGraph;
using System;
using UnityEngine;

namespace NBG.Core
{
    [HelpURL(HelpURLs.VolumeBlock)]
    public class VolumeBlock : MonoBehaviour
    {
        [SerializeField]
        new private Collider collider;
        [SerializeField]
        private LayerMask volumeBlockingLayers;

        [NodeAPI("OnVolumeFilledStateChanged", scope: NodeAPIScope.Sim)]
        public event Action<bool> onVolumeFilledStateChanged;

        private const float inwardsModifier = 0.65f;
        private const int raysCount = 5;
        private readonly Vector3 direction = Vector3.up;
        private bool volumeFilled;

        private BoxBounds bounds;

        private Vector3 localCorner0;
        private Vector3 localCorner1;
        private Vector3 localCorner2;
        private Vector3 localCorner3;
        private Vector3 localCenter;

        private void OnValidate()
        {
            if (collider == null)
                collider = GetComponent<Collider>();
        }

        private void Start()
        {
            bounds = new BoxBounds(collider);
            SetLocalCastPoints();
        }

        //would like to move from update to on trigger enter, but that is way too inaccurate, since the object can leave trigger but still be detected by raycasts
        //maybe jobify performance fails?
        private void FixedUpdate()
        {
            CheckIfVolumeFilled();
        }

        private void CheckIfVolumeFilled()
        {
            var data = GetCastData();
            int hitCount = 0;
            foreach (var origin in data.origins)
            {
                if (Physics.Raycast(origin, data.direction, data.direction.magnitude, volumeBlockingLayers, QueryTriggerInteraction.Ignore))
                {
                    hitCount++;
                }
            }

            if (hitCount == raysCount && !volumeFilled)
            {
                volumeFilled = true;
                onVolumeFilledStateChanged?.Invoke(volumeFilled);
            }
            else if (hitCount < raysCount && volumeFilled)
            {
                volumeFilled = false;
                onVolumeFilledStateChanged?.Invoke(volumeFilled);
            }
        }

        //these dont change during gameplay
        void SetLocalCastPoints()
        {
            var min = bounds.center - bounds.extents;
            var max = bounds.center + bounds.extents;

            Vector3 inwardsModifierVector = new Vector3(inwardsModifier, 1, inwardsModifier);

            localCorner0 = Vector3.Scale(new Vector3(min.x, min.y, min.z) - transform.position, inwardsModifierVector);
            localCorner1 = Vector3.Scale(new Vector3(min.x, min.y, max.z) - transform.position, inwardsModifierVector);
            localCorner2 = Vector3.Scale(new Vector3(max.x, min.y, min.z) - transform.position, inwardsModifierVector);
            localCorner3 = Vector3.Scale(new Vector3(max.x, min.y, max.z) - transform.position, inwardsModifierVector);
            localCenter = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, bounds.center.z) - transform.position;
        }

        private (Vector3[] origins, Vector3 direction) GetCastData()
        {
            Vector3[] origins = new Vector3[raysCount];

            origins[0] = transform.TransformPoint(localCorner0);
            origins[1] = transform.TransformPoint(localCorner1);
            origins[2] = transform.TransformPoint(localCorner2);
            origins[3] = transform.TransformPoint(localCorner3);
            origins[4] = transform.TransformPoint(localCenter);

            return (origins, transform.TransformDirection(Vector3.Scale(direction, bounds.size)).normalized);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                bounds = new BoxBounds(collider);
                SetLocalCastPoints();
            }

            Gizmos.color = volumeFilled ? Color.green : Color.red;
            var data = GetCastData();

            foreach (var origin in data.origins)
            {
                if (Physics.Raycast(origin, data.direction, data.direction.magnitude, volumeBlockingLayers, QueryTriggerInteraction.Ignore))
                {
                    DebugExtension.DrawArrow(origin, data.direction, Color.red);
                }
                else
                {
                    DebugExtension.DrawArrow(origin, data.direction, Color.green);
                }
            }
        }
    }
}
