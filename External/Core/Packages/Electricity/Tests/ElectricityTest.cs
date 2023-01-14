using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NBG.Electricity;
using UnityEngine.SceneManagement;

public class ElectricityTest
{

    public GameObject dummyObject;


    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        dummyObject = new GameObject("DummyObject");
    }

    [SetUp]
    public void Setup()
    {
        Electricity.CleanInstance();
    }

    [Test]
    public void Basic()
    {
        ElectricityProvider source = Create<ElectricityProvider>();
        source.power = 20.0f;

        OnOffDevice device = Create<OnOffDevice>();
        device.requiredAmperes = 20.0f;

        Electricity.Connect(source, device);
        Electricity.RebuildLogic();
        Electricity.Simulate();

        Assert.IsTrue(device.isOn);
    }

    [Test]
    public void SplitEnergy()
    {
        ElectricityProvider source = Create<ElectricityProvider>();
        source.power = 20.0f;

        OnOffDevice deviceA = Create<OnOffDevice>();
        deviceA.requiredAmperes = 20.0f;

        OnOffDevice deviceB = Create<OnOffDevice>();
        deviceB.requiredAmperes = 20.0f;

        Electricity.Connect(source, deviceA);
        Electricity.Connect(source, deviceB);
        Electricity.RebuildLogic();

        Electricity.Simulate();

        Assert.AreEqual(deviceA.Input, 10.0f);
        Assert.IsFalse(deviceA.isOn);
        Assert.IsFalse(deviceB.isOn);
    }

    [Test]
    public void Battery()
    {
        ElectricityProvider source = Create<ElectricityProvider>();
        source.power = 20.0f;

        OnOffDevice deviceA = Create<OnOffDevice>();
        deviceA.requiredAmperes = 20.0f;

        OnOffDevice deviceB = Create<OnOffDevice>();
        deviceB.requiredAmperes = 20.0f;

        Battery battery = Create<Battery>();
        battery.maxStorage = 100.0f;
        battery.outputAmperes = 40.0f;
        battery.inputSocket = Create<ElectricityReceiver>();

        Electricity.Connect(source, battery.inputSocket);
        Electricity.Connect(battery, deviceA);
        Electricity.Connect(battery, deviceB);
        Electricity.RebuildLogic();

        Electricity.Simulate();

        Assert.IsFalse(deviceA.isOn);
        Assert.IsFalse(deviceB.isOn);

        Electricity.Simulate();
        Assert.IsTrue(deviceA.isOn);
        Assert.IsTrue(deviceB.isOn);
    }

    private T Create<T>() where T : Component
    {
        return dummyObject.AddComponent<T>();
    }
}
