using NUnit.Framework;
using UnityEngine;

public class RotatingSwitchTests
{
    [Test]
    public void GetCurrentStepPositive()
    {
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(0, 90), 0);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(89, 90), 1);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(90, 90), 1);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(91, 90), 1);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(359, 90), 4);
    }

    [Test]
    public void GetCurrentStepNegative()
    {
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(-1, 90), 0);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(-89, 90), -1);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(-91, 90), -1);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(-179, 90), -2);
        Assert.AreEqual(RotatingSwitch.GetCurrentStep(-180, 90), -2);
    }

    [Test]
    public void CanSnapAtAngle()
    {
        var shouldSnap = RotatingSwitch.ShouldSnapRotation(0.44f, 90f, 73f);
        Assert.AreEqual(shouldSnap.shouldSnap, true);
        Assert.AreEqual(shouldSnap.newSnapStep, 1);

        shouldSnap = RotatingSwitch.ShouldSnapRotation(0.44f, 90f, 93f);
        Assert.AreEqual(shouldSnap.shouldSnap, true);
        Assert.AreEqual(shouldSnap.newSnapStep, 1);

        shouldSnap = RotatingSwitch.ShouldSnapRotation(0.44f, 90f, -1f);
        Assert.AreEqual(shouldSnap.shouldSnap, true);
        Assert.AreEqual(shouldSnap.newSnapStep, 0);

        shouldSnap = RotatingSwitch.ShouldSnapRotation(0.44f, 90f, 10f);
        Assert.AreEqual(shouldSnap.shouldSnap, true);
        Assert.AreEqual(shouldSnap.newSnapStep, 0);

        shouldSnap = RotatingSwitch.ShouldSnapRotation(0.44f, 90f, -173);
        Assert.AreEqual(shouldSnap.shouldSnap, true);
        Assert.AreEqual(shouldSnap.newSnapStep, -2);

    }
}
