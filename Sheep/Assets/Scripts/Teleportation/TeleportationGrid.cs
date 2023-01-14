using System.Collections.Generic;
using UnityEngine;

public class TeleportationGrid : MonoBehaviour
{
    public Dictionary<Vector2Int, bool> GridDictionary { get; private set; }

    [SerializeField]
    private GameObject gridSquarePrefab;

    [SerializeField]
    [Tooltip("Grid squares will not spawn if there are no objects within this range.")]
    private float maxDistanceFromObject = 10;
    [SerializeField]
    [Tooltip("The distance grid squares will attempt to spawn from.")]
    private float boundaryDistance = 50;  

    //[SerializeField]  // TODO: disabled ability to change size for now as couldn't get ray collision working with larger square sizes. // (consider starting from 0,0 in the corner...)
    private int gridSquareSize = 1;

    public GameObject GridGameObject { get; private set; }

    public float BoundaryDistance => boundaryDistance;

    Collider[] tempColliders = new Collider[256];

    private void Start()
    {
        GridDictionary = new Dictionary<Vector2Int, bool>();
        GenerateGrid();
        Teleportation.Instance?.RegisterGrid(this);
    }

    private void OnDestroy()
    {
        Destroy(GridGameObject);
        Teleportation.Instance?.UnregisterGrid(this);
    }

    public GameObject GenerateGrid()
    {
        if (GridGameObject)
        {
            Destroy(GridGameObject);
        }

        Vector3 center = transform.position;

        GridGameObject = new GameObject();
        GridGameObject.transform.position = center;
        GridGameObject.name = "Grid";

        int layerMask = (int)Layers.Walls;
        Vector3 halfExtents = new Vector3((float)gridSquareSize / 2, 0.05f, (float)gridSquareSize / 2);
        Vector3 localScale = new Vector3((float)gridSquareSize / 10, 1.0f, (float)gridSquareSize / 10);
        for (int z = -(int)boundaryDistance; z < boundaryDistance; z += gridSquareSize)
        {
            for (int x = -(int)boundaryDistance; x < boundaryDistance; x += gridSquareSize)
            {
                Vector3 position = center + new Vector3(x, 0, z);
                int numberOfColliders;
                numberOfColliders = Physics.OverlapBoxNonAlloc(position, halfExtents, tempColliders, Quaternion.identity, layerMask);
                if (HitStaticCollider(tempColliders, numberOfColliders))
                {
                    GridDictionary[new Vector2Int(x, z)] = false;
                    continue;
                }
                numberOfColliders = Physics.OverlapSphereNonAlloc(position, maxDistanceFromObject, tempColliders, layerMask);
                if (numberOfColliders == 0)
                {
                    GridDictionary[new Vector2Int(x, z)] = false;
                    continue;
                }
                GameObject gridSquare = Instantiate(gridSquarePrefab);
                gridSquare.transform.position = position;
                gridSquare.transform.localScale = localScale;
                gridSquare.transform.parent = GridGameObject.transform;
                gridSquare.name = "gridSquare (" + x + ", " + z + ")";
                GridDictionary[new Vector2Int(x, z)] = true;
            }
        }
        return GridGameObject;
    }

    public float GetGridHeight()
    {
        Debug.Assert(GridGameObject);
        return GridGameObject.transform.position.y;
    }

    public bool IsPositionValid(Vector2Int gridPosition)
    {
        return GridDictionary.ContainsKey(gridPosition) && GridDictionary[gridPosition];
    }

    private bool HitStaticCollider(Collider[] colliders, int numberOfColliders)
    {
        for (int i = 0; i < numberOfColliders; i++)
        {
            if (!colliders[i]) return false;

            if (colliders[i].gameObject.isStatic)
            {
                return true;
            }
        }
        return false;
    }

    public void SetVisible(bool visible)
    {
        GridGameObject.SetActive(visible);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawCube(transform.position, new Vector3(boundaryDistance * 2, 0.01f, boundaryDistance * 2));
    }
}
