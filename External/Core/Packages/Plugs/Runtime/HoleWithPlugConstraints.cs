using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Plugs
{
    public class HoleWithPlugConstraints : Hole
    {
        [SerializeField]
        List<Plug> allowedPlugs = new List<Plug>();

        public override bool CanConnect(Plug plug)
        {
            var canChangeConnection = base.CanConnect(plug);

            return canChangeConnection && allowedPlugs.Contains(plug);
        }
    }
}
