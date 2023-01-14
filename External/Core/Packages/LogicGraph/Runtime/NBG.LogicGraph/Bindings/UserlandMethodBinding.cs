using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace NBG.LogicGraph
{
    internal enum UserlandBindingMethodType
    {
        UBMT_Function,
        UBMT_PropertyGet,
        UBMT_PropertySet,
    }

    internal class UserlandMethodBinding : UserlandBinding
    {
        public override string Description { get => NodeAPI.Description; }

        internal struct Parameter
        {
            public ParameterInfo pi;
            public IVariantProvider variants;
        }

        public UserlandBindingMethodType MethodType { get; protected set; }
        public MethodInfo Source { get; private set; }
        public IReadOnlyList<Parameter> Parameters => _parameters;
        public NodeAPIAttribute NodeAPI { get; private set; }
        public UserlandBindings.BindingDelegate Func { get; private set; }

#if UNITY_EDITOR
        public override string SourceFile => NodeAPI.SourceFile;
        public override int SourceLine => NodeAPI.SourceLine;
#endif

        //public BindingType Type; // method type (call, get, set)
        string _funcName;
        List<Parameter> _parameters = new List<Parameter>();
        
        public bool HasReturnValues
        {
            get
            {
                if (Source != null)
                {
                    if (Source.ReturnType != typeof(void))
                        return true;

                    var p = Source.GetParameters();
                    if (p.Any(x => x.IsOut))
                        return true;
                }

                return false; //TODO: custom bindings (functions) need to declare this somehow
            }
        }

        public override string ToString()
        {
            var ret = Name;
            if (NodeAPI != null)
            {
                ret = $"{ret} \"{NodeAPI.Description}\"";
            }
            //ret = $"{ret} @{Type} @{FuncName}";
            ret = $"{ret} @{_funcName}";
            if (Source != null)
            {
                ret = $"{ret} on {Source.DeclaringType.FullName}";
            }
            return ret;
        }

        public UserlandMethodBinding(MethodInfo mi, UserlandBindingMethodType mt, System.Type masterType, NodeAPIAttribute attributeOverride = null)
        {
            var isExtension = (masterType != mi.DeclaringType);

            this.Type = UserlandBindingType.UBT_Method;
            this.MethodType = mt;
            this.TargetType = masterType;
            this.DeclaringType = mi.DeclaringType;
            this.Name = mi.Name;
            this.IsStatic = mi.IsStatic && !isExtension; // Don't clasify extension methods as static - they behave like instance methods

            this.Source = mi;

            // Extract parameters
            foreach (var pi in mi.GetParameters())
            {
                if (pi.Position == 0 && isExtension)
                    continue;

                var p = new Parameter();
                p.pi = pi;

                /*if (!pi.IsOut) // TODO: enable once variant UI is resolved
                {
                    var variants = pi.GetCustomAttribute<NodeParamVariantsAttribute>();
                    if (variants != null)
                    {
                        var variantsMI = mi.DeclaringType.GetMethod(variants.VariantsMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                        if (variantsMI == null)
                        {
                            UnityEngine.Debug.LogError($"Failed to find NodeParamVariants({variants.VariantsMethod})");
                        }
                        else
                        {
                            p.variants = Variants.CreateForVariableType(VariableTypes.FromSystemType(pi.ParameterType), variantsMI);
                        }
                    }
                }*/

                _parameters.Add(p);
            }

            NodeAPI = attributeOverride != null ? attributeOverride : mi.GetCustomAttribute<NodeAPIAttribute>();
            Assert.IsNotNull(this.NodeAPI);

            // Build the delegate
            _funcName = $"{UserlandBindings.Prefix}CALL_{mi.Name}";
            var delMi = this.DeclaringType.GetMethod(_funcName, UserlandBindings.GenBindingFlags);
            this.Func = (UserlandBindings.BindingDelegate)delMi.CreateDelegate(typeof(UserlandBindings.BindingDelegate));
        }
    }
}
