using System;

namespace NBG.Net
{
    public enum NetPrecisionPos
    {
        /// <summary>
        /// Position information will not be send to clients
        /// Use this for objects that only rotate but never translate (ferris wheel for example)
        /// </summary>
        None,
        /// <summary>
        /// Position information will be send as a delta to its initial spawn position at the start of the level
        /// This is the best pick for almost all Level Objects
        /// </summary>
        RelativeToSpawnPos,
        /// <summary>
        /// Position information will be expressed as an Delta to another body.
        /// This is used for extra compression for objects that always move as a group (i.e. player, Lunar Lander, etc)
        /// </summary>
        RelativeToRigidBody,

        WorldSpace,
    }

    public enum NetPrecisionRot
    {
        /// <summary>
        /// No rotation information will be send to clients.
        /// Use this for objects that never rotate or where it does not matter (untextured spheres) 
        /// </summary>
        None,
        /// <summary>
        /// Rotation information will be relative to initial spawn position at the start of the level
        /// This is a good value for all objects you can find in the level
        /// </summary>
        RelativeToSpawnPos,
        /// <summary>
        /// Rotation will be only transmitted for rotations around the (world space) X-Axis.
        /// Use this for objects that only rotate around the X-Axis, i.e. ferris wheel
        /// </summary>
        EulerX,
        /// <summary>
        /// Rotation will be only transmitted for rotations around the (world space) Y-Axis.
        /// Use this for objects that only rotate around the Y-Axis
        /// </summary>
        EulerY,
        /// <summary>
        /// Rotation will be only transmitted for rotations around the (world space) Z-Axis.
        /// Use this for objects that only rotate around the Z-Axis, i.e. old moonbase space station
        /// </summary>
        EulerZ,
        /// <summary>
        /// Rotation information will be relative to another rigid body
        /// Use this if you have a group of objects that always rotate together. Declare one of them absolute and all other ones relative to this one
        /// </summary>
        RelativeTo,
    }

    [Serializable]
    public struct NetPrecisionSettings
    {
        //TODO: Measure and Tweak
        //TODO: Per Level makes sense, as range needs to be larger for some and ranges need to be tweaked around pos
        public static readonly NetPrecisionSettings DEFAULT = new NetPrecisionSettings(
           syncPosition: NetPrecisionPos.RelativeToSpawnPos,
           syncRotation: NetPrecisionRot.RelativeToSpawnPos,
           posRange: 500,
           possmall: 5,
           poslarge: 9,
           posfull: 18,
           rotsmall: 5,
           rotlarge: 9,
           rotfull: 12);

        public NetPrecisionPos syncPosition;
        public NetPrecisionRot syncRotation;
        public float posRange;
        public ushort possmall;
        public ushort poslarge;
        public ushort posfull;
        public ushort rotsmall;
        public ushort rotlarge;
        public ushort rotfull;

        public NetPrecisionSettings(NetPrecisionPos syncPosition, NetPrecisionRot syncRotation, float posRange, ushort possmall, ushort poslarge, ushort posfull, ushort rotsmall, ushort rotlarge, ushort rotfull)
        {
            this.syncPosition = syncPosition;
            this.syncRotation = syncRotation;
            this.posRange = posRange;
            this.possmall = possmall;
            this.poslarge = poslarge;
            this.posfull = posfull;
            this.rotsmall = rotsmall;
            this.rotlarge = rotlarge;
            this.rotfull = rotfull;
        }
    }
}
