using NBG.Audio;

namespace SampleProject
{
    [SurfaceTypeEnum((int)AudioSurfaceType.Unknown, (int)AudioSurfaceType.Stone)]
    public enum AudioSurfaceType
    {
        Unknown = 0,
        Stone = 1,
        Wood = 2,
        MetalSolid = 3,
        Player = 4,
        PlayerFeet = 5,
        GlassShard = 6,
        Suitcase = 7,
        Grass = 8,
        WoodHard = 9,
        Dirt = 10,
        Concrete = 11,
        MetalStrong = 12,
        Sand = 13,
        Gong = 14,
        Taiko = 15,
        StoneBig = 16,
        Nail = 17
    }
}
