using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumable : MonoBehaviour
{
    [SerializeField]
    SingleShotParticleEffect consumeEffect;

    MeshRenderer mesh;
    DestructibleObject destructible;

    private void Awake()
    {
        mesh = GetComponentInChildren<MeshRenderer>();
        destructible = GetComponent<DestructibleObject>();
    }

    public void Consume()
    {
        if (destructible != null)
        {
            destructible.DestroyObject(transform.position, Vector3.up);
        }
        else
        {
            if (consumeEffect != null)
            {
                var meshEffect = consumeEffect as SingleShotParticleMeshEffect;
                if (meshEffect && mesh != null)
                {
                    var effectInstance = meshEffect.Create(transform.position, transform.rotation) as SingleShotParticleMeshEffect;
                    effectInstance.SetTargetMesh(mesh);
                }
                else
                {
                    consumeEffect.Create(transform.position, transform.rotation);
                }
            }
            Destroy(gameObject);
        }
    }
}
