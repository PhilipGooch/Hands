using System;
using System.Collections.Generic;
using NBG.Entities;
using NBG.Impale;
using Noodles;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class MilkshakeImpaler : Impaler, Noodles.IGrabbable
    {
        [SerializeField]
        private bool impaleOnlyIfGrabbed;
        [SerializeField]
        private bool allowPullOutIfGrabbed;

        [SerializeField] private bool ignorePlayers;
        // [SerializeField] private CustomTag[] ignoreTags;

        protected int GrabCount { get; private set; }

        public Func<Collider, RaycastHit, bool, bool> Filter = null;

        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();
            onJointLocked += OnJointLocked;
            onJointCreated += OnJointCreated;
            onImpalerRemoved += OnJointDestroyed;
            onUnimpaled += OnUnimpaled;
        }

        protected void OnJointCreated(ConfigurableJoint joint, Collider target, Vector3 impalePos)
        {

        }

        protected void OnJointDestroyed(ConfigurableJoint joint, Collider target)
        {

        }

        protected void OnUnimpaled()
        {

        }

        protected void OnJointLocked()
        {

        }

        void Noodles.IGrabbable.OnGrab(Entity noodle, NoodleHand hand)
        {
            GrabCount++;
        }

        void Noodles.IGrabbable.OnRelease(Entity noodle, NoodleHand hand)
        {
            GrabCount--;
        }

        protected override bool ShouldPreventThisAndFurtherImpales(Collider other, RaycastHit hit)
        {
            //NOTE: Tags prevent further penetration
            /*if (ignoreTags != null)
            {
                foreach (var ignoreTag in ignoreTags)
                {
                    //Filter tags on GO
                    if (other.gameObject.TryGetComponent(out CustomTags tags))
                        if (tags.HasTag(ignoreTag))
                        {
                            Debug.Log("Ignored due to tag");
                            return true;
                        }


                    //Filter tags on RB
                    if (other.attachedRigidbody != null)
                        if (other.attachedRigidbody.TryGetComponent(out CustomTags tags2))
                            if (tags2.HasTag(ignoreTag))
                            {
                                Debug.Log("Ignored due to tag");
                                return true;
                            }

                }
            }*/


            return base.ShouldPreventThisAndFurtherImpales(other, hit);
        }

        protected override bool CanImpaleNewObject(Collider other, RaycastHit hit, bool stationaryImpale = false)
        {
            /*//Ignore Player layers
            if (ignorePlayers)
            {
                if (((1 << other.gameObject.layer) & (int)H2Layers.Player) > 0 || ((1 << other.gameObject.layer) & (int)H2Layers.Ball) > 0)
                {
                    Debug.Log("Ignored due to player");
                    return false;
                }

            }*/

            //Grab count rules
            if (GrabCount > 0 || impaleOnlyIfGrabbed)
            {
                Debug.Log("Ignored due to grab count");
                return false;
            }


            //Impale system checks
            if (!base.CanImpaleNewObject(other, hit, stationaryImpale))
            {
                return false;
            }


            //Check if a filter prevents impale
            if (Filter != null && Filter.Invoke(other, hit, stationaryImpale) == false)
            {
                Debug.Log("Filter failed");
                return false;
            }


            return true;
        }

        /* Milkshake currently does not need to override this.
         This allows you to filter joint creation for various things
        protected override bool ShouldAddJoint(Collider other, float depth)
        {
            return base.ShouldAddJoint(other, depth);
        }
        */

        protected override bool ShouldUnlockMovementAlongImpaleAxis()
        {
            return GrabCount > 0 && allowPullOutIfGrabbed;
        }
    }
}
