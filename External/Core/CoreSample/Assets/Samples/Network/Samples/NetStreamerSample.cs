using NBG.Core;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Sample
{
    public class NetStreamerSample : MonoBehaviour, INetStreamer, INetBehavior
    {
        public bool IAmServer = true;
        public float floatValueToSync;

        const float MAX_FLOAT_VALUE = 100;
        const int BITS = 8;

        void Update()
        { 
            if (IAmServer)
            {
                floatValueToSync += Time.deltaTime;
            }
        }

        void INetStreamer.CollectState(IStreamWriter stream)
        {
            //it is best to quntize. That way it will be lest offten that we need to send data.
            //More bits - bigger precission
            var val = floatValueToSync.Quantize(MAX_FLOAT_VALUE, BITS);
            stream.Write(val, BITS);
        }

        void INetStreamer.ApplyState(IStreamReader state)
        {
            floatValueToSync = state.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
        }

        void INetStreamer.ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames)
        {
            var val0 = state0.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
            var val1 = state1.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
            floatValueToSync = Mathf.Lerp(val0, val1, mix);
        }

        void INetStreamer.CalculateDelta(IStreamReader state0, IStreamReader state1, IStreamWriter delta)
        {
            var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
            var q1 = state1.ReadInt32(BITS);
            if (q0 == q1) //if both values same - then nothing to send
            {
                delta.Write(false);//one bit to indicate that nothing needs to change
            }
            else
            {
                delta.Write(true);//indicate that there are some changes
                delta.Write(q1, BITS);//and write those changes
            }
        }

        void INetStreamer.AddDelta(IStreamReader state0, IStreamReader delta, IStreamWriter result)
        {
            var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
            if (!delta.ReadBool())
            {
                result.Write(q0, BITS); // not changed
            }
            else
            {
                //changed. We write absolute, because we don't really gain a lot by writing relative, with BITS=4
                //Make sure, if you increase bits to consider writing relative here
                //TODO: need example of how that could be done
                int q1 = delta.ReadInt32(BITS);
                result.Write(q1, BITS);
            }
        }


        //this is part is used just to update timer on server, not client
        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Client:
                    IAmServer = false;
                    break;
                case NetworkAuthority.Server:
                    IAmServer = true;
                    break;
            }
        }
    }
}
