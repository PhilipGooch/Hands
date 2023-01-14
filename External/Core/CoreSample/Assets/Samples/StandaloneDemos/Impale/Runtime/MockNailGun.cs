using NBG.Core;
using NBG.Impale;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class MockNailGun : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        public Rigidbody nail;

        public bool Enabled => isActiveAndEnabled;

        [SerializeField]
        float velocity;
        [SerializeField]
        Vector3 axis = Vector3.forward;
        [SerializeField]
        int fireOnFrame = 10;

        public void OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }
        public void OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        public void OnLevelLoaded()
        {
        }

        Vector3 startPos;

        [ContextMenu("Add Impulse and Torque")]
        void AddImpulseAndTorque()
        {
            nail.gameObject.SetActive(true);
            startPos = nail.transform.position;
            var dir = nail.transform.TransformDirection(axis);
            ReBody reBody = new ReBody(nail);
            //  var motionVector = new MotionVector(transform.right * 0.2f, dir.normalized * velocity);
            // Debug.Log($"dir {dir} || {dir.normalized} vel {dir.normalized * velocity}");

            // reBody.rigidbody.velocity = nail.transform.forward * velocity;
            //  reBody.angularVelocity = nail.transform.right * 0.2f;
            reBody.rigidbody.velocity = dir.normalized * velocity;
            // World.main.SetVelocity(reBody.Id, motionVector);
            fired = true;

            //StartCoroutine(waitToReset());
        }

        bool fired = false;

        IEnumerator waitToReset()
        {
            yield return new WaitForSeconds(2);
            nail.GetComponent<Impaler>().Unimpale();
            nail.transform.position = startPos;
            fired = false;
        }

        public void OnFixedUpdate()
        {
            if (Time.frameCount >= fireOnFrame && !fired)
                AddImpulseAndTorque();
        }

    }
}
