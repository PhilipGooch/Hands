using System;

public interface IBlockableInteractable
{
    public bool ActivatorBlocked { get; set; }

    public Action OnTryingToMoveBlockedActivator { get; set; }
}
