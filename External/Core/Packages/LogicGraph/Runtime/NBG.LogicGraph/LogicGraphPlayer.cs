using NBG.Core;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NBG.LogicGraph
{
    [DisallowMultipleComponent]
    [HelpURL(HelpURLs.LogicGraphPlayer)]
    public class LogicGraphPlayer : MonoBehaviour, IManagedBehaviour, ISerializationCallbackReceiver, ILogicGraphContainer, ILogicGraphContainerCallbacks, IOnFixedUpdate
    {
        #region Global configuration
        public static bool EnableFixedUpdateEvents = true;
        public static bool EnableUpdateEvents = true;
        
        public const bool EnableScopes =
#if NBG_LOGIC_GRAPH_DISABLE_SCOPES
            false;
#else
            true;
#endif

        public const bool EnableScopeIcons =
#if NBG_LOGIC_GRAPH_DISABLE_SCOPES && !NBG_LOGIC_GRAPH_ENABLE_SCOPE_ICONS
            false;
#else
            true;
#endif
        #endregion

        #region Serialization types
        [Serializable]
        internal struct EntryContainer
        {
            public SerializableGuid id;
            public SerializableNodeEntry entry;
        }

        [Serializable]
        internal struct VariableContainer
        {
            public SerializableGuid id;
            public SerializableVariableEntry entry;
        }

        [Serializable]
        internal struct UnityObjectReferenceContainer
        {
            public SerializableGuid id;
            public UnityEngine.Object obj;
        }
        #endregion

        #region Graph data
        [SerializeField] int _serializationVersion;
        [SerializeField] List<EntryContainer> _entries = new List<EntryContainer>();
        [SerializeField] List<VariableContainer> _variables = new List<VariableContainer>();
        [SerializeField] List<UnityObjectReferenceContainer> _unityObjectReferences = new List<UnityObjectReferenceContainer>();
        LogicGraph _graph;
        #endregion

        #region UI data
        [Serializable]
        internal struct NodeUIData
        {
            public Rect rect;
            public Color color;
        }

        [Serializable]
        internal class NodeUIDataContainer
        {
            public SerializableGuid nodeId;
            public NodeUIData data;
        }

        [SerializeField]
        internal List<NodeUIDataContainer> _nodeUIDatas = new List<NodeUIDataContainer>();
        #endregion

        void IManagedBehaviour.OnLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;
        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (_graph == null)
                return;
            _graph.OnFixedUpdate(Time.fixedDeltaTime, EnableFixedUpdateEvents);
        }

        void Update()
        {
            if (_graph == null)
                return;
            _graph.OnUpdate(Time.deltaTime, EnableUpdateEvents);
        }

        internal void Clear()
        {
            _graph = new LogicGraph(this); // TODO: correct?

            _entries.Clear();
            _variables.Clear();
            _unityObjectReferences.Clear();
            _nodeUIDatas.Clear();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Debug.Assert(_serializationVersion == LogicGraph.SerializationVersion);

            // Build context
            var ctx = new DeserializationContextUnity();
            ctx._entries = _entries;
            ctx._variables = _variables;
            ctx._unityObjectReferences = _unityObjectReferences;
            
            // Deserialize
            try
            {
                _graph = new LogicGraph(this); // TODO: correct?
                _graph.Deserialize(ctx);
            }
            catch (Exception e)
            {
                Debug.LogError($"LogicGraph deserialization error: {e.Message}\n{e.StackTrace}");
            }

            //_containersDirty = true;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorUtility.IsDirty(this))
                return;
            UnityEditor.EditorUtility.ClearDirty(this);
#endif

            _serializationVersion = LogicGraph.SerializationVersion;

            // Pre-process graph
            var ctx = new SerializationContextUnity();
            ctx.existingUnityObjectIds = _unityObjectReferences;

            // Serialize serializables
            ctx.entries = this._entries;
            ctx.variables = this._variables;
            this._entries.Clear();
            this._variables.Clear();
            _graph?.Serialize(ctx);

            // Serialize UnityEngine.Object references
            _unityObjectReferences.Clear();
            foreach (var pair in ctx.unityObjectIds)
            {
                var entry = new UnityObjectReferenceContainer()
                {
                    id = pair.Value,
                    obj = pair.Key
                };
                _unityObjectReferences.Add(entry);
            }

            // Validate that NodeUIs exist
            foreach (var pair in _entries)
            {
                var guid = pair.id;
                if (guid != SerializableGuid.empty) // Prefab instance override handling.
                {
                    var hasNodeUI = _nodeUIDatas.Any(x => x.nodeId == pair.id);
                    Debug.Assert(hasNodeUI);
                }
            }
        }

        void OnEnable()
        {
            _graph.OnEnable();
        }

        void OnDisable()
        {
            _graph.OnDisable();
        }

        void Start()
        {
            _graph.OnStart();
        }

        #region ILogicGraphContainer
        public ILogicGraph Graph
        {
            get
            {
                if (_graph == null)
                    _graph = new LogicGraph(this); // TODO: correct?
                return _graph;
            }
        }

        void ILogicGraphContainerCallbacks.OnNodeAdded(SerializableGuid guid, INode node)
        {
            var container = new NodeUIDataContainer();
            container.nodeId = guid;
            container.data = new NodeUIData();
            _nodeUIDatas.Add(container);
        }

        void ILogicGraphContainerCallbacks.OnNodeRemoved(SerializableGuid guid, INode node)
        {
            _nodeUIDatas.RemoveAll(x => x.nodeId == guid);
        }
        #endregion
    }
}
