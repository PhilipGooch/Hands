using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public interface IGripMarker
    {
        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable, int bodyId, int gripId, in HandCarryData otherHand);
    }

}
