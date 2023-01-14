using NBG.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NBG.LogicGraph
{
    [Serializable]
    class SerializableVariableEntry
    {
        public string Name;
        public VariableType Type;
        public string Value;
    }

    [Serializable]
    class SerializableNodeEntry
    {
        public string NodeType;
        
        public string BindingName;
        public string BindingType;
        public bool BindingStatic;
        public SerializableGuid Target;

        public List<SerializableNodePropertyEntry> Properties;

        public List<SerializableFlowOutputEntry> FlowOutputs;
        public List<SerializableStackInputEntry> StackInputs;
        public List<SerializableStackOutputEntry> StackOutputs;

    }

    [Serializable]
    class SerializableNodePropertyEntry
    {
        public string Name;
        public VariableType Type;
        public string Value;
    }

    [Serializable]
    class SerializableFlowOutputEntry
    {
        public string Name; //TODO: should this be serialized?
        public SerializableGuid Target;
    }

    [Serializable]
    class SerializableStackInputEntry
    {
        public string Name;
        public VariableType Type;
        public string Constant;
        public SerializableGuid ReferenceTarget;
        public int ReferenceIndex;
    }

    [Serializable]
    class SerializableStackOutputEntry
    {
        public string Name;
        public VariableType Type;
    }

    interface ISerializationContext
    {
        /// <summary>
        /// Outputs an entry for a node.
        /// </summary>
        void OnWriteNodeEntry(SerializableGuid id, SerializableNodeEntry entry);

        /// <summary>
        /// Outputs an entry for a graph variable.
        /// </summary>
        void OnWriteVariableEntry(SerializableGuid id, SerializableVariableEntry entry);

        /// <summary>
        /// Declares that a Unity Object is being referenced.
        /// </summary>
        SerializableGuid ReferenceUnityObject(UnityEngine.Object value);
    }

    interface IDeserializationContext
    {
        int NodeCount { get; }
        void GetNodeEntry(int index, out SerializableGuid id, out SerializableNodeEntry entry);

        int VariableCount { get; }
        void GetVariableEntry(int index, out SerializableGuid id, out SerializableVariableEntry entry);

        UnityEngine.Object GetUnityObject(SerializableGuid id);
    }

    static class SerializationUtils
    {
        /// <summary>
        /// Builds a string to uniquely identify a type in LogicGraph serialized data.
        /// </summary>
        public static string GetSerializableTypeName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        /// <summary>
        /// When code is refactored, LogicGraph serialized data might become invalid.
        /// Type names must be upgraded to reflect the new code.
        /// 
        /// TODO: binding names should be upgradable as well!
        /// </summary>
        public static string UpgradeSerializableTypeName(string typeName)
        {
            // Nothing to upgrade just yet
            return typeName;
        }

        /// <summary>
        /// When code is refactored, LogicGraph serialized data might become invalid.
        /// Binding names must be upgraded to reflect the new code.
        /// 
        /// This runs after @UpgradeSerializableTypeName
        /// </summary>
        public static string UpgradeSerializableBindingName(Type baseType, string typeName)
        {
            // Nothing to upgrade just yet
            return typeName;
        }



        public static bool IsSerializableNode(Type type)
        {
            return SerializableNames.ContainsKey(type);
        }

        public static string GetSerializableNodeName(Type type)
        {
            return SerializableNames[type];
        }

        public static Type GetSerializedNodeType(string serializedName)
        {
            try
            {
                return SerializableNames.First(x => x.Value == serializedName).Key;
            }
            catch
            {
                throw new InvalidOperationException($"LogicGraph Failed to find archetype serialization info: '{serializedName}'");
            }
        }

        [ClearOnReload]
        static Dictionary<Type, string> _nodeSerializableNames;
        public static IReadOnlyDictionary<Type, string> SerializableNames
        {
            get
            {
                if (_nodeSerializableNames == null)
                    GatherSerializableNames();
                return _nodeSerializableNames;
            }
        }

        static void GatherSerializableNames()
        {
            Debug.Assert(_nodeSerializableNames == null);
            _nodeSerializableNames = new Dictionary<Type, string>();

            var types = AssemblyUtilities.GetAllTypesWithAttribute(typeof(NodeSerializationAttribute), includePrivateTypes: true);
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<NodeSerializationAttribute>();
                _nodeSerializableNames.Add(type, attr.SerializedName);
            }
        }
    }
}
