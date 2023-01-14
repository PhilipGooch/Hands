using System.Collections.Generic;
using UnityEngine;

namespace NBG.Electricity
{
    /// <summary>
    /// Basic class for all electric components.
    /// </summary>
    public class ElectricityComponent : MonoBehaviour
    {
        public CircuitManager.ElectricityCircuit assignedCircuit;
        [SerializeField] internal List<ElectricityComponent> jointedElements = new List<ElectricityComponent>();

        protected virtual void Start()
        {
            foreach (var element in jointedElements)
            {
                if (element != null)
                    CircuitManager.Connect(this, element, true);
            }
        }

        internal void AddJoint(ElectricityComponent newJoint)
        {
            jointedElements.Add(newJoint);
        }

        internal void RemoveJoint(ElectricityComponent newJoint)
        {
            jointedElements.Remove(newJoint);
        }

        internal List<ElectricityComponent> GetJointsRecursively(List<ElectricityComponent> foundElements)
        {
            if (foundElements == null)
                foundElements = new List<ElectricityComponent>();

            if (!foundElements.Contains(this))
                foundElements.Add(this);

            foreach (var j in jointedElements)
            {
                if (!foundElements.Contains(j))
                    foundElements = j.GetJointsRecursively(foundElements);
            }

            return foundElements;
        }
    }
}
