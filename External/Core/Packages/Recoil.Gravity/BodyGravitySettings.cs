using System.Collections.Generic;
using Unity.Mathematics;

namespace Recoil.Gravity
{
    /// <summary>
    /// Object gravity types:
    /// None - no gravity through recoil, no gravity through unity
    /// GlobalDefault - applies unity gravity
    /// Custom - no gravity through unity, custom gavity through recoil
    /// </summary>
    public enum GravityType
    {
        None = 0,
        GlobalDefault = 1,
        Custom = 2,
    }

    /// <summary>
    /// Fully defines a gravity state. What type of gravity it is, custom velocity vector
    /// for custom gravity and ID for the source of this gravity that is used for conditional overrides
    /// </summary>
    [System.Serializable]
    public struct GravityState
    {
        public int gravityId;
        public GravityType gravityType;
        public float3 customGravity;

        public GravityState(int gravityId, GravityType gravityType, float3 customGravity)
        {
            this.gravityId = gravityId;
            this.gravityType = gravityType;
            this.customGravity = customGravity;
        }
    }

    /// <summary>
    /// Defines gravity properties of an object. This includes the main gravity state, the override gravity state
    /// and settings such as whether an override is allowed and whether an override is currently being applied
    /// Is a class and not a struct, because 'mainGravity' state doesn't change almost ever, while 'overrideGravity' changes constantly.
    /// </summary>
    internal class BodyGravitySettings
    {
        public bool mainCanBeOverriden = true;
        public bool overrideExists = false;

        public GravityState mainGravity;
        public GravityState overrideGravity;

        // This is a very situational property so we avoid having it exist by default.
        private Dictionary<int, GravityState> modifiedOverrideGravities;

        public GravityState CurrentGravity
        {
            get
            {
                if (mainCanBeOverriden && overrideExists)
                {
                    if (modifiedOverrideGravities != null && modifiedOverrideGravities.TryGetValue(overrideGravity.gravityId, out GravityState modifiedOverride))
                    {
                        return modifiedOverride;
                    }
                    else
                    {
                        return overrideGravity;
                    }
                }
                else
                {
                    return mainGravity;
                }
            }
        }

        public void AddOverrideGravityModify(GravityState gravityState)
        {
            if (modifiedOverrideGravities == null)
            {
                modifiedOverrideGravities = new Dictionary<int, GravityState>();
            }
            modifiedOverrideGravities.Add(gravityState.gravityId, gravityState);
        }

        public BodyGravitySettings(GravityState mainGravity, GravityState overrideGravity, bool mainCanBeOverriden)
        {
            this.mainGravity = mainGravity;
            this.overrideGravity = overrideGravity;

            this.mainCanBeOverriden = mainCanBeOverriden;
        }
    }
}
