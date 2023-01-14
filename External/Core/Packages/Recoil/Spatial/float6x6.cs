using Unity.Mathematics;

namespace Recoil
{
    public struct float3x6 // vertical
    {
        public float3x3 l;
        public float3x3 r;

        public float3x6(float3x3 l, float3x3 r)
        {
            this.l = l;
            this.r = r;
        }
    }

    public struct float6x3 // vertical
    {
        public float3x3 t;
        public float3x3 b;

        public float6x3(float3x3 t, float3x3 b)
        {
            this.t = t;
            this.b = b;
        }
    }

    public struct float6x6
    {
        public float3x3 tl;
        public float3x3 tr;
        public float3x3 bl;
        public float3x3 br;

        public float6x6(float3x3 tl, float3x3 tr,
            float3x3 bl, float3x3 br)
        {
            this.tl = tl;
            this.tr = tr;
            this.bl = bl;
            this.br = br;
        }

        public static float6x6 operator +(float6x6 a, float6x6 b)
        {
            return new float6x6(a.tl+b.tl, a.tr + b.tr, a.bl + b.bl, a.br + b.br);
        }
        public static float6x6 operator -(float6x6 a, float6x6 b)
        {
            return new float6x6(a.tl - b.tl, a.tr - b.tr, a.bl - b.bl, a.br - b.br);
        }
        public static float6x6 operator -(float6x6 b)
        {
            return new float6x6(- b.tl, - b.tr, - b.bl, - b.br);
        }

        public override string ToString()
        {
            var str = "";
            str += $"{tl[0][0]:F3}"; str += $"\t{tl[1][0]:F3}"; str += $"\t{tl[2][0]:F3}"; str += $"\t{tr[0][0]:F3}"; str += $"\t{tr[1][0]:F3}"; str += $"\t{tr[2][0]:F3}\n";
            str += $"{tl[0][1]:F3}"; str += $"\t{tl[1][1]:F3}"; str += $"\t{tl[2][1]:F3}"; str += $"\t{tr[0][1]:F3}"; str += $"\t{tr[1][1]:F3}"; str += $"\t{tr[2][1]:F3}\n";
            str += $"{tl[0][2]:F3}"; str += $"\t{tl[1][2]:F3}"; str += $"\t{tl[2][2]:F3}"; str += $"\t{tr[0][2]:F3}"; str += $"\t{tr[1][2]:F3}"; str += $"\t{tr[2][2]:F3}\n";
            str += $"{bl[0][0]:F3}"; str += $"\t{bl[1][0]:F3}"; str += $"\t{bl[2][0]:F3}"; str += $"\t{br[0][0]:F3}"; str += $"\t{br[1][0]:F3}"; str += $"\t{br[2][0]:F3}\n";
            str += $"{bl[0][1]:F3}"; str += $"\t{bl[1][1]:F3}"; str += $"\t{bl[2][1]:F3}"; str += $"\t{br[0][1]:F3}"; str += $"\t{br[1][1]:F3}"; str += $"\t{br[2][1]:F3}\n";
            str += $"{bl[0][2]:F3}"; str += $"\t{bl[1][2]:F3}"; str += $"\t{bl[2][2]:F3}"; str += $"\t{br[0][2]:F3}"; str += $"\t{br[1][2]:F3}"; str += $"\t{br[2][2]:F3}\n";
            return str;
        }
    }

    public partial class re
    {
        public static float6x6 inverse(float6x6 m)
        {
            var A = m.tl;
            var B = m.tr;
            var C = m.bl;
            var D = m.br;

            var Ainv = re.inverse(A);

            var DCABinv = inverse( D -  math.mul(re.mul(C, Ainv), B));
            var AiB = re.mul(Ainv, B);
            var CAi = re.mul(C, Ainv);
            var Ai = Ainv + math.mul(re.mul(AiB, DCABinv), CAi);
            var Bi = -re.mul(AiB, DCABinv);
            var Ci = -math.mul( DCABinv,CAi);
            var Di = DCABinv;
             
            return  new float6x6(Ai,Bi,Ci,Di);
        }

        public static float6x6 mul(float6x6 a, float6x6 b)
        {
            return new float6x6(re.mul(a.tl,b.tl)+re.mul(a.tr,b.bl),re.mul(a.tl,b.tr)+re.mul(a.tr,b.br),
                re.mul(a.bl,b.tl)+re.mul(a.br,b.bl),re.mul(a.bl,b.tr)+re.mul(a.br,b.br));

        }
        public static float6x6 transpose(float6x6 a)
        {
            return new float6x6(math.transpose(a.tl),math.transpose(a.bl),
                math.transpose(a.tr),math.transpose(a.br));

        }
        public static float3x6 transpose(float6x3 a)
        {
            return new float3x6(math.transpose(a.t),math.transpose(a.b));

        }
        public static float6x3 transpose(float3x6 a)
        {
            return new float6x3(math.transpose(a.l),math.transpose(a.r));

        }public static float6x3 mul(float6x6 a, float6x3 b)
        {
            return new float6x3(re.mul(a.tl,b.t)+re.mul(a.tr,b.b),
                re.mul(a.bl,b.t)+re.mul(a.br,b.b));

        }
        public static float6x6 mul(float6x3 a, float3x6 b)
        {
            return new float6x6(re.mul(a.t,b.l),re.mul(a.t,b.r),
                re.mul(a.b,b.l),re.mul(a.b,b.r));

        } 
        public static float3x3 mul(float3x6 a, float6x3 b)
        {
            return  re.mul(a.l,b.t)+re.mul(a.r,b.b);
        }
        public static ForceVector mul(float6x6 a, ForceVector v)
        {
            return new ForceVector(re.mul(a.tl,v.angular)+re.mul(a.tr,v.linear), re.mul(a.bl,v.angular)+re.mul(a.br,v.linear));
        }
        public static ForceVector mul(float6x6 a, MotionVector v)
        {
            return new ForceVector(re.mul(a.tl, v.angular) + re.mul(a.tr, v.linear), re.mul(a.bl, v.angular) + re.mul(a.br, v.linear));
        }
        public static float6x3 mul(float6x3 a, float3x3 b)
        {
            return new float6x3(re.mul(a.t,b),
                re.mul(a.b,b));

        } 
       
    }
}