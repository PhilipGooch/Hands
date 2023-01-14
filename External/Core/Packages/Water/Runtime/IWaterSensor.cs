using System.Collections.Generic;

namespace NBG.Water
{
    public interface IWaterSensor
    {
        bool Submerged { get; }
        
        IReadOnlyList<BodyOfWater> BodiesOfWater { get; }
    }
}
