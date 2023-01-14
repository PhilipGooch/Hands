using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NBG.Audio
{
    // Game must provide an enum marked with [SurfaceTypeEnum]
    // Order of items in the enum should not be changed, because the values are serialized.
    // New items in the enum can be added.

    [AttributeUsage(AttributeTargets.Enum)]
    public class SurfaceTypeEnum : Attribute
    {
        public readonly int UnknownValue;
        public readonly int DefaultValue;
        
        public SurfaceTypeEnum(int unknownValue, int defaultValue)
        {
            UnknownValue = unknownValue;
            DefaultValue = defaultValue;
        }
    }

    [Serializable]
    public struct SurfaceType
    {
        public int id; //defaults to Unknown

        public SurfaceType(int id)
        {
            this.id = id;
        }

        public static implicit operator int(SurfaceType st) => st.id;
    }

    public static class SurfaceTypes
    {
        // Game specific Enum to use as surface types
        // Must contain "Unknown", "Default" and "Count" elements
        private static System.Type s_SurfaceTypeEnum;
        private static int[] s_TypeValues;
        private static string[] s_TypeNames;
        private static SurfaceTypeEnum s_Attribute;
        private static bool s_Initialized;

        public static System.Type TypeEnum => s_SurfaceTypeEnum;
        public static int[] TypeValues => s_TypeValues;
        public static string[] TypeNames => s_TypeNames;
        public static int MaxTypeValue { get; private set; }

        static Dictionary<PhysicMaterial, SurfaceType> materialMap = new Dictionary<PhysicMaterial, SurfaceType>();

        public static void EnsureInitialized()
        {
            if (s_Initialized)
                return;

            var types = Core.AssemblyUtilities.GetAllTypesWithAttribute(typeof(SurfaceTypeEnum));
            Debug.Assert(types.Count != 0, $"Could not locate an enum with [{typeof(SurfaceTypeEnum).FullName}] attribute.");
            Debug.Assert(types.Count <= 1, $"Found multiple enums with [{typeof(SurfaceTypeEnum).FullName}] attribute.");

            s_SurfaceTypeEnum = types[0];
            Debug.Assert(s_SurfaceTypeEnum.IsEnum, "Audio surface type must be an enum");
            s_Attribute = s_SurfaceTypeEnum.GetCustomAttribute<SurfaceTypeEnum>();

            s_TypeValues = (int[])Enum.GetValues(s_SurfaceTypeEnum);
            s_TypeNames = Enum.GetNames(s_SurfaceTypeEnum);

            var max = 0;
            foreach (var value in s_TypeValues)
                max = Math.Max(value, max);
            MaxTypeValue = max;

#if UNITY_EDITOR
            Debug.Log($"Audio surface type: {s_SurfaceTypeEnum.FullName} (MaxValue: {MaxTypeValue})");
#endif

            s_Initialized = true;
        }

        static public SurfaceType Resolve(PhysicMaterial physics)
        {
            EnsureInitialized();

            // default surface
            SurfaceType result = new SurfaceType(s_Attribute.DefaultValue);

            // if null return default
            if (physics == null)
                return result;

            // if already mapped return mapped value
            if (materialMap.TryGetValue(physics, out result))
                return result;

            var names = s_TypeNames;
            int count = names.Length;
            string physicName = physics.name;
            // else try mapping based on string name
            try
            {
                const string kSuffix = " (Instance)";
                String nm = physicName;
                if (nm.EndsWith(kSuffix))
                {
                    //Debug.LogErrorFormat("Had to trim suffix from \"{0}\" - some problem with PhysicsMaterial reference", nm);
                    nm = nm.Substring(0, nm.Length - kSuffix.Length);
                }

                for (int index = 0; index < count; ++index)
                {
                    if (string.Compare(names[index], nm, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = new SurfaceType(s_TypeValues[index]);
                        materialMap.Add(physics, result);
                        return result;
                    }
                }
            }
            catch
            {
            }

            // try mapping by substring
            for (int i = 0; i < count; i++)
            {
                if (physicName.Contains(names[i]))
                {
                    result = new SurfaceType(s_TypeValues[i]);
                    materialMap.Add(physics, result); // maybe?
                    return result;
                }
            }

            //Debug.LogError("No suface for physics material " + physics.name);
            return new SurfaceType(s_Attribute.UnknownValue); // fallback
        }
    }
}
