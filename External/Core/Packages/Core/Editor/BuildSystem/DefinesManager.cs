using System.Collections.Generic;
using UnityEditor;

namespace NBG.Core
{
    /// <summary>
    /// Helps manage scripting defines for build targets.
    /// </summary>
    public class DefinesManager
    {
        HashSet<string> _originalDefines;
        HashSet<string> _defines;

        /// <summary>
        /// Read defines from project settings into this instance.
        /// </summary>
        /// <param name="buildTargetGroup">Target scope.</param>
        public void Read(BuildTargetGroup buildTargetGroup)
        {
            _defines = new HashSet<string>();

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
            foreach (var define in defines)
            {
                _defines.Add(define);
            }

            _originalDefines = new HashSet<string>(_defines);
        }

        /// <summary>
        /// Write defines from this instance to project settings.
        /// </summary>
        /// <param name="buildTargetGroup">Target scope.</param>
        /// <returns>True if defined were changed.</returns>
        public bool Apply(BuildTargetGroup buildTargetGroup)
        {
            var combinedDefines = string.Join(";", _defines);
            if (_originalDefines.SetEquals(_defines))
            {
                UnityEngine.Debug.Log($"[{nameof(DefinesManager)}] '{buildTargetGroup}' scripting defines did not change: {combinedDefines}");
                return false;
            }
            else
            {
                UnityEngine.Debug.Log($"[{nameof(DefinesManager)}] Setting '{buildTargetGroup}' scripting defines to: {combinedDefines}");

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, combinedDefines);
                return true;
            }
        }

        /// <summary>
        /// Check if define is enabled in this instance.
        /// </summary>
        /// <param name="targetDefine">Target define.</param>
        /// <returns>True if define is enabled.</returns>
        public bool GetDefineEnabled(string targetDefine)
        {
            return _defines.Contains(targetDefine);
        }

        /// <summary>
        /// Set or clear a define in this instance.
        /// </summary>
        /// <param name="targetDefine">Target define.</param>
        /// <param name="enabled">Set if true, clear if false.</param>
        public void SetDefineEnabled(string targetDefine, bool enabled)
        {
            if (enabled)
            {
                _defines.Add(targetDefine);
            }
            else
            {
                _defines.Remove(targetDefine);
            }
        }
    }
}
