using System;
using UnityEngine;

namespace NBG.Conveyors
{
    /// <summary>
    /// Represents the linear parts of the belt.
    /// </summary>
    [Serializable]
    internal class LinearTrackPart : ITrackPart
    {
        [field: SerializeField] public float Length { get; set; }
        [field: SerializeField] public float Start { get; set; }
        [field: SerializeField] public int ShortPieceCount { get; set; }
        [field: SerializeField] public int LongPieceCount { get; set; }
        [field: SerializeField] public int CurrentPieceCount { get; set; }

        [SerializeField] internal Vector3 startPosition, endPosition;
        [SerializeField] private Quaternion rotation;
        public Vector3 Direction => (endPosition - startPosition).normalized;


        internal LinearTrackPart(AxisData start, AxisData end)
        {
            Vector3 dir = (end.localPosition - start.localPosition).normalized;
            Vector3 normal;

            float startNormalSign = 1.0f;
            float endNormalSign = 1.0f;
            Vector3 crossNormal = Vector3.Cross(dir, Vector3.right).normalized;
            if (start.concave == end.concave)
            {
                if (start.radius == end.radius)
                {
                    normal = crossNormal;
                }
                else if (start.radius < end.radius)
                {
                    normal = CalculateNormalVectorDirectTangent(start.radius, end.radius, start.localPosition, end.localPosition, start.concave && end.concave ? true : false);
                }
                else
                {
                    normal = CalculateNormalVectorDirectTangent(end.radius, start.radius, end.localPosition, start.localPosition, start.concave && end.concave ? false : true);
                }
            }
            else
            {
                if (start.concave)
                {
                    normal = CalculateNormalVectorIndirectTangent(start.radius, end.radius, start.localPosition, end.localPosition, true);
                    endNormalSign = -1.0f;
                }
                else
                {
                    normal = CalculateNormalVectorIndirectTangent(start.radius, end.radius, start.localPosition, end.localPosition, false);
                    endNormalSign = -1.0f;
                }
            }

            startPosition = start.localPosition + normal * start.radius * startNormalSign;
            endPosition = end.localPosition + normal * end.radius * endNormalSign;
            dir = (endPosition - startPosition).normalized;

            rotation = Quaternion.LookRotation(dir, crossNormal);

            Length = Vector3.Distance(startPosition, endPosition);
        }

        public Vector3 GetLocalPosition(float localPosition)
        {
            return startPosition + Direction * localPosition;
        }

        public Quaternion GetLocalRotation(float localPosition, bool useStartAngleOffset)
        {
            return rotation;
        }

        public void SetStart(float start)
        {
            Start = start;
        }

        //r1 should be always smaller than r2
        private Vector3 CalculateNormalVectorDirectTangent(float r1, float r2, Vector3 A, Vector3 B, bool invertAngle)
        {
            float d = Vector3.Distance(A, B);
            float rOffset = r2 - r1;
            float e = Mathf.Acos(Mathf.Sqrt(d * d - rOffset * rOffset) / d) * Mathf.Rad2Deg;
            Vector3 offset = B - A;
            return (Quaternion.Euler((e + 90.0f) * (invertAngle ? 1.0f : -1.0f), 0.0f, 0.0f) * offset).normalized;
        }
        private Vector3 CalculateNormalVectorIndirectTangent(float r1, float r2, Vector3 A, Vector3 B, bool invert)
        {
            float d = Vector3.Distance(A, B);
            float bothR = r2 + r1;
            float b = Mathf.Sqrt(d * d - bothR * bothR);

            float e = Mathf.Acos(b / d) * Mathf.Rad2Deg;
            Vector3 offset = B - A;
            Vector3 baseVector = (Quaternion.Euler(invert ? -e : e, 0.0f, 0.0f) * offset).normalized * b;
            return -((A + baseVector) - B).normalized;
        }
    }
}
