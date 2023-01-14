using Recoil;
using Recoil.Util;
using Unity.Mathematics;

namespace Noodles
{
    public class CrateCarryable : MetaCrateCarryable
    {
        // crate is the only carryable that has two handed carry without any grips
        protected override bool AllowTwoHanded(in HandCarryData first, in HandCarryData second) => true;
    }
}
