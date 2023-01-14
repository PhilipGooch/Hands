using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Impale
{
    /// <summary>
    /// Allows aligning impaler with the set normal at the start of impaling. I.e. arrows can always stick out of the wall at a 90 degrees angle.
    /// </summary>
    public interface IImpalerOrientationOverride
    {
        public Vector3 Normal { get; }
    }
}
