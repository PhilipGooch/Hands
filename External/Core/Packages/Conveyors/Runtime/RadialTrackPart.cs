using System;
using UnityEngine;

namespace NBG.Conveyors
{
    /// <summary>
    /// Represents the radial parts of the belt.
    /// </summary>
    [Serializable]
    public class RadialTrackPart : ITrackPart
    {
        [field: SerializeField] public float Length { get; set; }
        [field: SerializeField] public float Start { get; set; }
        [field: SerializeField] public int ShortPieceCount { get; set; }
        [field: SerializeField] public int LongPieceCount { get; set; }
        [field: SerializeField] public int CurrentPieceCount { get; set; }

        [SerializeField] internal float startAngle;
        [SerializeField] internal float endAngle;
        [SerializeField] internal Vector3 center;
        [SerializeField] internal float radius;
        [SerializeField] internal bool isConcave;
        public RadialTrackPart(Vector3 center, float radius, Vector3 startDir, Vector3 endDir, bool isConcave)
        {
            if (isConcave)
            {
                startAngle = Mathf.Atan2(startDir.y, startDir.z) - Mathf.PI * 0.5f;
                endAngle = Mathf.Atan2(endDir.y, endDir.z) - Mathf.PI * 0.5f;
                if (endAngle < startAngle)
                {
                    endAngle += Mathf.PI * 2.0f;
                }
            }
            else
            {
                startAngle = Mathf.Atan2(startDir.y, startDir.z) + Mathf.PI * 0.5f;
                endAngle = Mathf.Atan2(endDir.y, endDir.z) + Mathf.PI * 0.5f;
                if (endAngle > startAngle)
                {
                    endAngle -= Mathf.PI * 2.0f;
                }
            }

            float angleDifference = Vector3.Angle(startDir, endDir) * Mathf.Deg2Rad;
            Length = radius * angleDifference;

            this.center = center;
            this.radius = radius;
            this.isConcave = isConcave;
        }
        public Vector3 GetLocalPosition(float localPosition)
        {
            float targetAngle = Mathf.Lerp(startAngle, endAngle, localPosition / Length);

            return center + Vector3.forward * Mathf.Cos(targetAngle) * radius + Vector3.up * Mathf.Sin(targetAngle) * radius;
        }
        public Quaternion GetLocalRotation(float localPosition, bool relativeRotation = false)
        {
            float rotationSign = isConcave ? -1.0f : 1.0f;

            if (relativeRotation)
            {
                Quaternion localRotation = Quaternion.Euler(rotationSign * (localPosition * 360.0f) / (2f * Mathf.PI * radius), 0f, 0f);
                return localRotation;
            }
            else
            {
                Vector3 position = GetLocalPosition(localPosition) - center;
                position.Normalize();
                return Quaternion.LookRotation(Vector3.Cross(Vector3.right*rotationSign, position), position*rotationSign);
            }

        }
        public void SetStart(float start)
        {
            Start = start;
        }
    }
}
