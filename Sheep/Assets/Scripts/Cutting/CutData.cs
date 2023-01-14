using Recoil;
using UnityEngine;

namespace NBG.Sheep.Cutter
{
    public class CutData
    {
        public MeshFilter meshFilter;
        public ReBody reBody;
        public Collider collider;
        public ConfigurableJoint joint;
        public Rigidbody Rigidbody => reBody.rigidbody;

        public bool[] controlPointsCutState = new bool[12];

        public bool HasBeenCut()
        {
            if (HasCleanCut())
                return true;
            if (HasCornerCut())
                return true;

            return false;
        }

        //If any 4 edges have been cut - split the object
        private bool HasCleanCut()
        {
            int totalCut = 0;
            for (int x = 0; x < controlPointsCutState.Length; x++)
            {
                if (controlPointsCutState[x])
                {
                    totalCut++;
                }
                if (totalCut == 4)
                    return true;
            }

            return false;
        }

        //8 corners, each of them need to be checked, dont see a non hardcoded way of doing this
        private bool HasCornerCut()
        {
            if (controlPointsCutState[0])
                if ((controlPointsCutState[10] && controlPointsCutState[6]) || (controlPointsCutState[8] && controlPointsCutState[4]))
                    return true;

            if (controlPointsCutState[1])
                if ((controlPointsCutState[7] && controlPointsCutState[10]) || (controlPointsCutState[5] && controlPointsCutState[8]))
                    return true;

            if (controlPointsCutState[2])
                if ((controlPointsCutState[11] && controlPointsCutState[6]) || (controlPointsCutState[4] && controlPointsCutState[9]))
                    return true;

            if (controlPointsCutState[3])
                if ((controlPointsCutState[7] && controlPointsCutState[11]) || (controlPointsCutState[5] && controlPointsCutState[9]))
                    return true;

            return false;
        }
    }
}
