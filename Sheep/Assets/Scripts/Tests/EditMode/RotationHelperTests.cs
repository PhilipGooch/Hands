using NUnit.Framework;
using UnityEngine;

public class RotationHelperTests
{
    readonly Vector3 rotatedChildPosition = new Vector3(0.7f, -0.7f, 0);
    readonly Vector3 unrotatedChildPosition = new Vector3(0, 0, 1);

    readonly Quaternion rootRotationOffset = Quaternion.Euler(new Vector3(45, 0, 0));
    readonly Quaternion rotatedRootRotation = Quaternion.Euler(new Vector3(45, 90, 0));

    Quaternion RotatedRootWithAdditionalOffset => rotatedRootRotation * rootRotationOffset;

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_DefaultValuesPositionTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, Quaternion.identity,
            Vector3.zero, Quaternion.identity,
            Vector3.zero, Quaternion.identity);

        Assert.Less(Vector3.Distance(pos, childTransform.position), 0.02f);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_ZeroTargetPositionOffsetPositionTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, RotatedRootWithAdditionalOffset,
            Vector3.zero, rotatedRootRotation,
            rotatedChildPosition, rotatedRootRotation);

        Assert.Less(Vector3.Distance(pos, childTransform.position), 0.02f);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_WithTargetPositionOffsetPositionTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            new Vector3(10, 0, 0), RotatedRootWithAdditionalOffset,
            Vector3.zero, rotatedRootRotation,
            rotatedChildPosition, rotatedRootRotation);

        Assert.Less(Vector3.Distance(pos, childTransform.position), 0.02f);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_IsOffsetPositionCorrectComparedToOffsetGameObject()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, RotatedRootWithAdditionalOffset,
            Vector3.zero, rotatedRootRotation,
            rotatedChildPosition, Quaternion.identity);

        Assert.Less(Vector3.Distance(pos, childTransform.position), 0.02f);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_ChildPositionOffsetDefaultStartingRotationRotationTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, RotatedRootWithAdditionalOffset,
            Vector3.zero, Quaternion.identity,
            unrotatedChildPosition, Quaternion.identity);

        Assert.Less(Quaternion.Angle(rot, childTransform.rotation), 0.02);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_ChildPositionOffsetWithStartingRotationTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, RotatedRootWithAdditionalOffset,
            Vector3.zero, rotatedRootRotation,
            rotatedChildPosition, rotatedRootRotation);

        Assert.Less(Quaternion.Angle(rot, childTransform.rotation), 0.02);
    }

    [Test]
    public void RotationHelper_GetOffsetPositionAndRotation_ZeroTargetPositionOffsetRotationTest()
    {
        (Vector3 pos, Quaternion rot, Transform childTransform) = TestPositionAndRotation(
            Vector3.zero, RotatedRootWithAdditionalOffset,
            Vector3.zero, rotatedRootRotation,
            rotatedChildPosition, rotatedRootRotation);

        Assert.Less(Quaternion.Angle(rot, childTransform.rotation), 0.02);
    }

    [Test]
    public void RotationHelper_GetRotationOffset_IsRotationOffsetCorrectSameRotations()
    {
        Quaternion rot = RotationHelper.GetRotationOffset(rotatedRootRotation, rotatedRootRotation);

        Assert.Less(Quaternion.Angle(rot, Quaternion.identity), 0.02);
    }

    [Test]
    public void RotationHelper_GetPositionOffset_IsPositionOffsetCorrect()
    {
        Vector3 pos = RotationHelper.GetPositionOffset(rotatedChildPosition, Vector3.zero);

        Assert.Less(Vector3.Distance(pos, rotatedChildPosition), 0.02f);
    }

    [Test]
    public void RotationHelper_GetDeltaRotation_IsDeltaRotationCorrect()
    {
        Quaternion rot = RotationHelper.GetDeltaRotation(RotatedRootWithAdditionalOffset, rotatedRootRotation);

        Assert.Less(Quaternion.Angle(rot, rootRotationOffset), 0.02);
    }

    (Vector3 pos, Quaternion rot, Transform childTransform) TestPositionAndRotation(
        Vector3 targetParentPosition, Quaternion targetParentRotation,
        Vector3 parentStartPosition, Quaternion parentStartRotation,
        Vector3 childStartPosition, Quaternion childStartRotation)
    {
        (Vector3 pos, Quaternion rot) = RotationHelper.GetOffsetPositionAndRotation(
            targetParentPosition, targetParentRotation,
            parentStartPosition, parentStartRotation,
            childStartPosition, childStartRotation);

        //child offset and rotation is different because its not rotated
        var childTransform = CreateAndGetATestGameObject(
            targetParentPosition, targetParentRotation,
            parentStartPosition, parentStartRotation,
            childStartPosition, childStartRotation);

        return (pos, rot, childTransform);
    }

    Transform CreateAndGetATestGameObject(
        Vector3 targetParentPosition, Quaternion targetParentRotation,
        Vector3 parentStartPosition, Quaternion parentStartRotation,
        Vector3 childStartPosition, Quaternion childStartRotation)
    {
        GameObject rootObj = new GameObject("root");
        GameObject childObj = new GameObject("child");

        rootObj.transform.position = parentStartPosition;
        rootObj.transform.rotation = parentStartRotation;

        childObj.transform.position = childStartPosition;
        childObj.transform.rotation = childStartRotation;

        childObj.transform.parent = rootObj.transform;

        rootObj.transform.position = targetParentPosition;
        rootObj.transform.rotation = targetParentRotation;

        return childObj.transform;
    }

}
