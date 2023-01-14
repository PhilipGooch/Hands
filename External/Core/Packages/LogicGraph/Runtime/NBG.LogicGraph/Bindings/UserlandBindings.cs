using NBG.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;

namespace NBG.LogicGraph
{
    internal static class UserlandBindings
    {
        public const string Prefix = "_LogicGraph_Userland_Binding_";

        public delegate void BindingDelegate(object target, IStack context);
        const BindingFlags MethodBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        const BindingFlags PropertyBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        const BindingFlags EventBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        internal const BindingFlags GenBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;


        [ClearOnReload(newInstance: true)]
        static Dictionary<Type, List<UserlandBinding>> _bindings = new Dictionary<Type, List<UserlandBinding>>();

        public static IReadOnlyDictionary<Type, List<UserlandBinding>> Bindings => _bindings;

        static UserlandBindings()
        {
            Initialize();
        }

        [ExecuteOnReload]
        static void Initialize()
        {
            const string kFoundationName = "NBG.LogicGraph.Foundation";

            var sw = Stopwatch.StartNew();
            int count = 0;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
#if NET_4_6
                if (asm is System.Reflection.Emit.AssemblyBuilder)
                    continue;
#endif
                if (asm.IsDynamic)
                    continue; // GetExportedTypes does not work on dynamic assemblies

                var refs = asm.GetReferencedAssemblies();
                var actual = refs.Any(x => x.Name.Contains(kFoundationName));
                if (!actual)
                    continue;

                foreach (var exportedType in asm.GetExportedTypes())
                {
                    count += InitializeType(exportedType);
                }
            }
            UnityEngine.Debug.Log($"[UserlandBindings] {count} bindings initialized in {sw.Elapsed}");
        }

