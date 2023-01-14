using NBG.Core;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclesDemo
{
    public class SetSolverIterationCount : MonoBehaviour, IManagedBehaviour
    {
        public Rigidbody Rigidbody;
        public int solverVelocityIterations = 10;
        void Start()
        {
            Rigidbody.solverVelocityIterations = solverVelocityIterations;
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            Rigidbody.solverVelocityIterations = solverVelocityIterations;
            ManagedWorld.main.ResyncPhysXBody(Rigidbody);
            //World.main.ConfigureBodySleep(ManagedWorld.main.FindBody(Rigidbody), false);
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
        }

        private void OnValidate()
        {
            Rigidbody = GetComponent<Rigidbody>();
            GetComponent<HingeJoint>().connectedBody = gameObject.transform.parent.GetComponent<Rigidbody>();
        }
    }
}
