using System;
using UnityEngine.LowLevel;

namespace NBG.Core.GameSystems
{
    /// <summary>
    /// Prevents a system from being automatically run.
    /// </summary>
    /// <remarks>
    /// By default, all systems (classes derived from <see cref="GameSystemBase"/>) are automatically discovered,
    /// instantiated, and added to the default <see cref="GameSystemWorld"/> when that World is created.
    ///
    /// Add this attribute to a system class that you want created automatically, but not added to any groups.
    /// Note that the attribute is not inherited by any subclasses.
    ///
    /// <code>
    /// using NBG.Core;
    ///
    /// [DisableAutoRegistration]
    /// public class CustomSystem : GameSystem
    /// { // Implementation... }
    /// </code>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisableAutoRegistrationAttribute : Attribute
    {
    }
}
