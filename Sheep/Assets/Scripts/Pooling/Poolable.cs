using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poolable : MonoBehaviour
{
    public Poolable OriginalPrefab { get; private set; }

    static Dictionary<Poolable, Stack<Poolable>> allPoolables = new Dictionary<Poolable, Stack<Poolable>>();

    // Make sure to only call Create on prefabs and not instances of objects!
    public Poolable Create()
    {
        return GetPoolableInstance(this);
    }

    public Poolable Create(Vector3 position)
    {
        var instance = Create();
        instance.transform.position = position;
        return instance;
    }

    public Poolable Create(Vector3 position, Quaternion rotation)
    {
        var instance = Create(position);
        instance.transform.rotation = rotation;
        return instance;
    }

    public Poolable Create(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var instance = Create(position, rotation);
        instance.transform.localScale = scale;
        return instance;
    }

    // TODO: save poolables between scenes, react to scene load to deactivate existing poolables, 
    // remove no longer needed poolables based on level and warmup some poolables when loading a level
    Poolable GetPoolableInstance(Poolable prefab)
    {
        EnsurePoolableStackInitialized(prefab);
        var poolableStack = allPoolables[prefab];
        if (poolableStack.Count > 0)
        {
            Poolable obj = poolableStack.Pop();
            while(obj == null && poolableStack.Count > 0)
            {
                obj = poolableStack.Pop();
            }
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }

        // No poolable available, create a new one
        return InstantiatePoolable(prefab);
    }

    void EnsurePoolableStackInitialized(Poolable prefab)
    {
        if (!allPoolables.ContainsKey(prefab))
        {
            allPoolables[prefab] = new Stack<Poolable>();
        }
    }

    Poolable InstantiatePoolable(Poolable prefab)
    {
        var instance = Instantiate(prefab);
        instance.OriginalPrefab = prefab;
        return instance;
    }

    protected virtual void ResetState()
    {
        transform.position = Vector3.zero;
        transform.rotation = OriginalPrefab.transform.rotation;
        transform.localScale = OriginalPrefab.transform.localScale;
    }

    public void Deinstantiate()
    {
        if (OriginalPrefab)
        {
            gameObject.SetActive(false);
            ResetState();
            allPoolables[OriginalPrefab].Push(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void WarmupPool(int count)
    {
        // Create a bunch of poolables and instantly disable them, saving for later use
        // This helps avoid lagspikes when instantiating a bunch of poolables at once
        EnsurePoolableStackInitialized(this);
        for (int i = 0; i < count; i++)
        {
            var instance = InstantiatePoolable(this);
            instance.Deinstantiate();
        }
    }
}
