using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclesDemo
{
    public class ReadSolverIterationCount : MonoBehaviour
    {
        public int iterationCount;
        public int defaultIterationCount;
        // Update is called once per frame
        void Update()
        {
            iterationCount = gameObject.GetComponent<Rigidbody>().solverVelocityIterations;
            defaultIterationCount = Physics.defaultSolverVelocityIterations;
        }
    }
}
