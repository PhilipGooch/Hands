using NBG.Core;
using System.Collections.Generic;

namespace NBG.Electricity
{
    /// <summary>
    /// This handles the groups of connections made in a level for all the electric components,
    /// which then are used to handle power distribution in Electricity.cs.
    /// </summary>
    public class CircuitManager
    {
        [ClearOnReload] private static CircuitManager instance;
        internal static CircuitManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CircuitManager();

                return instance;
            }
        }

        public class ElectricityCircuit
        {
            internal List<ElectricityComponent> components = new List<ElectricityComponent>();
            internal List<IProvider> providers = new List<IProvider>();
            internal List<IReceiver> consumers = new List<IReceiver>();
            public float GetOutput()
            {
                float power = 0;
                foreach (var p in providers)
                {
                    power += p.Output;
                }
                return power;
            }
        }

        private readonly List<ElectricityCircuit> circuits = new List<ElectricityCircuit>();
        public static void Connect(ElectricityComponent comp1, ElectricityComponent comp2, bool force = false)
        {
            Instance.ConnectionMade(comp1, comp2, force);
        }

        public static bool IsConnected(ElectricityComponent comp1, ElectricityComponent comp2)
        {
            return comp1.jointedElements.Contains(comp2);
        }
        private void ConnectionMade(ElectricityComponent comp1, ElectricityComponent comp2, bool force = false)
        {
            if (comp1.jointedElements.Contains(comp2) && comp2.jointedElements.Contains(comp1) && !force)
                return;
            if (!comp1.jointedElements.Contains(comp2))
                comp1.jointedElements.Add(comp2);
            if (!comp2.jointedElements.Contains(comp1))
                comp2.jointedElements.Add(comp1);
            if (comp1.assignedCircuit == null && comp2.assignedCircuit == null)
            {
                //create a new circuit:
                ElectricityCircuit newCircuit = new ElectricityCircuit();
                AddComponentToCircuit(comp1, newCircuit);
                AddComponentToCircuit(comp2, newCircuit);
                circuits.Add(newCircuit);
                ConnectCircuit(newCircuit);
                Electricity.RebuildLogic();
                CheckElectrified(newCircuit);
            }
            else if (comp1.assignedCircuit == null && comp2.assignedCircuit != null)
            {
                AddComponentToCircuit(comp1, comp2.assignedCircuit);
                ConnectCircuit(comp2.assignedCircuit);
                Electricity.RebuildLogic();
                CheckElectrified(comp2.assignedCircuit);
            }
            else if (comp1.assignedCircuit != null && comp2.assignedCircuit == null)
            {
                AddComponentToCircuit(comp2, comp1.assignedCircuit);
                ConnectCircuit(comp1.assignedCircuit);
                Electricity.RebuildLogic();
                CheckElectrified(comp1.assignedCircuit);
            }
            else if (comp1.assignedCircuit != comp2.assignedCircuit)
            {
                //merge both circuits
                var oldCircuit = comp2.assignedCircuit;
                foreach (var comp in oldCircuit.components)
                {
                    AddComponentToCircuit(comp, comp1.assignedCircuit);
                }
                circuits.Remove(oldCircuit);
                ConnectCircuit(comp1.assignedCircuit);
                Electricity.RebuildLogic();
                CheckElectrified(comp1.assignedCircuit);
            }
        }
        private void ConnectCircuit(ElectricityCircuit circuit)
        {
            foreach (var item in circuit.components)
            {
                if (item is IProvider provider)
                {
                    foreach (var item2 in circuit.components)
                    {
                        if (item2 is IReceiver reciever)
                            Electricity.Connect(provider, reciever);
                    }
                }
            }
        }
        public static void Disconnect(ElectricityComponent comp1, ElectricityComponent comp2)
        {
            Instance.ConnectionBroken(comp1, comp2);
        }
        private void ConnectionBroken(ElectricityComponent comp1, ElectricityComponent comp2)
        {
            comp1.RemoveJoint(comp2);
            comp2.RemoveJoint(comp1);
            var elements1 = comp1.GetJointsRecursively(null);
            if (!elements1.Contains(comp2))
            {
                Electricity.Disconnect(comp1.assignedCircuit.providers, comp1.assignedCircuit.consumers);

                circuits.Remove(comp1.assignedCircuit);

                ElectricityCircuit newCircuit1 = new ElectricityCircuit();
                for (int i = 0; i < elements1.Count; i++)
                    AddComponentToCircuit(elements1[i], newCircuit1);

                var elements2 = comp2.GetJointsRecursively(null);
                ElectricityCircuit newCircuit2 = new ElectricityCircuit();
                for (int i = 0; i < elements2.Count; i++)
                    AddComponentToCircuit(elements2[i], newCircuit2);

                ConnectCircuit(newCircuit1);
                ConnectCircuit(newCircuit2);
                Electricity.RebuildLogic();
                CheckElectrified(newCircuit1);
                CheckElectrified(newCircuit2);
            }
        }

        private static void AddComponentToCircuit(ElectricityComponent newComponent, ElectricityCircuit circuit)
        {
            circuit.components.Add(newComponent);
            newComponent.assignedCircuit = circuit;

            if (newComponent is IProvider provider)
                circuit.providers.Add(provider);
            else if (newComponent is IReceiver reciever)
                circuit.consumers.Add(reciever);
        }
        private void CheckElectrified(ElectricityCircuit circuit)
        {
            float output = 0;
            foreach (var provider in circuit.providers)
            {
                output += provider.Output;
            }
            for (int i = 0; i < circuit.components.Count; i++)
            {
                if (circuit.components[i] is ConductiveComponent conductive)
                {
                    foreach (var mesh in conductive.electrifiableMeshes)
                    {
                        mesh.material.SetFloat("_Electricity", output == 0 ? 0 : 1);
                        if (output == 0)
                            mesh.material.DisableKeyword("_ELECTRIC");
                        else
                            mesh.material.EnableKeyword("_ELECTRIC");
                    }
                }
            }
        }
    }
}
