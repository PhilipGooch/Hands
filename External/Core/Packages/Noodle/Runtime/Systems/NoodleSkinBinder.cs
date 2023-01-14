using NBG.Core;
using NBG.Core.GameSystems;
using NBG.DebugUI;
using System.Collections.Generic;

namespace Noodles
{
    [UpdateInGroup(typeof(LateUpdateSystemGroup))]
    public class NoodleSkinBinder : GameSystem
    {
        private bool showDebugPose = false;
        private IDebugItem debugPoseToggle;

        protected override void OnCreate()
        {
            base.OnCreate();
            debugPoseToggle = DebugUI.Get().RegisterBool("Show debug pose", "Debug", () => { return showDebugPose; }, (bool b) => { showDebugPose = b; });
        }

        public bool Toggle() => showDebugPose = !showDebugPose;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DebugUI.Get()?.Unregister(debugPoseToggle);
        }

        HashSet<string> _disabledSkinsWarnings = new HashSet<string>();

        protected override void OnUpdate()
        {
            for (int i = 0; i < NoodleSkin.s_Skins.Count; i++)
            {
                var skin = NoodleSkin.s_Skins[i];
                if (!skin.IsAlive)
                {
                    var path = skin.gameObject.GetFullPath();
                    if (!_disabledSkinsWarnings.Contains(path))
                    {
                        _disabledSkinsWarnings.Add(path);
                        UnityEngine.Debug.LogWarning($"{nameof(NoodleSkinBinder)} can't update {nameof(NoodleSkin)} which is not alive: {path}");
                    }
                    continue;
                }

                if (skin.isActiveAndEnabled)
                {
                    if (showDebugPose)
                        skin.BindPose();
                    else
                        skin.BindRig();
                }
            }
        }
    }
}
