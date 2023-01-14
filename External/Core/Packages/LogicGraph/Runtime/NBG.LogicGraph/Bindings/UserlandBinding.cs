using System;

namespace NBG.LogicGraph
{
    internal enum UserlandBindingType
    {
        UBT_Method,
        UBT_Event,
        UBT_CustomMethod,
    }

    internal abstract class UserlandBinding
    {
        public UserlandBindingType Type { get; protected set; }

        public string Name { get; protected set; }
        public virtual string Description { get => Name; }

        /// <summary>
        /// Type for which the binding is for.
        /// </summary>
        public Type TargetType { get; protected set; }

        /// <summary>
        /// Type in which the binding is declared.
        /// Different from TargetType in case of extension methods.
        /// </summary>
        public Type DeclaringType { get; protected set; }
        
        public bool IsStatic { get; protected set; }
        public bool HideInUI { get; internal set; }
        public NodeConceptualType ConceptualType { get; internal set; }

        private string _categoryPath;
        public string CategoryPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_categoryPath))
                    return TargetType.Name;
                else
                    return _categoryPath;
            }
            internal set => _categoryPath = value;
        }

#if UNITY_EDITOR
        public abstract string SourceFile { get; }
        public abstract int SourceLine { get; }
#endif
    }
}
