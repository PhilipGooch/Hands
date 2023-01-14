using NBG.LogicGraph;
using Recoil;
using UnityEngine;

public class InstantiateOnStart : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;
    [SerializeField]
    int objectsCount = 10;
    [Tooltip("max velocity used to scatter spawned objects around, 0 - no scatter")]
    [SerializeField]
    float maxScatterVelocity = 10;

    void Start()
    {
        for (int i = 0; i < objectsCount; i++)
        {
            var objectInstance = Instantiate(prefab, transform.position, Quaternion.identity);

            RigidbodyRegistration.RegisterHierarchy(objectInstance);

            var rig = objectInstance.GetComponent<Rigidbody>();
            if (rig != null)
            {
                ReBody reBody = new ReBody(rig);

                var dir = (Random.insideUnitSphere * maxScatterVelocity);
                reBody.AddForce(dir, ForceMode.VelocityChange);
            }
        }

    }
}
