using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    public class RopeSolveExample : MonoBehaviour, IRopeSolveListener
    {
        public void AfterRopeSolve(Rope target)
        {
            Debug.DrawRay(target.Bones[0].transform.position, Vector3.right * 5f, Color.red);
            Debug.DrawRay(target.Bones[target.ActiveBoneCount-1].transform.position, Vector3.right * 5f, Color.red);
        }

        public void BeforeRopeSolve(Rope target)
        {
        }
    }
}

