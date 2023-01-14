using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Keyhole : MonoBehaviour
{
    [NodeAPI("OnUnlockStateChanged")]
    public event Action<bool> onUnlockStateChanged;
    public bool Unlocked { get; private set; }

    [SerializeField]
    private List<Key> acceptedKeys;
    [SerializeField]
    private TriggerEventSender keyMovementLockTrigger;
    [SerializeField]
    private TriggerEventSender lockUnlockedTrigger;

    [SerializeField]
    private Transform center;
    [SerializeField]
    private Rigidbody rootRigidbody;

    private Key activeKey;

    // Start is called before the first frame update
    private void Start()
    {
        keyMovementLockTrigger.onTriggerEnter += KeyMovementTriggerEnter;
        keyMovementLockTrigger.onTriggerExit += KeyMovementTriggerExit;

        lockUnlockedTrigger.onTriggerEnter += LockUnlockedTriggerEnter;
        lockUnlockedTrigger.onTriggerExit += LockUnlockedTriggerExit;
    }

    private void OnDestroy()
    {
        keyMovementLockTrigger.onTriggerEnter -= KeyMovementTriggerEnter;
        keyMovementLockTrigger.onTriggerExit -= KeyMovementTriggerExit;

        lockUnlockedTrigger.onTriggerEnter -= LockUnlockedTriggerEnter;
        lockUnlockedTrigger.onTriggerExit -= LockUnlockedTriggerExit;
    }

    private bool IsAcceptedKey(Collider obj)
    {
        Key key = obj.attachedRigidbody.GetComponent<Key>();
        return key != null && activeKey == key && acceptedKeys.Contains(activeKey);
    }

    private void LockUnlockedTriggerEnter(Collider obj)
    {
        if (obj.attachedRigidbody != null)
        {
            if (IsAcceptedKey(obj))
            {
                Unlocked = true;
                onUnlockStateChanged?.Invoke(Unlocked);
            }
        }
    }

    private void LockUnlockedTriggerExit(Collider obj)
    {
        if (obj.attachedRigidbody != null)
        {
            if (IsAcceptedKey(obj))
            {
                Unlocked = false;
                onUnlockStateChanged?.Invoke(Unlocked);
            }
        }
    }

    private void KeyMovementTriggerEnter(Collider obj)
    {
        if (obj.attachedRigidbody != null)
        {
            Key key = obj.attachedRigidbody.GetComponent<Key>();
            if (key != null && activeKey == null)
            {
                activeKey = key;
                activeKey.LockBlade(center, IsAcceptedKey(obj), rootRigidbody);

            }
        }
    }

    private void KeyMovementTriggerExit(Collider obj)
    {
        if (obj.attachedRigidbody != null)
        {
            Key key = obj.attachedRigidbody.GetComponent<Key>();
            if (key != null && key == activeKey)
            {
                activeKey.UnlockBlade();
                activeKey = null;
            }
        }
    }

}
