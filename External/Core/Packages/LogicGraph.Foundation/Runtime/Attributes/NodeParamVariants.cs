using System;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Provides a list of possible variants for a parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    /*public*/ class NodeParamVariantsAttribute : Attribute // TODO: enable once variant UI is resolved
    {
        public string VariantsMethod { get; }

        public NodeParamVariantsAttribute(string variantsMethod)
        {
            this.VariantsMethod = variantsMethod;
        }
    }
}
