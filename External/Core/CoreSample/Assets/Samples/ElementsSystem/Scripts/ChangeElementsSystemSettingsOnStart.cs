using NBG.Core.GameSystems;
using NBG.ElementsSystem;
using UnityEngine;

namespace CoreSample.ElementsSystemDemo
{
    public class ChangeElementsSystemSettingsOnStart : MonoBehaviour
    {
        public LayerMask layerMask;
        public int maximumCollisionCountPerObject;

        private void Awake()
        {
            GameSystemWorldDefault.Instance.GetExistingSystem<ElementsGameSystem>().ResetUniqueElementID();
        }

        void Start()
        {
            var elementsSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ElementsGameSystem>();
            elementsSystem.layerMask = layerMask;
            elementsSystem.maximumObjectCountThatCanBeAffectedByOneObject = maximumCollisionCountPerObject;
        }
    }
}
