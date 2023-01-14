using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Recoil;
using NBG.Entities;
using NBG.Core.GameSystems;
using NBG.Core.Events;

public class BaseRecoilPlaymodeTest
{
    protected Transform parent;

    [SetUp]
    public void Setup()
    {
        ManagedWorld.Create(16);
        EntityStore.Create(10, 500);
        EventBus.Create();
        GameSystemWorldDefault.Create();
        RecoilSystems.Initialize(GameSystemWorldDefault.Instance);
        Threat.Initialize();
        var go = new GameObject("Parent");
        parent = go.transform;
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.DestroyImmediate(parent.gameObject);
        Threat.Dispose();
        SheepScareIterative.Dispose();
        RecoilSystems.Shutdown();
        GameSystemWorldDefault.Destroy();
        EventBus.Destroy();
        EntityStore.Destroy();
        ManagedWorld.Destroy();
    }
}
