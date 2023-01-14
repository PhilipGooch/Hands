using Unity.Mathematics;

namespace Recoil
{
    public struct ArticulatedBodyInertia
    {
        public lt3x3 M;
        public float3x3 H;
        public lt3x3 I;

        public ArticulatedBodyInertia(lt3x3 m, float3x3 h, lt3x3 i)
        {
            M = m;
            H = h;
            I = i;
        }

        public static ArticulatedBodyInertia operator +(ArticulatedBodyInertia a, ArticulatedBodyInertia b)
        {
            return new ArticulatedBodyInertia(a.M + b.M, a.H + b.H, a.I + b.I);
        }
        public static ArticulatedBodyInertia operator -(ArticulatedBodyInertia a, ArticulatedBodyInertia b)
        {
            return new ArticulatedBodyInertia(a.M - b.M, a.H - b.H, a.I - b.I);
        }
        public static ArticulatedBodyInertia operator -(ArticulatedBodyInertia b)
        {
            return new ArticulatedBodyInertia(- b.M, - b.H, - b.I);
        }
        public static ArticulatedBodyInertia FromRigidBodyInertia(RigidBodyInertia rbi)
        {
            return new ArticulatedBodyInertia(lt3x3.Diagonal(rbi.m),re.cross(rbi.h), rbi.I);
        }
        public static ArticulatedBodyInertia TranslateMassBy(lt3x3 M, float3 r)
        {

            var HminusrxM = -re.cross(r, M);
            return new ArticulatedBodyInertia(M, HminusrxM, re.ltcross(HminusrxM, r));
        }        
        

        public ArticulatedBodyInertia TranslateBy(float3 r)
        {
            // V2. optimized
            var HminusrxM = H - re.cross(r, M);
            return new ArticulatedBodyInertia(M, HminusrxM, I - re.ltcrossT(r, H) + re.ltcross(HminusrxM, r));

        }

        public float6x6 ToFloat6x6()
        {
            return new float6x6(I.ToFloat3x3(), H, math.transpose(H), M.ToFloat3x3());
        }

        public override string ToString()
        {
            return $"{I} {H} {M}";
        }
    }
}