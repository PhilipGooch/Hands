using System.Collections.Generic;
using UnityEngine;
using System;
using NBG.Actor;
using NBG.Core;
using Recoil;
using UnityEngine.InputSystem;

namespace CoreSample.ImpaleDemo
{
    public class NailToolPen : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        private static readonly int FadeOverride = Shader.PropertyToID("_FadeOverride");
        public event Action Shot;
        [SerializeField] Rigidbody rb = default;
        [SerializeField] Transform nailShotPosition = default;
        [SerializeField] List<GameObject> nailObjects;
        [SerializeField] private ActorSystem.IActor[] nails;
        [SerializeField] float timeBetweenShoots = .4f;
        [SerializeField] private float nailVelocity = 3;
        [SerializeField] private float nailSpinVelocity = 0.2f;
        [SerializeField] private float recoilVelocity = 0.6f;

        private float timeSinceLastShot = 0;
        private int poolID;

        public bool Enabled => isActiveAndEnabled;

        bool shoot;

        void Update()
        {
            if (Application.isEditor)
            {
                if (Keyboard.current.enterKey.isPressed)
                {
                    shoot = true;
                }
                else
                    shoot = false;

                if (Gamepad.current.xButton.isPressed)
                {
                    shoot = true;
                }
                else
                    shoot = false;
            }

        }

        public void OnFixedUpdate()
        {
            if (shoot)
            {
                if (timeSinceLastShot > timeBetweenShoots)
                {
                    timeSinceLastShot = 0;
                    if (ActorSystem.InstantiationModule.TrySpawnFromPool(poolID, out ActorSystem.IActor projectileActor))
                    {
                        //   var motionVector = new MotionVector(transform.right * nailSpinVelocity, transform.forward * nailVelocity);
                        var rig = projectileActor.ActorGameObject.GetComponent<Rigidbody>();
                        rig.velocity = transform.forward * nailVelocity;
                        rig.angularVelocity = transform.right * nailSpinVelocity;


                        //World.main.SetVelocity(projectileActor.PivotBodyID, motionVector);
                        //    StartCoroutine(wait(projectileActor));
                    }
                    Shot?.Invoke();
                }

            }



            timeSinceLastShot += Time.fixedDeltaTime;
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            nails = new ActorSystem.IActor[nailObjects.Count];
            for (int i = 0; i < nailObjects.Count; i++)
            {
                nails[i] = nailObjects[i].GetComponentInChildren<ActorSystem.IActor>();
            }

            var spawnPos = nailShotPosition.GetRigidWorldTransform();
            var spawnBodyID = ManagedWorld.main.FindBody(rb, false);
            poolID = ActorSystem.InstantiationModule.CreatePool(nails, spawnPos, spawnBodyID, 1);
        }

        public void OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        public void OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }



    }
}
