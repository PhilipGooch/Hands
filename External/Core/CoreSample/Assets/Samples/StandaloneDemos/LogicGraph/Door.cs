using NBG.LogicGraph;
using System;
using UnityEngine;

namespace Sample.LogicGraph
{
    public class Door : MonoBehaviour
    {
        public bool open;

        [NodeAPI("Event with arg", scope: NodeAPIScope.Sim)]
        public event Action<bool> OnOpen;

        [NodeAPI("Open/close door")]
        public bool Open
        {
            get
            {
                return open;
            }

            set
            {
                open = value;
                OnOpen?.Invoke(value);
            }
        }

        [NodeAPI("Is the door open?")]
        public bool IsOpen()
        {
            return open;
        }

        [NodeAPI("Open door")]
        public void OpenFunc1()
        {
            Open = true;
            OnOpen?.Invoke(true);
        }

        [NodeAPI("Open/close door")]
        public void OpenFunc2(bool value)
        {
            Open = value;
            OnOpen?.Invoke(value);
        }

        [NodeAPI("Open/close door (Sim only)", scope: NodeAPIScope.Sim)]
        public void OpenFunc2Sim(bool value)
        {
            Open = value;
            OnOpen?.Invoke(value);
        }

        [NodeAPI("Slam", flags: NodeAPIFlags.ForceFlowNode)]
        public void SlamDoor(float initialPower, out float resultingPower)
        {
            resultingPower = initialPower;
        }

        private void FixedUpdate()
        {
            var rot = transform.localEulerAngles;
            if (open)
            {
                rot.y = Mathf.MoveTowards(rot.y, 90.0f, 3.0f);
            }
            else
            {
                rot.y = Mathf.MoveTowards(rot.y, 0.0f, 3.0f);
            }
            transform.localEulerAngles = rot;
        }
    }
}
