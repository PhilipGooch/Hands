using UnityEngine;

public static class BoidHelper
{
    const int numViewDirections = 12;
    public static readonly Vector3[] Directions;

    static BoidHelper()
    {
        Directions = new Vector3[numViewDirections];
        Directions[0] = new Vector3(-1, 1, 1).normalized;
        Directions[1] = new Vector3(-1, -1, 1).normalized;
        Directions[2] = new Vector3(1, -1, 1).normalized;
        Directions[3] = new Vector3(1, 1, 1).normalized;
        Directions[4] = Vector3.up;
        Directions[5] = Vector3.left;
        Directions[6] = Vector3.down;
        Directions[7] = Vector3.right;
        Directions[8] = new Vector3(1, 1, -1).normalized;
        Directions[9] = new Vector3(-1, 1, -1).normalized;
        Directions[10] = new Vector3(-1, -1, -1).normalized; 
        Directions[11] = new Vector3(1, -1, -1).normalized; 
    }
}
