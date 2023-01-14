using System;
using System.Reflection;
#if UNITY_EDITOR
using System.Runtime.CompilerServices;
#endif

namespace NBG.LogicGraph
{
    internal enum NodeBindingType
    {
        NBT_Call,
        NBT_Set,
        NBT_Get,
    }

    /// <summary>
    /// Custom binding for the LogicGraph to use.
    /// WIP!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property)]
    internal class NodeBindingAttribute : Attribute
    {
        public string Name { get; }
        public NodeBindingType BindingType { get; }

#if UNITY_EDITOR
        public string SourceFile { get; }
        public int SourceLine { get; }
#endif

        public NodeBindingAttribute(string name, NodeBindingType type
#if UNITY_EDITOR
            , [CallerFilePath] string sourceFile = ""
            , [CallerLineNumber] int sourceLine = 0
#endif
            )
        {
            this.Name = name;
            this.BindingType = type;
#if UNITY_EDITOR
            this.SourceFile = sourceFile;
            this.SourceLine = sourceLine;
#endif
        }

        public MethodInfo ResolveSource(MemberInfo attributeOwner)
        {
            var baseType = attributeOwner.DeclaringType;
            var members = baseType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var member in members)
            {
                var name = member.Name;
                if (name != Name)
                    continue;

                // Verify
                if (member.MemberType != MemberTypes.Method)
                    continue;
                var mi = (MethodInfo)member;

                var returnType = $"{mi.ReturnType.Name}_";
                var paramTypes = string.Empty;
                foreach (var param in mi.GetParameters())
                {
                    paramTypes = $"{paramTypes}_{param.ParameterType.Name}";
                }
                var bindingName = $"{UserlandBindings.Prefix}CALL_{returnType}{mi.Name}{paramTypes}";
                if (bindingName != attributeOwner.Name)
                    continue;

                var attr = mi.GetCustomAttribute<NodeAPIAttribute>();
                if (attr == null)
                    continue;

                return mi;
            }

            return null;
        }
    }
}
