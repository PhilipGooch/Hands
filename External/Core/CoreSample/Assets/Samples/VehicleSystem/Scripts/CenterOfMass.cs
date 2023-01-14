using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclesDemo
{
    public class CenterOfMass : MonoBehaviour
    {
        public Rigidbody mainBody;
        public GameObject COM;

        // Start is called before the first frame update
        void Start()
        {
            mainBody.centerOfMass = mainBody.transform.InverseTransformPoint(COM.transform.position);
        }

        // Update is called once per frame
        void Update()
        {
            mainBody = GetComponent<Rigidbody>();
        }
    }
}
