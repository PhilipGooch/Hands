using System;

namespace NBG.Core
{
    /// <summary>
    /// NBG_NO_DOMAIN_RELOAD must be defined for this attribute to work.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    public class ClearOnReloadAttribute : Attribute
    {
        public readonly object value;
        public readonly bool newInstance;

        /// <summary>
        ///     Marks field, property or event to be cleared on reload.
        /// </summary>
        public ClearOnReloadAttribute()
        {
            this.value = null;
            this.newInstance = false;
        }

        /// <summary>
        ///     Marks field of property to be cleared and assigned given value on reload.
        /// </summary>
        /// <param name="value">Explicit value which will be assigned to field/property on reload. Has to match field/property type. Has no effect on events.</param>
        public ClearOnReloadAttribute(object value)
        {
            this.value = value;
            this.newInstance = false;
        }

        /// <summary>
        ///     Marks field of property to be cleared or re-initialized on reload.
        /// </summary>
        /// <param name="newInstance">If true, field/property will be assigned a newly created object of its type on reload. Has no effect on events.</param>
        public ClearOnReloadAttribute(bool newInstance = false)
        {
            this.value = null;
            this.newInstance = newInstance;
        }
    }

    /// <summary>
    /// NBG_NO_DOMAIN_RELOAD must be defined for this attribute to work.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExecuteOnReloadAttribute : Attribute
    {
        /// <summary>
        ///     Marks method to be executed on reload.
        /// </summary>
        public ExecuteOnReloadAttribute() { }

    }
}
