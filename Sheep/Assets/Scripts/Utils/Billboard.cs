using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(cameraTransform.position - transform.position);
        }
    }
}
