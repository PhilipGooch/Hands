using NBG.Core;
using Recoil;
using UnityEngine;
using System.Collections.Generic;
using NBG.Locomotion;
using UnityEngine.InputSystem;

namespace Experimental
{
    /// <summary>
    /// TODO: please fill out
    /// </summary>
    public class LocomotionExample : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        [SerializeField]
        List<BallLocomotion> agents;
        [SerializeField]
        LayerMask groundLayers;

        BallLocomotionSystem system;

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IManagedBehaviour.OnLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
            system = new BallLocomotionSystem(agents, groundLayers);
            system.AddLocomotionHandler(new Flocking());
            system.AddLocomotionHandler(new ObstacleAvoidance(groundLayers));
            system.AddLocomotionHandler(new EdgeAvoidance(groundLayers));
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
            system.Dispose();
            system = null;
        }

        void IOnFixedUpdate.OnFixedUpdate()
        {
            var horz = 0f;
            var vert = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed)
                {
                    horz = -1;
                }
                else if (keyboard.dKey.isPressed)
                {
                    horz = 1;
                }

                if (keyboard.wKey.isPressed)
                {
                    vert = 1;
                }
                else if (keyboard.sKey.isPressed)
                {
                    vert = -1;
                }

                var jump = keyboard.spaceKey.isPressed;
                for (int i = 0; i < agents.Count; i++)
                {
                    agents[i].SetInput(new Vector3(horz, 0f, vert).normalized, jump);
                }
            }

            system.UpdateLocomotion();
        }
    }
}
