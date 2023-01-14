using NBG.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    public static class PlaneDestructionManager
    {
        public static bool IsHandlerAvailable => Handler != null;

        public static IPlaneDestructionHandler Handler
        {
            get
            {
                return _handler;
            }
            set{
                if(_handler != null)
                {
                    Debug.LogError("There's an existing handler in the singleton");
                }
                else
                {
                    _handler = value;
                }
            }
        }

        [ClearOnReload] private static IPlaneDestructionHandler _handler;
    }
}
