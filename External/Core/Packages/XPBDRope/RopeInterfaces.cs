using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    public interface IRopeCreationListener
    {
        void BeforeRopeCreation(Rope target);
        void AfterRopeCreation(Rope target);
    }

    public interface IRopeSegmentCreationListener
    {
        void BeforeSegmentCreation(GameObject target);
        void AfterSegmentCreation(RopeSegment target);
    }

    public interface IRopeSolveListener
    {
        void BeforeRopeSolve(Rope target);
        void AfterRopeSolve(Rope target);
    }

    public interface ISegmentRigidbodyListener
    {
        void BeforeReadingSegmentRecoilbody(RopeSegment target);
        void AfterWritingSegmentRecoilbody(RopeSegment target);
    }
}
