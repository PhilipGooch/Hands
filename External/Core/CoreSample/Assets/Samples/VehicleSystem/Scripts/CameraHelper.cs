using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehiclesDemo
{
    public class CameraHelper : MonoBehaviour
    {
        public GameObject followObj;
        Vector3 offset;
        private void Start()
        {
            offset = followObj.transform.position - transform.position;
        }
        private void LateUpdate()
        {
            transform.position = followObj.transform.position - offset;
        }
    }
}
