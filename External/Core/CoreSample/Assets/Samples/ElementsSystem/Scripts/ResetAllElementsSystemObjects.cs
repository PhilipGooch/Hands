using NBG.Core;
using NBG.Core.GameSystems;
using NBG.ElementsSystem;
using Recoil;
using UnityEngine;

namespace CoreSample.ElementsSystemDemo
{
    public class ResetAllElementsSystemObjects : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        public bool resetNow;

        ElementsGameSystem elementsSystem;

        void IManagedBehaviour.OnLevelLoaded()
        {
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);

            elementsSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ElementsGameSystem>();
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;
        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (resetNow)
            {
                resetNow = false;

                foreach (var element in elementsSystem.GetAllRegisteredObjects())
                {
                    element.ResetState();
                }
            }
        }
    }
}
