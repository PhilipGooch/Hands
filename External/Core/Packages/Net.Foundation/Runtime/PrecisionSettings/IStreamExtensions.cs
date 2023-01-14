using NBG.Core;
using NBG.Core.Streams;
using Unity.Mathematics;

namespace NBG.Net
{
    static class QConsts
    {
        internal const int rotBitSmal = 4;
        internal const int rotBitMed = 8;
        internal const int rotBitFull = 12;
        internal const int rotBitComp = 2; //Quaternion selected components (always 0-3)
        internal const int rotRange = 180; //Full circle; //TODO: This should be 2*pi and we should transmit radians, to avoid having to convert all the time
    }

    public static class IStreamExtensions
    {
        public static float3 ReadPos(this IStreamReader reader, in NetPrecisionSettings settings)
        {
            var value = reader.ReadPosQuantized(settings);
            return Quantization.Dequantize(value, settings.posRange, settings.posfull);
        }

        public static void WritePos(this IStreamWriter writer, float3 pos, in NetPrecisionSettings settings)
        {
            var posQuant = Quantization.Quantize(pos, settings.posRange, settings.posfull);
            writer.WritePosQuantized(posQuant, settings);
        }

        public static quaternion ReadRot(this IStreamReader reader, in NetPrecisionSettings settings)
        {
            return Quantization.Dequantize(new int4
            {
                w = (int)reader.ReadUInt32(QConsts.rotBitComp),
                x = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
                y = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
                z = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
            }, settings.rotfull);
        }

        public static void WriteRot(this IStreamWriter writer, in quaternion value, in NetPrecisionSettings settings)
        {
            var quat = value.Quantize(settings.rotfull);
            //NOTE: We write w first, because it contains which component we dropped and is always 2 bits long
            writer.Write((uint)quat.w, QConsts.rotBitComp);
            writer.Write(quat.x, settings.rotsmall, settings.rotlarge, settings.rotfull);
            writer.Write(quat.y, settings.rotsmall, settings.rotlarge, settings.rotfull);
            writer.Write(quat.z, settings.rotsmall, settings.rotlarge, settings.rotfull);
        }

        public static int3 ReadPosQuantized(this IStreamReader reader, in NetPrecisionSettings settings)
        {
            return new int3
            {
                x = reader.ReadInt32(settings.possmall, settings.poslarge, settings.posfull),
                y = reader.ReadInt32(settings.possmall, settings.poslarge, settings.posfull),
                z = reader.ReadInt32(settings.possmall, settings.poslarge, settings.posfull),
            };
        }

        public static void WritePosQuantized(this IStreamWriter writer, int3 value, in NetPrecisionSettings settings)
        {
            writer.Write(value.x, settings.possmall, settings.poslarge, settings.posfull);
            writer.Write(value.y, settings.possmall, settings.poslarge, settings.posfull);
            writer.Write(value.z, settings.possmall, settings.poslarge, settings.posfull);
        }

        public static int4 ReadRotQuantized(this IStreamReader reader, in NetPrecisionSettings settings)
        {
            return new int4
            {
                w = (int)reader.ReadUInt32(QConsts.rotBitComp),
                x = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
                y = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
                z = reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull),
            };
        }

        public static void WriteRotQuantized(this IStreamWriter writer, in int4 value, in NetPrecisionSettings settings)
        {
            writer.Write((uint)value.w, QConsts.rotBitComp);
            writer.Write(value.x, settings.rotsmall, settings.rotlarge, settings.rotfull);
            writer.Write(value.y, settings.rotsmall, settings.rotlarge, settings.rotfull);
            writer.Write(value.z, settings.rotsmall, settings.rotlarge, settings.rotfull);
        }

        public static float ReadAngle(this IStreamReader reader, in NetPrecisionSettings settings)
        {
            return Quantization.Dequantize(reader.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull), QConsts.rotRange, settings.rotfull);
        }

        public static void WriteAngle(this IStreamWriter writer, in float angle, in NetPrecisionSettings settings)
        {
            var quant = Quantization.Quantize(angle, QConsts.rotRange, settings.rotfull);
            writer.Write(quant, settings.rotsmall, settings.rotlarge, settings.rotfull);
        }
    }
}
