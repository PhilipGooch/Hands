using UnityEngine;

public class TrainWagon : TrainBase
{
    private void FixedUpdate()
    {
        if (WasMovingLastFrame && !IsMoving)
            reBody.velocity = Vector3.zero;

        WasMovingLastFrame = IsMoving;
    }
}
