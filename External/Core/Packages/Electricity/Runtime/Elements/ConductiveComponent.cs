using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Electricity
{
    /// <summary>
    /// ConductiveComponent are objects with specific colliders that transmit power on contact with other conductive elements
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ConductiveComponent : ElectricityComponent
    {
        public List<Collider> conductiveColliders;
        public List<MeshRenderer> electrifiableMeshes;
        [SerializeField] private readonly float contactOffset = 0.01f;

        protected override void Start()
        {
            base.Start();
            foreach (var collider in conductiveColliders)
            {
                collider.contactOffset = contactOffset;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (conductiveColliders == null || conductiveColliders.Count == 0)
                return;
            var conductiveObject = collision.body?.GetComponent<ConductiveComponent>();
            if (conductiveObject == null)
                return;
            if (conductiveObject.conductiveColliders.Contains(collision.collider))
            {
                //Debug.Log("decollided " + collision.collider.gameObject.name + " with  " + this.gameObject.name, collision.collider.gameObject);
                CircuitManager.Disconnect(this, conductiveObject);
            }
        }

        private readonly ContactPoint[] contacts = new ContactPoint[3];
        ConductiveComponent conductiveObject = null;
        private void OnCollisionStay(Collision collision)
        {
            if (conductiveColliders == null || conductiveColliders.Count == 0)
                return;
            collision.GetContacts(contacts);
            foreach (var contact in contacts)
            {
                if (conductiveColliders.Contains(contact.thisCollider))
                {
                    if (collision.body == null)
                        return;
                    conductiveObject = null;
                    bool isConductive = collision.body.TryGetComponent<ConductiveComponent>(out conductiveObject); //always use tryget here to avoid create garbage on Editor
                    if (!isConductive)
                        return;
                    if (conductiveObject.conductiveColliders.Contains(contact.otherCollider))
                    {
                        if (!jointedElements.Contains(conductiveObject))
                        {
                            CircuitManager.Connect(this, conductiveObject);
                            if (assignedCircuit.providers.Count > 0 && assignedCircuit.GetOutput() > 0)
                            {
                                Electricity.TriggerComponentContact(collision.GetContact(0).point, collision.GetContact(0).normal);
                            }
                        }
                    }
                }
            }
        }
    }
}
