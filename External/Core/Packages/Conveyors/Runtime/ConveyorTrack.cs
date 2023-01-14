using System;
using UnityEngine;

namespace NBG.Conveyors
{
    /// <summary>
    /// Contains and process globally the belt parts.
    /// </summary>
    [Serializable]
    internal class ConveyorTrack
    {
        [SerializeReference] internal ITrackPart[] parts;
        [SerializeField] internal float fullTrackLength;
        internal ConveyorTrack(ITrackPart[] trackParts, float pieceLength)
        {
            parts = new ITrackPart[trackParts.Length];
            for (int i = 0; i < trackParts.Length; i++)
            {
                parts[i] = trackParts[i];
            }
            CalculateFullTrackLength();
            CalculatePiecesPerPart(pieceLength);
        }
        private void CalculateFullTrackLength()
        {
            fullTrackLength = 0;
            for (int i = 0; i < parts.Length; i++)
                fullTrackLength += parts[i].Length;
        }

        private void CalculatePiecesPerPart(float pieceLength)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].ShortPieceCount = (int)(parts[i].Length / pieceLength);
                parts[i].LongPieceCount = parts[i].ShortPieceCount + 1;
            }
        }

        internal float AdjustPositionWithinLimits(float position)
        {
            if (position > fullTrackLength)
                position -= fullTrackLength;
            else if (position < 0)
                position += fullTrackLength;
            return position;
        }
        internal ITrackPart GetTrackPartInPosition(float position)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                float partLength = part.Length;
                float partStart = part.Start;
                if (position >= partStart && position < (partStart + partLength))
                    return parts[i];
            }
            return null;
        }

        internal void CalculatePieceTransform(ref float trackPosition, out Vector3 localPosition, out Quaternion localRotation)
        {
            trackPosition = AdjustPositionWithinLimits(trackPosition);
            ITrackPart part = GetTrackPartInPosition(trackPosition);
            localPosition = part.GetLocalPosition(trackPosition - part.Start);
            localRotation = part.GetLocalRotation(trackPosition - part.Start, false);
        }

        internal void CalculatePieceCount(float[] positions)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].CurrentPieceCount = 0;
            }

            for (int i = 0; i < positions.Length; i++)
                GetTrackPartInPosition(positions[i]).CurrentPieceCount++;
        }
    }

    /// <summary>
    /// Interface to implement needed calls for parts.
    /// </summary>
    internal interface ITrackPart
    {
        Vector3 GetLocalPosition(float localPosition);
        Quaternion GetLocalRotation(float localPosition, bool useStartAngleOffset);
        void SetStart(float start);
        float Length { get; }
        float Start { get; }
        int ShortPieceCount { get; set; }
        int LongPieceCount { get; set; }
        int CurrentPieceCount { get; set; }
    }
}
