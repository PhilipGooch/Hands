using System.Reflection;
using UnityEngine.Assertions;

namespace NBG.LogicGraph
{
    internal class UserlandEventBinding : UserlandBinding
    {
        string _description;
        NodeAPIScope _scope;

        public override string Description => _description;
        public NodeAPIScope Scope => _scope;

        public EventInfo Source { get; }

        public string HandlerName { get; }
        // Currently is globally unique (assembly name hash + index in assembly)
        public long EventId { get; }

        public MethodInfo AddMethod { get; }
        public MethodInfo RemoveMethod { get; }

#if UNITY_EDITOR
        string _sourceFile;
        int _sourceLine;
        public override string SourceFile => _sourceFile;
        public override int SourceLine => _sourceLine;
#endif

        public override string ToString()
        {
            var ret = $"{Name} ({EventId})";
            if (string.IsNullOrWhiteSpace(_description))
            {
                ret = $"{ret} \"{_description}\"";
            }
            ret = $"{ret} on {Source.DeclaringType.FullName}";
            return ret;
        }

        public UserlandEventBinding(EventInfo ei)
        {
            AddMethod = ei.GetAddMethod(true);
            if (AddMethod == null)
                throw new System.InvalidOperationException($"Add() method missing on {ei.DeclaringType.Name}.{ei.Name}");

            RemoveMethod = ei.GetRemoveMethod(true);
            if (RemoveMethod == null)
                throw new System.InvalidOperationException($"Remove() method missing on {ei.DeclaringType.Name}.{ei.Name}");

            this.Type = UserlandBindingType.UBT_Event;
            this.TargetType = ei.DeclaringType;
            this.DeclaringType = ei.DeclaringType;
            this.Name = ei.Name;
            this.IsStatic = AddMethod.IsStatic;
            this.Source = ei;

            var nodeAPI = ei.GetCustomAttribute<NodeAPIAttribute>();
            Assert.IsNotNull(nodeAPI);
            _description = nodeAPI.Description;
            if (nodeAPI.Scope == NodeAPIScope.Generic)
            {
                UnityEngine.Debug.LogWarning($"{ei.DeclaringType.Name}.{ei.Name} can't be in generic scope. Defaulting to {NodeAPIScope.Sim}");
                _scope = NodeAPIScope.Sim;
            }
            else
            {
                _scope = nodeAPI.Scope;
            }
#if UNITY_EDITOR
            _sourceFile = nodeAPI.SourceFile;
            _sourceLine = nodeAPI.SourceLine;
#endif

            HandlerName = $"{UserlandBindings.Prefix}HANDLE_{ei.Name}";

            // Extract the id
            var funcName = $"{UserlandBindings.Prefix}GET_EVENTID_{ei.Name}";
            var delGID = this.DeclaringType.GetMethod(funcName, UserlandBindings.GenBindingFlags);
            //var getEventIdFunc = (GetEventIdDelegate)delGID.CreateDelegate(typeof(GetEventIdDelegate));
            EventId = (long)delGID.Invoke(null, null);
        }
    }
}
