using NBG.Core;
using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace NBG.Electricity
{
    /// <summary>
    /// This handles the power distribution between all the inputs and outputs of the level
    /// </summary>
    public class Electricity
    {
        [ClearOnReload] private static Electricity instance;

        private readonly Dictionary<IProvider, List<IReceiver>> outputs;
        private readonly Dictionary<IElectricityNode, List<IElectricityNode>> dependencies;

        private readonly List<IReceiver> receivers;
        private readonly List<IProvider> providers;
        private readonly List<IElectricityNode> all;

        [ClearOnReload] [NodeAPI("OnConductiveComponentContact", NodeAPIScope.View)] public static event Action<Vector3, Vector3> onConductiveComponentContact;

        internal static Electricity Instance
        {
            get
            {
                if (instance == null)
                    instance = new Electricity();

                return instance;
            }
        }

        private readonly List<IElectricityNode> execution;
        internal Electricity()
        {
            outputs = new Dictionary<IProvider, List<IReceiver>>();

            receivers = new List<IReceiver>();
            providers = new List<IProvider>();
            all = new List<IElectricityNode>();
            dependencies = new Dictionary<IElectricityNode, List<IElectricityNode>>();
            execution = new List<IElectricityNode>();
        }

        internal void Register(IReceiver receiver)
        {
            var node = (IElectricityNode)receiver;
            all.Add(node);
            dependencies.Add(node, new List<IElectricityNode>());
            receivers.Add(receiver);
        }

        internal void Register(IProvider provider)
        {
            var node = (IElectricityNode)provider;
            all.Add(node);
            outputs.Add(provider, new List<IReceiver>());
            dependencies.Add(node, new List<IElectricityNode>());
            providers.Add(provider);
        }
        private void AddConnection(IProvider provider, IReceiver receiver)
        {
            AddDependency((IElectricityNode)receiver, (IElectricityNode)provider);
            var outs = outputs[provider];
            if (!outs.Contains(receiver))
                outs.Add(receiver);
        }

        private void AddDependency(IElectricityNode node, IElectricityNode dependency)
        {
            var nodeDependencies = dependencies[node];
            if (!nodeDependencies.Contains(dependency))
                nodeDependencies.Add(dependency);
        }

        internal static void Disconnect(List<IProvider> connectionProviders, List<IReceiver> connectionReceivers)
        {
            int connectionProviderCount = connectionProviders.Count;
            int connectionReceiversCount = connectionReceivers.Count;

            for (int i = 0; i < connectionProviderCount; i++)
            {
                var providerOutput = Instance.outputs[connectionProviders[i]];

                for (int j = 0; j < connectionReceiversCount; j++)
                {
                    providerOutput.Remove(connectionReceivers[j]);
                }
            }

            for (int i = 0; i < connectionReceiversCount; i++)
            {
                var dependencies = Instance.dependencies[(IElectricityNode)connectionReceivers[i]];

                for (int j = 0; j < connectionProviderCount; j++)
                {
                    dependencies.Remove((IElectricityNode)connectionProviders[j]);
                }
            }
        }

        internal static void ClearConnections()
        {
        }

        internal static void Connect(IProvider provider, IReceiver receiver)
        {
            Instance.AddConnection(provider, receiver);
        }
        internal static void CreateDependency(IElectricityNode node, IElectricityNode dependency)
        {
            Instance.AddDependency(node, dependency);
        }
        public static void Simulate()
        {
            Instance.SimulateOnce();
        }

        internal static bool IsConnected(IProvider provider)
        {
            return instance.outputs[provider].Count > 0;
        }
        internal static bool IsConnected(IReceiver receiver)
        {
            return instance.dependencies[(IElectricityNode)receiver].Count > 0;
        }
        private void SimulateOnce()
        {
            CleanReceiversInput();

            for (int i = 0; i < execution.Count; i++)
            {
                var node = execution[i];

                node.Tick();

                if (node is IProvider provider)
                    Provide(provider);
            }
        }

        private void CleanReceiversInput()
        {
            foreach (var receiver in receivers)
                receiver.Input = 0;
        }
        private void Provide(IProvider provider)
        {
            var outs = outputs[provider];
            int outsCount = outs.Count;
            float ampsPerOutput = provider.Output / outsCount;

            for (int i = 0; i < outsCount; i++)
            {
                outs[i].Input += ampsPerOutput;
            }
        }
        private void CompileExecution()
        {
            execution.Clear();

            int providerCount = providers.Count;
            int receiverCount = receivers.Count;

            for (int i = 0; i < providerCount; i++)
            {
                IElectricityNode provider = (IElectricityNode)providers[i];
                AddToExecution(provider, provider);
            }

            for (int i = 0; i < receiverCount; i++)
            {
                IElectricityNode receiver = (IElectricityNode)receivers[i];
                AddToExecution(receiver, receiver);
            }
        }

        internal static void RebuildLogic()
        {
            Instance.CompileExecution();
        }

        internal static void TriggerComponentContact(float3 point, float3 normal)
        {
            onConductiveComponentContact(point, normal);
        }

        private bool AddToExecution(IElectricityNode node, IElectricityNode start)
        {
            if (!execution.Contains(node))
            {
                var nodeDependencies = dependencies[node];
                int nodeDependencyCount = nodeDependencies.Count;
                for (int i = 0; i < nodeDependencyCount; i++)
                {
                    var dependency = nodeDependencies[i];
                    if (dependency == start || !AddToExecution(dependency, start))
                        return false;
                }
                execution.Add(node);
            }
            return true;
        }

        internal static void CleanInstance()
        {
            instance = null;
        }
    }
}