        static int InitializeType(Type type)
        {
            bool hideTypeInUI = (type.GetCustomAttribute<NodeHideInUIAttribute>() != null);
            var typeConceptualTypeAttr = type.GetCustomAttribute<NodeConceptualTypeAttribute>();
            var typeCategoryPathAttr = type.GetCustomAttribute<NodeCategoryPathAttribute>();

            int count = 0;
            var mis = type.GetMethods(MethodBindingFlags);
            foreach (var mi in mis)
            {
                // Standard functions with [NodeAPI]
                var nodeAPIAttribute = mi.GetCustomAttribute<NodeAPIAttribute>();
                if (nodeAPIAttribute != null)
                {
                    var masterType = type;
                    var isExtension = (mi.GetCustomAttribute<ExtensionAttribute>() != null);
                    if (isExtension)
                    {
                        masterType = mi.GetParameters()[0].ParameterType;
                    }

                    List<UserlandBinding> ubs;
                    if (!_bindings.TryGetValue(masterType, out ubs))
                    {
                        ubs = new List<UserlandBinding>();
                        _bindings.Add(masterType, ubs);
                    }

                    try
                    {
                        bool hideInUI = hideTypeInUI | (mi.GetCustomAttribute<NodeHideInUIAttribute>() != null);

                        var conceptualTypeAttr = mi.GetCustomAttribute<NodeConceptualTypeAttribute>();
                        var categoryPathAttr = mi.GetCustomAttribute<NodeCategoryPathAttribute>();
                        string categoryPath = categoryPathAttr != null ? categoryPathAttr.Path :
                            typeCategoryPathAttr != null ? typeCategoryPathAttr.Path : null;

                        var binding = new UserlandMethodBinding(mi, UserlandBindingMethodType.UBMT_Function, masterType);
                        binding.HideInUI = hideInUI;
                        binding.ConceptualType = binding.HasReturnValues ? NodeConceptualType.Getter : NodeConceptualType.Function;
                        if (nodeAPIAttribute.Flags.HasFlag(NodeAPIFlags.ForceFlowNode))
                            binding.ConceptualType = NodeConceptualType.Function;
                        binding.CategoryPath = categoryPath;
                        ubs.Add(binding);
                        ++count;

                        // Override conceptual type
                        NodeConceptualType conceptualType = conceptualTypeAttr != null ? conceptualTypeAttr.Type :
                            typeConceptualTypeAttr != null ? typeConceptualTypeAttr.Type : NodeConceptualType.Undefined;
                        if (conceptualType != NodeConceptualType.Undefined)
                            binding.ConceptualType = conceptualType;
                        //UnityEngine.Debug.Log($"[UserlandBindings] Registered {binding}");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"UserlandBindings failed to resolve {mi.DeclaringType.Name}.{mi.Name}");
                        UnityEngine.Debug.LogException(e);
                    }
                }

                // Custom functions with [NodeBinding]
                var nodeBindingAttribute = mi.GetCustomAttribute<NodeBindingAttribute>();
                if (nodeBindingAttribute != null)
                {
                    List<UserlandBinding> ubs;
                    if (!_bindings.TryGetValue(type, out ubs))
                    {
                        ubs = new List<UserlandBinding>();
                        _bindings.Add(type, ubs);
                    }

                    try
                    {
                        bool hideInUI = hideTypeInUI | (mi.GetCustomAttribute<NodeHideInUIAttribute>() != null);
                        var conceptualTypeAttr = mi.GetCustomAttribute<NodeConceptualTypeAttribute>();
                        var categoryPathAttr = mi.GetCustomAttribute<NodeCategoryPathAttribute>();
                        NodeConceptualType conceptualType = conceptualTypeAttr != null ? conceptualTypeAttr.Type :
                            typeConceptualTypeAttr != null ? typeConceptualTypeAttr.Type : NodeConceptualType.Undefined;
                        string categoryPath = categoryPathAttr != null ? categoryPathAttr.Path :
                            typeCategoryPathAttr != null ? typeCategoryPathAttr.Path : null;

                        var binding = new UserlandCustomMethodBinding(mi);
                        binding.HideInUI = hideInUI;
                        binding.ConceptualType = conceptualType;
                        binding.CategoryPath = categoryPath;
                        ubs.Add(binding);
                        ++count;
                        //UnityEngine.Debug.Log($"[UserlandBindings] Registered {binding}");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"UserlandBindings failed to resolve {mi.DeclaringType.Name}.{mi.Name}");
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            // Standard events with [NodeAPI]
            var eis = type.GetEvents(EventBindingFlags);
            foreach (var ei in eis)
            {
                var attribute = ei.GetCustomAttribute<NodeAPIAttribute>();
                if (attribute != null)
                {
                    List<UserlandBinding> ubs;
                    if (!_bindings.TryGetValue(type, out ubs))
                    {
                        ubs = new List<UserlandBinding>();
                        _bindings.Add(type, ubs);
                    }

                    try
                    {
                        bool hideInUI = hideTypeInUI | (ei.GetCustomAttribute<NodeHideInUIAttribute>() != null);
                        var conceptualTypeAttr = ei.GetCustomAttribute<NodeConceptualTypeAttribute>();
                        var categoryPathAttr = ei.GetCustomAttribute<NodeCategoryPathAttribute>();
                        NodeConceptualType conceptualType = conceptualTypeAttr != null ? conceptualTypeAttr.Type :
                            typeConceptualTypeAttr != null ? typeConceptualTypeAttr.Type : NodeConceptualType.Undefined;
                        string categoryPath = categoryPathAttr != null ? categoryPathAttr.Path :
                            typeCategoryPathAttr != null ? typeCategoryPathAttr.Path : null;

                        var binding = new UserlandEventBinding(ei);
                        binding.HideInUI = hideInUI;
                        binding.ConceptualType = conceptualType;
                        binding.CategoryPath = categoryPath;
                        ubs.Add(binding);
                        ++count;
                        //UnityEngine.Debug.Log($"[UserlandBindings] Registered {binding}");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"UserlandBindings failed to resolve {ei.DeclaringType.Name}.{ei.Name}");
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            // Properties with [NodeAPI]
            var pis = type.GetProperties(PropertyBindingFlags);
            foreach (var pi in pis)
            {
                var attribute = pi.GetCustomAttribute<NodeAPIAttribute>();
                if (attribute != null)
                {
                    var masterType = type;

                    List<UserlandBinding> ubs;
                    if (!_bindings.TryGetValue(masterType, out ubs))
                    {
                        ubs = new List<UserlandBinding>();
                        _bindings.Add(masterType, ubs);
                    }

                    try
                    {
                        bool hideInUI = hideTypeInUI | (pi.GetCustomAttribute<NodeHideInUIAttribute>() != null);

                        var conceptualTypeAttr = pi.GetCustomAttribute<NodeConceptualTypeAttribute>();
                        var categoryPathAttr = pi.GetCustomAttribute<NodeCategoryPathAttribute>();
                        string categoryPath = categoryPathAttr != null ? categoryPathAttr.Path :
                            typeCategoryPathAttr != null ? typeCategoryPathAttr.Path : null;

                        // Override conceptual type
                        NodeConceptualType conceptualType = conceptualTypeAttr != null ? conceptualTypeAttr.Type :
                            typeConceptualTypeAttr != null ? typeConceptualTypeAttr.Type : NodeConceptualType.Undefined;

                        // get_*
                        if (pi.GetMethod != null && pi.GetMethod.IsPublic)
                        {
                            var getBinding = new UserlandMethodBinding(pi.GetMethod, UserlandBindingMethodType.UBMT_PropertyGet, masterType, attribute);
                            getBinding.HideInUI = hideInUI;
                            getBinding.ConceptualType = NodeConceptualType.Getter;
                            getBinding.CategoryPath = categoryPath;
                            if (conceptualType != NodeConceptualType.Undefined)
                                getBinding.ConceptualType = conceptualType;
                            ubs.Add(getBinding);
                            ++count;
                            //UnityEngine.Debug.Log($"[UserlandBindings] Registered {binding}");
                        }

                        // set_*
                        if (pi.SetMethod != null && pi.SetMethod.IsPublic)
                        {
                            var setBinding = new UserlandMethodBinding(pi.SetMethod, UserlandBindingMethodType.UBMT_PropertySet, masterType, attribute);
                            setBinding.HideInUI = hideInUI;
                            setBinding.ConceptualType = NodeConceptualType.Function;
                            setBinding.CategoryPath = categoryPath;
                            if (conceptualType != NodeConceptualType.Undefined)
                                setBinding.ConceptualType = conceptualType;
                            ubs.Add(setBinding);
                            ++count;
                            //UnityEngine.Debug.Log($"[UserlandBindings] Registered {binding}");
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"UserlandBindings failed to resolve {pi.DeclaringType.Name}.{pi.Name}");
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            return count;
        }

        public static IEnumerable<UserlandBinding> GetStrict(Type type)
        {
            if (_bindings.TryGetValue(type, out List<UserlandBinding> bindings))
            {
                return bindings;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<UserlandBinding> Get(Type type) => GetWithAncestors(type);

        public static IEnumerable<UserlandBinding> GetWithAncestors(Type type)
        {
            while (type != null)
            {
                if (_bindings.TryGetValue(type, out List<UserlandBinding> bindings))
                {
                    for (int i = 0; i < bindings.Count; ++i)
                        yield return bindings[i];
                }

                type = type.BaseType;
            }
        }
    }
}
