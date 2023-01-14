using System;
using UnityEngine.LowLevel;

namespace NBG.Core.GameSystems
{
    /// <summary>
    /// Prevents a system from being automatically created and run.
    /// </summary>
    /// <remarks>
    /// By default, all systems (classes derived from <see cref="GameSystemBase"/>) are automatically discovered,
    /// instantiated, and added to the default <see cref="GameSystemWorld"/> when that World is created.
    ///
    /// Add this attribute to a system class that you do not want created automatically. Note that the attribute is not
    /// inherited by any subclasses.
    ///
    /// <code>
    /// using NBG.Core;
    ///
    /// [DisableAutoCreation]
    /// public class CustomSystem : GameSystem
    /// { // Implementation... }
    /// </code>
    ///
    /// You can also apply this attribute to an entire assembly to prevent any system class in that assembly from being
    /// created automatically. This is useful for test assemblies containing many systems that expect to be tested
    /// in isolation.
    ///
    /// To declare an assembly attribute, place it in any C# file compiled into the assembly, outside the namespace
    /// declaration:
    /// <code>
    /// using NBG.Core;
    ///
    /// [assembly: DisableAutoCreation]
    /// namespace Tests{}
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class DisableAutoCreationAttribute : Attribute
    {
    }
}
