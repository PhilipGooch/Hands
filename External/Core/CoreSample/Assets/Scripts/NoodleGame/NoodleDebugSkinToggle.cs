using NBG.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noodles
{
    public class NoodleDebugSkinToggle : MonoBehaviour, INoodleDebugSkinToggle
    {
        public NoodleSkin skin;
        public NoodleSkin debugSkin;
        public bool showSkin { get; set; } = true;
        public bool showDebug { get; set; } = false;

        private void Start()
        {
            NoodleDebugSkinToggleAdapter.adapter = this;

            showSkin = true;
            showDebug = false;
            debugSkin.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (Keyboard.current == null)
                return;
            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (!showDebug)
            {
                showDebug = true;
                debugSkin.gameObject.SetActive(true);
                showSkin = false;
                skin.gameObject.SetActive(false);
            }
            else if (!showSkin)
            {
                showSkin = true;
                skin.gameObject.SetActive(true);
            }
            else
            {
                showDebug = false;
                debugSkin.gameObject.SetActive(false);
            }
            EntityStore.GetComponentData<NoodleData>(GetComponent<Noodle>().entity).debug = showDebug;
        }
    }
}
