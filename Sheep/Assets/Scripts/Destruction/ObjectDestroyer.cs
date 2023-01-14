using UnityEngine;

public class ObjectDestroyer : MonoBehaviour
{
    [SerializeField]
    PhysicalDestructionSource destroyerType;

    public PhysicalDestructionSource DestroyerType => destroyerType;

    PhysicalDestructionSource originalDestroyerType;

    private void Awake()
    {
        originalDestroyerType = destroyerType;
    }

    public void SetDestuctionActive(bool active)
    {
        if (active)
        {
            destroyerType = originalDestroyerType;
        }
        else
        {
            destroyerType = (PhysicalDestructionSource)0;
        }
    }

  
}
