using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tests.UnityJointingWithRecoil
{
    public class JointingTestPermutations : MonoBehaviour
    {
        [SerializeField]
        private float testRerunClock = 3f;
        [SerializeField]
        Vector3 placementEdge = Vector3.zero;
        [SerializeField]
        float placementStep = 5f;
        [SerializeField]
        private JointingTest targetTest;
        [SerializeField]
        private TextMeshPro countdownField;
        private List<JointingTest> allJointTests = new List<JointingTest>();

        private bool testRan = false;
        private float time = 0f;

        private void Awake()
        {
            Vector3 initialPlacement = targetTest.transform.position;
            Vector3 nextPlacement = initialPlacement;
            allJointTests.Add(targetTest);
            bool[] addStrongForce = new bool[2] { true, false };
            for (int i = 0; i < addStrongForce.Length; i++)
            {
                foreach (JointingTest.AttachJointTest attachTest in (JointingTest.AttachJointTest[])Enum.GetValues(typeof(JointingTest.AttachJointTest)))
                {
                    foreach (JointingTest.ObjectMoveTest moveTest in (JointingTest.ObjectMoveTest[])Enum.GetValues(typeof(JointingTest.ObjectMoveTest)))
                    {
                        if (targetTest.attachCondition == attachTest && targetTest.objectMoveCondition == moveTest
                            && targetTest.addStrongForceBefore == addStrongForce[i])
                        {
                            continue;
                        }
                        nextPlacement.x += placementStep;
                        if (nextPlacement.x > placementEdge.x)
                        {
                            nextPlacement.x = initialPlacement.x;
                            nextPlacement.y += placementStep;
                        }

                        JointingTest newTest = Instantiate(targetTest, nextPlacement, targetTest.transform.rotation);
                        newTest.attachCondition = attachTest;
                        newTest.objectMoveCondition = moveTest;
                        newTest.addStrongForceBefore = addStrongForce[i];
                        newTest.RefreshTextField();
                        allJointTests.Add(newTest);
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!testRan)
            {
                if (time > testRerunClock)
                {
                    for (int i = 0; i < allJointTests.Count; i++)
                    {
                        allJointTests[i].RunTestOnNextFixedUpdate();
                    }
                    time = testRerunClock;
                    testRan = true;
                }
                else
                {
                    time += Time.deltaTime;
                }
                countdownField.text = $"Time until test launch:\n{testRerunClock - time}";
            }
        }
    }
}
