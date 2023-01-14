using NBG.Core.GameSystems;
using NBG.DebugUI;
using UnityEngine;

/// <summary>
/// Adds all systems to debug ui as toggles. Since it's very destructive
/// it's allowed only in editor with a special menu checkbox ticked
/// </summary>
public static class DebugSystemToggle
{
    private const string kSystemEnableToggle = "No Brakes Games/Systems/Toggle systems in debug UI";

    public static void Register()
    {
        if (!AllowRegistration())
            return;

        DebugUI.Get().RegisterBool("PhysX auto simulation", "FixedUpdate/PhysX systems", () => Physics.autoSimulation, (b) => Physics.autoSimulation = b);
        var world = GameSystemWorldDefault.Instance;
        var fixedGroup = world.GetExistingSystem<FixedUpdateSystemGroup>();
        RegRecur("FixedUpdate/PhysX systems", fixedGroup, 0);
        var updateGroup = world.GetExistingSystem<UpdateSystemGroup>();
        RegRecur("Update/LateUpdate systems", updateGroup, 0);
        var lateUpdateGroup = world.GetExistingSystem<LateUpdateSystemGroup>();
        RegRecur("Update/LateUpdate systems", lateUpdateGroup, 0);
    }

    private static void RegRecur(string cat, GameSystemGroup group, int indent)
    {
        DebugUI.Get().RegisterBool(new string('\t', indent) + group.GetType().Name, cat,
                () => group.Enabled, (b) => group.Enabled = b);

        indent++;

        foreach (var system in group.Systems)
        {
            if (system is GameSystemGroup systemGroup)
            {
                RegRecur(cat, systemGroup, indent);
            }
            else
            {
                DebugUI.Get().RegisterBool(new string('\t', indent) + system.GetType().Name, cat,
                    () => system.Enabled, (b) => system.Enabled = b);
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem(kSystemEnableToggle)]
    static void EnableToggle()
    {
        var isOnFlipped = !UnityEditor.SessionState.GetBool(kSystemEnableToggle, false);
        UnityEditor.SessionState.SetBool(kSystemEnableToggle, isOnFlipped);
        UnityEditor.Menu.SetChecked(kSystemEnableToggle, isOnFlipped);
    }
#endif

    /// <summary>
    /// For now we allow this toggle only with a custom menu item on.
    /// </summary>
    /// <returns></returns>
    private static bool AllowRegistration()
    {
#if UNITY_EDITOR
        return UnityEditor.SessionState.GetBool(kSystemEnableToggle, false);
#else
        return false;
#endif
    }
}
