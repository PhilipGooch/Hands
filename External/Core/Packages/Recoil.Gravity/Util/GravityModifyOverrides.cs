using NBG.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Recoil.Gravity
{
    /// <summary>
    /// This replaces the possible overrides for a body. Here you define id of gravity source and
    /// define a GravityState for that id. When an override with said id is applied,
    /// this gravity state will be used instead of the one defined by that override.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityModifyOverrides : MonoBehaviour, IManagedBehaviour
    {
        [SerializeField]
        private List<GravityState> overrideGravityModifications = new List<GravityState>();

        private int bodyId = -1;

        void IManagedBehaviour.OnLevelLoaded() { }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            bodyId = ManagedWorld.main.FindBody(GetComponent<Rigidbody>());

            for (int i=0; i<overrideGravityModifications.Count; i++)
            {
                GravitySystem.Instance.SetModifiedOverrideGravity(bodyId, overrideGravityModifications[i]);
            }
        }

        void IManagedBehaviour.OnLevelUnloaded() { }
    }
}
