using NBG.LogicGraph;
using UnityEngine;

namespace Sample.LogicGraph
{
    public class AllInputsAndOutputs : MonoBehaviour
    {
        public bool boolInputTest;
        public float floatInputTest;
        public int intInputTest;
        public string stringInputTest;
        public UnityEngine.Object objectInputTest;
        public Vector3 vector3InputTest;
        public Quaternion quaternionInputTest;

        [NodeAPI("All Inputs Test")]
        public void Test(
            bool boolInputTest,
            float floatInputTest,
            int intInputTest,
            string stringInputTest,
            UnityEngine.Object objectInputTest,
            Vector3 vector3InputTest,
            Quaternion quaternionInputTest)
        {

        }
    }
}
