using System;
using System.Collections.Generic;
using System.Reflection;

namespace NBG.LogicGraph
{
    interface IVariantProvider
    {
        public IReadOnlyList<string> Variants { get; }
    }

    internal class IVariantProviderTyped<T> : IVariantProvider
    {
        public IReadOnlyList<string> Variants => _variantNames;

        Func<IReadOnlyList<T>> _variants;

        IReadOnlyList<T> _variantValues;
        List<string> _variantNames;
        
        public IVariantProviderTyped(MethodInfo variantsMI)
        {
            _variants = (Func<IReadOnlyList<T>>)variantsMI.CreateDelegate(typeof(Func<IReadOnlyList<T>>));
            Refresh();
        }

        public void Refresh()
        {
            _variantValues = _variants();
            _variantNames = new List<string>(_variantValues.Count);
            for (int i = 0; i < _variantValues.Count; ++i)
            {
                _variantNames.Add(_variantValues[i].ToString());
            }
        }

        public int GetVariantIndexByName(string variantName)
        {
            for (int i = 0; i < _variantNames.Count; ++i)
            {
                if (_variantNames[i] == variantName)
                    return i;
            }

            return -1;
        }

        public int GetVariantIndex(T value)
        {
            for (int i = 0; i < _variantValues.Count; ++i)
            {
                if (_variantValues[i].Equals(value))
                    return i;
            }

            return -1;
        }

        public T GetVariantValue(int index)
        {
            if (index < 0 || index >= _variantValues.Count)
                index = 0;
            return _variantValues[index];
        }
    }

    static class Variants
    {
        public static IVariantProvider CreateForVariableType(VariableType variableType, MethodInfo variantsMI)
        {
            switch (variableType)
            {
                case VariableType.Bool:
                    return new IVariantProviderTyped<bool>(variantsMI);
                case VariableType.Int:
                    return new IVariantProviderTyped<int>(variantsMI);
                case VariableType.Float:
                    return new IVariantProviderTyped<float>(variantsMI);
                case VariableType.String:
                    return new IVariantProviderTyped<string>(variantsMI);
                case VariableType.UnityVector3:
                    return new IVariantProviderTyped<UnityEngine.Vector3>(variantsMI);
                case VariableType.UnityObject:
                    return new IVariantProviderTyped<UnityEngine.Object>(variantsMI);
                case VariableType.Quaternion:
                    return new IVariantProviderTyped<UnityEngine.Quaternion>(variantsMI);
                case VariableType.Color:
                    return new IVariantProviderTyped<UnityEngine.Color>(variantsMI);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
