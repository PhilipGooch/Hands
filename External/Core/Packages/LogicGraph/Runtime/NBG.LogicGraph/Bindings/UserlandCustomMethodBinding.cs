using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace NBG.LogicGraph
{
    internal class UserlandCustomMethodBinding : UserlandBinding
    {
        public MethodInfo Source { get; private set; }
        public NodeBindingAttribute NodeBinding { get; private set; }
        public UserlandBindings.BindingDelegate Func { get; private set; }

#if UNITY_EDITOR
        public override string SourceFile => NodeBinding.SourceFile;
        public override int SourceLine => NodeBinding.SourceLine;
#endif

        //public BindingType Type; // method type (call, get, set)
        string _funcName;

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
            if (NodeBinding != null)
            {
                ret = $"{ret} \"{NodeBinding.Name}\"";
            }
            //ret = $"{ret} @{Type} @{FuncName}";
            ret = $"{ret} @{_funcName}";
            if (Source != null)
            {
                ret = $"{ret} on {Source.DeclaringType.FullName}";
            }
            return ret;
        }

        public UserlandCustomMethodBinding(MethodInfo mi)
        {
            this.Type = UserlandBindingType.UBT_CustomMethod;
            this.TargetType = mi.DeclaringType;
            this.DeclaringType = mi.DeclaringType;
            this.Name = mi.Name;
            this.IsStatic = mi.IsStatic;

            this.Source = mi;

            this.NodeBinding = mi.GetCustomAttribute<NodeBindingAttribute>();
            Assert.IsNotNull(this.NodeBinding);

            // Build the delegate
            _funcName = mi.Name;
            var delMi = this.DeclaringType.GetMethod(_funcName, UserlandBindings.GenBindingFlags);
            this.Func = (UserlandBindings.BindingDelegate)delMi.CreateDelegate(typeof(UserlandBindings.BindingDelegate));
        }
    }
}
