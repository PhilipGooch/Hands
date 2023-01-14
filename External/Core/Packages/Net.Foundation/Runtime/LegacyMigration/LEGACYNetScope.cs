//#define DUMPNET //If enabled, this prints what messages are processed for netScopes

using NBG.Core.Streams;
using System;
using UnityEngine;


namespace NBG.Net.Legacy
{
    [Obsolete("Obsolete with system based networking")]
    public class LEGACYNetScope : MonoBehaviour
    {
        public IStreamWriter BeginEvent(uint breakEvent)
        {
            //throw new NotImplementedException();
            return null;
        }

        public void EndEvent()
        {
            //throw new NotImplementedException();
        }

        public uint RegisterEvent(Action<IStreamReader> spawnUmbrellaParticles)
        {
            //throw new NotImplementedException();
            return 0;
        }
    }
}