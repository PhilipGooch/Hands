using System.Collections.Generic;
using UnityEngine;

namespace WaterPipeSystem
{
    public class WaterPipeManager : SingletonBehaviour<WaterPipeManager>
    {
        private List<WaterPool> pools = new List<WaterPool>();
        private List<WaterPipe> pipes = new List<WaterPipe>();

        protected override void Awake()
        {
            base.Awake();
        }

        private void FixedUpdate()
        {
            foreach (WaterPool pool in pools)
            {
                foreach (var socket in pool.Sockets)
                {
                    float waterPressure = CalculateWaterPressure(socket, pool);
                    SetWaterPressure(socket, waterPressure);
                    float waterPressureAtOtherEndOfPipeChain = GetWaterPressureAtOtherEndOfPipeChain(socket);
                    float tightestValveRestrictionInChain = FindTightestWaterValveRestrictionInChain(socket);
                    float waterFlow = CalculateWaterFlow(waterPressure, waterPressureAtOtherEndOfPipeChain, tightestValveRestrictionInChain);
                    SetWaterFlow(socket, waterFlow);
                    int waterFlowDirection = CalculateWaterFlowDirection(waterPressure, waterPressureAtOtherEndOfPipeChain);
                    SetWaterFlowDirection(socket, waterFlowDirection);
                }
            }
        }

        private void SetWaterFlowDirectionAtOtherEndOfPipeChain(WaterSocket socket, int direction)
        {
            WaterSocket otherSocket = socket.Pipe.GetOtherSocket(socket);
            if (LockedConnection(otherSocket))
            {
                Debug.Assert(otherSocket.TargetSocket);
                SetWaterFlowDirectionAtOtherEndOfPipeChain((WaterSocket)otherSocket.TargetSocket, direction);
            }
            else
            {
                otherSocket.WaterFlowDirection = direction;
            }
        }

        private void SetWaterPressure(WaterSocket socket, float pressure)
        {
            socket.WaterPressure = pressure;
        }

        private void SetWaterFlowDirection(WaterSocket socket, int direction)
        {
            socket.WaterFlowDirection = direction;
            SetWaterFlowDirectionAtOtherEndOfPipeChain(socket, -direction);
        }

        private float GetWaterPressureAtOtherEndOfPipeChain(WaterSocket socket)
        {
            WaterSocket otherSocket = socket.Pipe.GetOtherSocket(socket);
            if (LockedConnection(otherSocket))
            {
                Debug.Assert(otherSocket.TargetSocket);
                return GetWaterPressureAtOtherEndOfPipeChain((WaterSocket)otherSocket.TargetSocket);
            }
            return otherSocket.WaterPressure;
        }

        private float CalculateWaterPressure(WaterSocket socket, WaterPool pool)
        {
            float waterLevel = pool.HeightAtBottomOfPool + pool.Depth;
            float heightAtBottomOfSocket = socket.transform.position.y - socket.Pipe.Radius;
            float depthOfWaterInPipeToOvercomeResistence = (socket.Pipe.Radius * 2.0f) * socket.Pipe.Resistence;
            float waterPressure = Mathf.Max(0.0f, SheepMathUtils.Map(waterLevel, heightAtBottomOfSocket, heightAtBottomOfSocket + depthOfWaterInPipeToOvercomeResistence, 0.0f, 1.0f));
            return waterPressure;
        }

        private float CalculateWaterFlow(float waterPressure, float waterPressureAtOtherEndOfPipeChain, float tightestValveRestrictionInChain)
        {
            float waterFlow = Mathf.Clamp01(Mathf.Abs(waterPressure - waterPressureAtOtherEndOfPipeChain));
            waterFlow = Mathf.Min(waterFlow, 1.0f - tightestValveRestrictionInChain);
            return waterFlow > 0.999f ? 1.0f : waterFlow;
        }

        private int CalculateWaterFlowDirection(float waterPressure, float waterPressureAtOtherEndOfPipeChain)
        {
            return waterPressure > waterPressureAtOtherEndOfPipeChain ? -1 : 1;
        }

        private void SetWaterFlow(WaterSocket socket, float waterFlow)
        {
            socket.Pipe.WaterFlow = waterFlow;
            WaterSocket otherSocket = socket.Pipe.GetOtherSocket(socket);
            if (LockedConnection(otherSocket))
            {
                Debug.Assert(otherSocket.TargetSocket);
                SetWaterFlow((WaterSocket)otherSocket.TargetSocket, waterFlow);
            }
        }

        private float FindTightestWaterValveRestrictionInChain(WaterSocket socket, float restriction = 0.0f)
        {
            restriction = Mathf.Max(restriction, socket.Pipe.Restriction);
            WaterSocket otherSocket = socket.Pipe.GetOtherSocket(socket);
            if (LockedConnection(otherSocket))
            {
                Debug.Assert(otherSocket.TargetSocket);
                return FindTightestWaterValveRestrictionInChain((WaterSocket)otherSocket.TargetSocket, restriction);
            }
            return restriction;
        }

        public void ResetWaterFlow(Block block, Hand hand)
        {
            foreach (WaterPipe pipe in pipes)
            {
                pipe.WaterFlow = 0.0f;
            }
        }

        public void AddPool(WaterPool pool)
        {
            if (!pools.Contains(pool))
            {
                pools.Add(pool);
            }
        }

        public void RemovePool(WaterPool pool)
        {
            pools.Remove(pool);
        }

        public void AddPipe(WaterPipe pipe)
        {
            if (!pipes.Contains(pipe))
            {
                pipes.Add(pipe);
            }
            pipe.onGrab += ResetWaterFlow;
        }

        public void RemovePipe(WaterPipe pipe)
        {
            pipes.Remove(pipe);
            pipe.onGrab -= ResetWaterFlow;
        }

        private bool LockedConnection(WaterSocket socket)
        {
            return socket.Connection != null && socket.Connection.Locked;
        }
    }
}
