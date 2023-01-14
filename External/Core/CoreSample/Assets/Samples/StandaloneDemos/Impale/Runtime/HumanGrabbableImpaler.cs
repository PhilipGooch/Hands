using NBG.Entities;
using NBG.Impale;
using NBG.XPBDRope;
using Noodles;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class HumanGrabbableImpaler : Impaler, Noodles.IGrabbable
    {
        [SerializeField]
        private bool impaleOnlyIfGrabbed;
        [SerializeField]
        private bool allowPullOutIfGrabbed;

        private bool isGrabbed = false;

        public override void OnLevelLoaded()
        {
            base.OnLevelLoaded();
            onJointLocked += OnJointLocked;
            onJointCreated += OnJointCreated;
            onImpalerRemoved += OnJointDestroyed;
            onUnimpaled += OnUnimpaled;
        }

        private void OnJointCreated(ConfigurableJoint joint, Collider target, Vector3 impalePos)
        {
            // Debug.Log("OnJointCreated");
        }

        private void OnJointDestroyed(ConfigurableJoint joint, Collider target)
        {
            // Debug.Log("OnJointDestroyed");
        }

        private void OnUnimpaled()
        {
            // Debug.Log("OnUnimpaled");
        }


        private void OnJointLocked()
        {
            // Debug.Log("OnJointLocked");
        }


        public void OnGrab(Entity noodle, NoodleHand hand)
        {
            isGrabbed = true;
        }

        public void OnRelease(Entity noodle, NoodleHand hand)
        {
            isGrabbed = false;
        }

        protected override bool ShouldPreventThisAndFurtherImpales(Collider other, RaycastHit hit)
        {
            if (other.GetComponent<BlockerObject>())
                return true;

            return base.ShouldPreventThisAndFurtherImpales(other, hit);
        }

        protected override bool CanImpaleNewObject(Collider other, RaycastHit hit, bool stationaryImpale = false)
        {
            if (isGrabbed || !impaleOnlyIfGrabbed)
            {
                if (other.GetComponent<BlockerObject>())
                    return false;

                return base.CanImpaleNewObject(other, hit, stationaryImpale);
            }
            else
                return false;
        }

        protected override bool ShouldAddJoint(Collider other, float depth)
        {
            var segmentA = other.GetComponentInParent<RopeSegment>();
            
            if (segmentA)
            {
                var ropeA = segmentA.GetComponentInParent<Rope>();
                foreach (var item in ImpaledObjects)
                {
                    var segmentB = item.Value.Collider.GetComponentInParent<RopeSegment>();
                    if (segmentB != null)
                    {
                        var ropeB = segmentB.GetComponentInParent<Rope>();
                        if (ropeA == ropeB && item.Value.Joint != null)
                            return false;
                    }
                }
            }

            return base.ShouldAddJoint(other, depth);
        }

        protected override bool ShouldUnlockMovementAlongImpaleAxis()
        {
            return isGrabbed && allowPullOutIfGrabbed;
        }
    }
}
