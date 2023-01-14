using System;
using System.Collections.Generic;
using System.Reflection;

namespace NBG.Core
{
    public static class AssemblyUtilities
    {
        /// <summary>
        /// Gets all types by name.
        /// </summary>
        /// <param name="name">Type name to look for.</param>
        /// <param name="excludedAssemblies">Assemblies to ignore.</param>
        /// /// <param name="includePrivateTypes">Whether to include not exported types.</param>
        /// <returns>List of compatible types.</returns>
        public static List<Type> GetAllTypesByName(string name, string[] excludedAssemblies = null, bool includePrivateTypes = false)
        {
            var result = new List<Type>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
#if NET_4_6
                if (asm is System.Reflection.Emit.AssemblyBuilder)
                    continue;
#endif
                if (asm.IsDynamic)
                    continue; // GetExportedTypes does not work on dynamic assemblies

                if (excludedAssemblies != null)
                {
                    var exclude = false;
                    foreach (var prefix in excludedAssemblies)
                    {
                        if (asm.GetName().FullName.StartsWith(prefix))
                        {
                            exclude = true;
                            break;
                        }
                    }
                    if (exclude)
                        continue;
                }

                var types = includePrivateTypes ? asm.GetTypes() : asm.GetExportedTypes();
                foreach (var exportedType in types)
                {
                    if (string.Compare(name, exportedType.Name, false, System.Globalization.CultureInfo.InvariantCulture) == 0)
                    {
                        result.Add(exportedType);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all types with an attribute.
        /// </summary>
        /// <param name="attributeType">Attribute type to look for.</param>
        /// <param name="excludedAssemblies">Assemblies to ignore.</param>
        /// /// <param name="includePrivateTypes">Whether to include not exported types.</param>
        /// <returns>List of compatible types.</returns>
        public static List<Type> GetAllTypesWithAttribute(Type attributeType, string[] excludedAssemblies = null, bool includePrivateTypes = false)
        {
            var result = new List<Type>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
#if NET_4_6
                if (asm is System.Reflection.Emit.AssemblyBuilder)
                    continue;
#endif
                if (asm.IsDynamic)
                    continue; // GetExportedTypes does not work on dynamic assemblies

                if (excludedAssemblies != null)
                {
                    var exclude = false;
                    foreach (var prefix in excludedAssemblies)
                    {
                        if (asm.GetName().FullName.StartsWith(prefix))
                        {
                            exclude = true;
                            break;
                        }
                    }
                    if (exclude)
                        continue;
                }

                var types = includePrivateTypes ? asm.GetTypes() : asm.GetExportedTypes();
                foreach (var exportedType in types)
                {
                    var attribute = exportedType.GetCustomAttribute(attributeType);
                    if (attribute != null)
                        result.Add(exportedType);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all classes derived from <baseClass>.
        /// </summary>
        /// <param name="baseClass">Type to check against.</param>
        /// <param name="excludedAssemblies">Assemblies to ignore.</param>
        /// /// <param name="includePrivateTypes">Whether to include not exported types.</param>
        /// <returns>List of compatible types.</returns>
        public static List<Type> GetAllDerivedClasses(this Type baseClass, string[] excludedAssemblies = null, bool includePrivateTypes = false)
        {
            var result = new List<Type>();
            
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
#if NET_4_6
                if (asm is System.Reflection.Emit.AssemblyBuilder)
                    continue;
#endif
                if (asm.IsDynamic)
                    continue; // GetExportedTypes does not work on dynamic assemblies

                if (excludedAssemblies != null)
                {
                    var exclude = false;
                    foreach (var prefix in excludedAssemblies)
                    {
                        if (asm.GetName().FullName.StartsWith(prefix))
                        {
                            exclude = true;
                            break;
                        }
                    }
                    if (exclude)
                        continue;
                }

                if (baseClass.IsInterface)
                {
                    var types = includePrivateTypes ? asm.GetTypes() : asm.GetExportedTypes();
                    foreach (var exportedType in types)
                    {
                        foreach (var interfaceType in exportedType.GetInterfaces())
                        {
                            if (baseClass == interfaceType)
                            {
                                result.Add(exportedType);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var types = includePrivateTypes ? asm.GetTypes() : asm.GetExportedTypes();
                    foreach (var exportedType in types)
                    {
                        if (exportedType.IsSubclassOf(baseClass))
                            result.Add(exportedType);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a single class derived derived from <baseClass>.
        /// Throws when there is not exactly one instance of it.
        /// </summary>
        /// <param name="baseClass">Type to check against.</param>
        /// <param name="excludedAssemblies">Assemblies to ignore.</param>
        /// /// <param name="includePrivateTypes">Whether to include not exported types.</param>
        /// <returns>A compatible type.</returns>
        public static Type GetSingleDerivedClass(this Type baseClass, string[] excludedAssemblies = null, bool includePrivateTypes = false)
        {
            var list = GetAllDerivedClasses(baseClass, excludedAssemblies, includePrivateTypes);
            if (list.Count == 0)
                throw new InvalidOperationException($"Could not find an implementation of {baseClass}.");
            else if (list.Count > 1)
                throw new InvalidOperationException($"Found {list.Count} implementations of {baseClass}.");
            return list[0];
        }
    }
}
