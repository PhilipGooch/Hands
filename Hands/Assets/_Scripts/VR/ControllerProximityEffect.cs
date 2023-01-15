using System.Collections.Generic;
using UnityEngine;

namespace VR.System
{
    public class ControllerProximityEffect : MonoBehaviour
    {
        [SerializeField]
        Material targetMaterial;
        [SerializeField]
        float minProximityToShowController = 0.5f;
        [SerializeField]
        float maxAlphaProximity = 0.2f;
        Material materialInstance;
        List<Renderer> renderers = new List<Renderer>();
        IVRDataProvider dataProvider;

        void Awake()
        {
            dataProvider = GetComponentInParent<IVRDataProvider>();
            materialInstance = Instantiate(targetMaterial);
        }

        private void OnEnable()
        {
            dataProvider.ModelLoaded += UpdateAllMeshes;
            VRSystem.ControllerProximityChanged += UpdateProximity;
            UpdateAllMeshes();
        }

        private void OnDisable()
        {
            dataProvider.ModelLoaded -= UpdateAllMeshes;
            VRSystem.ControllerProximityChanged -= UpdateProximity;
        }

        private void OnDestroy()
        {
            Destroy(materialInstance);
        }

        void UpdateProximity(float proximity)
        {
            var color = materialInstance.color;
            var progress = (proximity - maxAlphaProximity) / minProximityToShowController;
            color.a = Mathf.Lerp(1f, 0f, progress);
            materialInstance.color = color;
        }

        void UpdateAllMeshes()
        {
            GetComponentsInChildren(true, renderers);
            foreach(var rend in renderers)
            {
                rend.sharedMaterial = materialInstance;
            }
        }
    }
}
