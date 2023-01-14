using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    public interface IBreakableWallSubscription
    {
        void OnBreakableWallChanged(BreakableWall wall);
    }
}
