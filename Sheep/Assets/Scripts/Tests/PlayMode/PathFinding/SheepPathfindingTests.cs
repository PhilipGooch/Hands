using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Recoil;
using NBG.Entities;
using NBG.Core.GameSystems;

public class SheepPathfindingTests : BaseRecoilPlaymodeTest
{
    [UnityTest]
    public IEnumerator SheepPathfindingScaredForwardMovesForward()
    {
        yield return SetupSheepArea(1);
        var threatPos = new Vector3(0, 0, -1f);
        var threatRange = 2f;

        yield return CreateThreat(threatPos, threatRange, 60);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 0.95f, "Sheep position.z was incorrect!");
        Assert.Less(sheepPos.x, 0.05f, "Sheep position was.x incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathfindingScareToRightWallInFrontAvoided()
    {
        yield return SetupSheepArea(1);

        CreateCube(new Vector3(0,0,0.5f), new Vector3(1f, 1f, 0.25f), Quaternion.identity);

        var threatPos = new Vector3(-0.5f, 0.0f, -0.5f);
        var threatRange = 2f;

        yield return CreateThreat(threatPos, threatRange, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 0.95f, "Sheep position.z was incorrect!");
        Assert.Greater(sheepPos.x, 0.95f, "Sheep position was.x incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingScareDirectlyIntoWallAvoided()
    {
        yield return SetupSheepArea(1);

        CreateCube(new Vector3(0, 0, 0.5f), new Vector3(1f, 1f, 0.25f), Quaternion.identity);

        var threatPos = new Vector3(0f, 0.0f, -1f);
        var threatRange = 2f;

        yield return CreateThreat(threatPos, threatRange, 60);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 0.75f, "Sheep position.z was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingCornerScaredClockwiseAvoided()
    {
        yield return SetupSheepArea(1);
        CreateCorner();

        var threatPos = new Vector3(0.05f, 0f, -1f);
        var threatRange = 2f;

        yield return CreateThreat(threatPos, threatRange, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 0.75f, "Sheep position.z was incorrect!");
        Assert.Greater(sheepPos.x, 0.75f, "Sheep position.x was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingCornerScaredCounterClockwiseAvoided()
    {
        yield return SetupSheepArea(1);
        CreateCorner();

        var threatPos = new Vector3(1f, 0f, -0.1f);
        var threatRange = 2f;

        yield return CreateThreat(threatPos, threatRange, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Less(sheepPos.z, -0.75f, "Sheep position.z was incorrect!");
        Assert.Less(sheepPos.x, -0.5f, "Sheep position.x was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingDiagonallyInsideCornerScaringFromSideAvoidsCorner()
    {
        yield return SetupSheepArea(1);
        CreateCorner();

        yield return CreateThreat(new Vector3(0.5f, 0f, -0.5f), 2f, 15);
        yield return CreateThreat(new Vector3(0.0f, 0f, -0.5f), 2f, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.x, 0.75f, "Sheep position.x was incorrect!");
    }

    [UnityTest]
    [Ignore("Unstable - flip flops")]
    public IEnumerator SheepPathFindingScaredTowardsCornerDoesNotGetStuck()
    {
        yield return SetupSheepArea(1);
        CreateCorner();

        yield return CreateThreat(new Vector3(0.2f, 0f, -0.5f), 2f, 60);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.x, 0.75f, "Sheep position.x was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingScaredAlongWallDoesNotJitter()
    {
        yield return SetupSheepArea(1);
        CreateCube(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 1f, 5f), Quaternion.identity);

        yield return CreateThreat(new Vector3(1.0f, 0f, -0.25f), 5f, 60);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 1f, "Sheep position.z was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingScaredTowardsStairsClimbs()
    {
        yield return SetupSheepArea(1);
        CreateStairs(10);

        yield return CreateThreat(new Vector3(0.25f, 0f, -0.5f), 5f, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 1f, "Sheep position.z was incorrect!");
    }

    [UnityTest]
    public IEnumerator SheepPathFindingScaredTowardsWallWithStairsClimbs()
    {
        yield return SetupSheepArea(1);
        CreateCube(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 5f, 5f), Quaternion.identity);
        CreateStairs(10);

        yield return CreateThreat(new Vector3(0.25f, 0f, -0.25f), 5f, 120);

        var sheep = GameObject.FindObjectOfType<Sheep>();
        var sheepPos = sheep.head.position;
        Assert.Greater(sheepPos.z, 1f, "Sheep position.z was incorrect!");
    }

    IEnumerator CreateThreat(Vector3 position, float range, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Threat.AddRegularThreat(new BoxBoundThreat(position, range));
            var sheep = GameObject.FindObjectOfType<Sheep>();
            if (sheep != null)
            {
                var sheepMiddle = (sheep.head.position + sheep.tail.position) / 2f;
                Debug.DrawLine(position, sheepMiddle, Color.red);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator SetupSheepArea(int sheepCount)
    {
        // 0,0,0 will be exactly on the floor
        var floor = CreateCube(new Vector3(0, -0.5f, 0), new Vector3(10, 1, 10), Quaternion.identity);

        var managerGo = new GameObject("SheepManager");
        managerGo.transform.position = Vector3.zero;
        var manager = managerGo.AddComponent<SheepManager>();
        manager.sheepPrefab = GameParameters.Instance.sheepPrefab;
        manager.count = sheepCount;
        managerGo.transform.SetParent(parent);

        SetupCamera(new Vector3(0,5,0), Vector3.zero);

        for(int i = 0; i < sheepCount; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForFixedUpdate();

        if (sheepCount == 1)
        {
            manager.PlaceSheep(0, Vector3.zero, Quaternion.identity);
        }
    }

    void CreateCorner()
    {
        CreateCube(new Vector3(0.4f, 0, 0.1f), new Vector3(1f, 1f, 0.25f), Quaternion.identity);
        CreateCube(new Vector3(-0.2f, 0, 0.1f), new Vector3(0.25f, 1f, 2f), Quaternion.identity);
    }

    void CreateStairs(int count, float stairsHeight = 0.1f, float stairsLength = 0.25f)
    {
        for (int i = 0; i < count; i++)
        {
            CreateCube(new Vector3(0f, i * stairsHeight, i * stairsLength), new Vector3(3f, stairsHeight, stairsLength), Quaternion.identity);
        }
    }

    GameObject CreateCube(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.transform.localRotation = rotation;
        cube.layer = LayerUtils.GetLayerNumber(Layers.Wall);
        cube.transform.SetParent(parent);
        return cube;
    }

    GameObject SetupCamera(Vector3 position, Vector3 pointToLookAt)
    {
        // HACK: Sheep require main camera for animations
        var cameraGo = new GameObject("Camera");
        cameraGo.transform.position = position;
        cameraGo.transform.LookAt(pointToLookAt);
        var camera = cameraGo.AddComponent<Camera>();
        // Silence warnings about no listeners
        cameraGo.AddComponent<AudioListener>();
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(parent);
        return cameraGo;
    }
}
