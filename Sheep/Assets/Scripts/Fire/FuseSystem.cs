using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FuseSystem : MonoBehaviour, IRespawnListener
{
    List<FuseLayer> fuseTree = new List<FuseLayer>();

    [NodeAPI("OnLastFuseBurnt")]
    public event Action onLastFuseBurnt;

    int endFusesCount = 0;
    int burntOutEndFusesCount = 0;

    void Start()
    {
        for (int x = 0; x < transform.childCount; x++)
        {
            Fuse fuse = transform.GetChild(x).GetComponent<Fuse>();
            if (fuse != null)
                FillTree(fuse, fuseTree);
        }
    }

    void FillTree(Fuse fuse, List<FuseLayer> layer)
    {
        layer.Add(new FuseLayer(fuse));
        var thisLayer = layer[layer.Count - 1];
        for (int x = 0; x < fuse.transform.childCount; x++)
        {
            Fuse childFuse = fuse.transform.GetChild(x).GetComponent<Fuse>();
            if (childFuse != null)
                FillTree(childFuse, thisLayer.childFuses);
        }

        //last fuse in chain
        if (thisLayer.childFuses.Count == 0)
        {
            thisLayer.fuse.onBurnt += EndFuseBurntOut;
            endFusesCount++;
        }
        else
        {
            foreach (var child in thisLayer.childFuses)
            {
                fuse.onBurnt += child.fuse.Ignite;
            }
        }
    }

    void EndFuseBurntOut()
    {
        burntOutEndFusesCount++;
        if (burntOutEndFusesCount == endFusesCount)
        {
            onLastFuseBurnt?.Invoke();
            ResetState();
        }
    }

    void ResetState()
    {
        burntOutEndFusesCount = 0;
    }

    public void OnDespawn()
    {

    }

    public void OnRespawn()
    {
        ResetState();
    }

    public class FuseLayer
    {
        public List<FuseLayer> childFuses = new List<FuseLayer>();
        public Fuse fuse;

        public FuseLayer(Fuse fuse)
        {
            this.fuse = fuse;
        }
    }
}
