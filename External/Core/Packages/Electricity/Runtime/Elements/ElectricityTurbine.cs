using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Electricity
{
    public class ElectricityTurbine : ElectricityProvider
    {
        public float outputPerRotation = 1.0f;

        private Rigidbody body;
        private float angularVelocity;
        protected override void Start()
        {
            base.Start();
            body = GetComponent<Rigidbody>();
        }
        public void Update()
        {
            angularVelocity = Mathf.Max(
                Mathf.Abs(body.angularVelocity.x),
                Mathf.Abs(body.angularVelocity.y),
                Mathf.Abs(body.angularVelocity.z));
        }
        public override void Tick()
        {
            base.Tick();
            Output = outputPerRotation * angularVelocity;
        }
    }
}
