using System;
using Unity.Mathematics;

namespace NBG.Core
{
    public static class Quantization
    {
        public const float QuaternionRange = 0.707107f; // a bit more to prevent overflow

        public static int Quantize(in float value, float range, ushort bits)
        {
            var max = 1 << (bits - 1);
            var s = value / range * (max - 1);
            //if (s < -max) { Debug.LogWarningFormat("Value out of range {0} {1}",value,range); return (-max); }
            //if (s >= max) { Debug.LogWarningFormat("Value out of range {0} {1}", value, range); return (max - 1); }
            if (s < -max)
                return (-max);
            if (s >= max)
                return (max);

            return (int)s;
            //if (value <= -range) return (short)-max;
            //if (value >= range) return (short)max;
            //return (short)(value / range*max);
        }

        public static float Dequantize(in int value, float range, ushort bits)
        {
            var max = 1 << (bits - 1);
            return range * value / (max - 1);
        }

        public static int2 Quantize(in float2 vec, float range, ushort bits)
        {
            return new int2()
            {
                x = Quantize(vec.x, range, bits),
                y = Quantize(vec.y, range, bits),
            };
        }

        public static float2 Dequantize(in int2 value, float range, ushort bits)
        {
            return new float2()
            {
                x = Dequantize(value.x, range, bits),
                y = Dequantize(value.y, range, bits),
            };
        }

        public static int3 Quantize(in float3 vec, float range, ushort bits)
        {
            return new int3()
            {
                x = Quantize(vec.x, range, bits),
                y = Quantize(vec.y, range, bits),
                z = Quantize(vec.z, range, bits),
            };
        }

        public static float3 Dequantize(in int3 value, float range, ushort bits)
        {
            return new float3()
            {
                x = Dequantize(value.x, range, bits),
                y = Dequantize(value.y, range, bits),
                z = Dequantize(value.z, range, bits),
            };
        }

        public static int4 Quantize(in quaternion q, ushort bits)
        {
            // find largest component
            var absX = math.abs(q.value.x);
            var absY = math.abs(q.value.y);
            var absZ = math.abs(q.value.z);
            var absW = math.abs(q.value.w);
            var sel = 0;
            if (absX > absY) // not Y
            {
                if (absX > absZ) // not Z
                {
                    sel = absX > absW ? 0 : 3;
                }
                else //not X
                {
                    sel = absZ > absW ? 2 : 3;
                }
            }
            else // not X
            {
                if (absY > absZ) // not Z
                {
                    sel = absY > absW ? 1 : 3;
                }
                else //not Y
                {
                    sel = absZ > absW ? 2 : 3;
                }
            }

            int4 ret;
            switch (sel)
            {
                case 0: //x
                    ret = new int4(
                        Quantize(q.value.y, QuaternionRange, bits),
                        Quantize(q.value.z, QuaternionRange, bits),
                        Quantize(q.value.w, QuaternionRange, bits),
                        (int)math.sign(q.value.x));
                    break;
                case 1: //y
                    ret = new int4(
                        Quantize(q.value.x, QuaternionRange, bits),
                        Quantize(q.value.z, QuaternionRange, bits),
                        Quantize(q.value.w, QuaternionRange, bits),
                        (int)math.sign(q.value.y));
                    break;
                case 2: //z
                    ret = new int4(
                        Quantize(q.value.x, QuaternionRange, bits),
                        Quantize(q.value.y, QuaternionRange, bits),
                        Quantize(q.value.w, QuaternionRange, bits),
                        (int)math.sign(q.value.z));
                    break;
                case 3: //w
                    ret = new int4(
                        Quantize(q.value.x, QuaternionRange, bits),
                        Quantize(q.value.y, QuaternionRange, bits),
                        Quantize(q.value.z, QuaternionRange, bits),
                        (int)math.sign(q.value.w));
                    break;
                default:
                    throw new System.InvalidOperationException("can't get here");
            }

            ret *= (ret.w); //if the dropped component was negative, we invert
            ret.w = sel;
            return ret;
        }

        public static quaternion Dequantize(in int4 quantized, ushort bits)
        {
            var da = Dequantize(quantized.x, QuaternionRange, bits);
            var db = Dequantize(quantized.y, QuaternionRange, bits);
            var dc = Dequantize(quantized.z, QuaternionRange, bits);
            
            var ddrop = math.sqrt(1 - da * da - db * db - dc * dc);
            switch (quantized.w)
            {
                case 0: //x
                    return new quaternion(ddrop, da, db, dc);
                case 1: //y
                    return new quaternion(da, ddrop, db, dc);
                case 2: //z
                    return new quaternion(da, db, ddrop, dc);
                case 3: //w
                    return new quaternion(da, db, dc, ddrop);
                default:
                    throw new ArgumentOutOfRangeException("quaternion component selector out of range");
            }
        }
    }

    public static class QuantizationExtensions
    {
        public static int Quantize(this float value, float range, ushort bits) => Quantization.Quantize(value, range, bits);
        public static float Dequantize(this int value, float range, ushort bits) => Quantization.Dequantize(value, range, bits);

        public static int2 Quantize(this float2 value, float range, ushort bits) => Quantization.Quantize(value, range, bits);
        public static float2 Dequantize(this int2 value, float range, ushort bits) => Quantization.Dequantize(value, range, bits);

        public static int3 Quantize(this float3 value, float range, ushort bits) => Quantization.Quantize(value, range, bits);
        public static float3 Dequantize(this int3 value, float range, ushort bits) => Quantization.Dequantize(value, range, bits);

        public static int4 Quantize(this quaternion value, ushort bits) => Quantization.Quantize(value, bits);
        public static quaternion Dequantize(this int4 value, ushort bits) => Quantization.Dequantize(value, bits);
    }
}
