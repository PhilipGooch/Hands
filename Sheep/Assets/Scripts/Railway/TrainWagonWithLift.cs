using NBG.LogicGraph;
using UnityEngine;

public enum LiftDirection
{
    Up,
    Down
}

public class TrainWagonWithLift : TrainWagon
{
    [SerializeField]
    private WagonLiftPlatform wagonLiftActivator;

    private void Awake()
    {
        wagonLiftActivator.onLoweredAndNotMoving += UnlockMovement;
    }

    [NodeAPI("TryMoveLift")]
    public void RaiseElevator(bool liftedState)
    {
        TryMoveLift(liftedState ? LiftDirection.Up : LiftDirection.Down);
    }

    public void TryMoveLift(LiftDirection direction)
    {
        switch (direction)
        {
            case LiftDirection.Up:
                RaisePlatform();
                break;
            case LiftDirection.Down:
                LowerPlatform();
                break;
            default:
                break;
        }
    }

    private void LowerPlatform()
    {
        wagonLiftActivator.Lower();
    }

    private void RaisePlatform()
    {
        LockMovement();
        wagonLiftActivator.Raise();
    }

    internal override void UnlockMovement()
    {
        if (wagonLiftActivator.State == PlatformState.AT_START)
            base.UnlockMovement();
    }
}
