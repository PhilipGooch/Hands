using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Recoil
{
    [Serializable]
    public struct Spring
    {
        const float EPSILON = .0001f; // too weak springs generate degenerate matrices
        const float DAMPER_LERP_POWER = 1.25f; // spring is lerped using square, to preserve same damping rate damper should be lerped linearly, but raising it allows underdamping when spring is relaxed
        public float kp;
        public float kd;
        public float minSpring;
        public float maxSpring;
        public bool isFree => kp>=0 && kd>=0 && kp  < EPSILON && kd < EPSILON;
        public bool hasLimit => kp > 0 && maxSpring > 0;

        public static Spring free => new Spring(0, 0);
        public static Spring stiff => new Spring(-1, -1);

        public Spring(float kp, float kd, float maxSpring = 0)
        {
            this.kp = kp;
            this.kd = kd;
            this.minSpring = maxSpring;
            this.maxSpring = maxSpring;
        }
        public Spring(float kp, float kd, float minSpring,float maxSpring)
        {
            this.kp = kp;
            this.kd = kd;
            this.minSpring = minSpring;
            this.maxSpring = maxSpring;
        }

        public static Spring Lerp(Spring a, Spring b, float mix)
        {
            // if one spring has no limit use limit from the other, otherwise blend
            var min = a.minSpring == 0 ? b.minSpring : b.minSpring == 0 ? a.minSpring : squarelerp(a.minSpring, b.minSpring, mix);
            var max = a.maxSpring == 0 ? b.maxSpring : b.maxSpring == 0 ? a.maxSpring : squarelerp(a.maxSpring, b.maxSpring, mix);

            return new Spring(squarelerp(a.kp, b.kp, mix), powlerp(a.kd, b.kd, mix, DAMPER_LERP_POWER), min, max);

        }
        public static float squarelerp(float a, float b, float mix)
        {
            var blend = math.lerp(math.sqrt(a), math.sqrt(b), mix);
            return blend * blend;
        }
        public static float powlerp(float a, float b, float mix, float p)
        {
            a = math.max(a, 0); // can't pow negative
            b = math.max(b, 0); // can't pow negative
            var blend = math.lerp(math.pow(a,1/p), math.pow(b,1/p), mix);
            blend = math.max(blend, 0);// can't pow negative
            return math.pow( blend,p);
        }
        public void Lerp(Spring b, float mix)
        {
            kp = math.lerp(kp, b.kp, mix);
            kd = math.lerp(kd, b.kd, mix);
            minSpring = math.lerp(minSpring, b.minSpring, mix);
            maxSpring = math.lerp(maxSpring, b.maxSpring, mix);
        }

        public static Spring operator *(Spring a, float mul)
        {
            mul = math.max(mul, 0); // can't pow negative
            //return new Spring(a.kp * mul * mul, a.kd * mul, a.maxSpring * mul * mul);
            return new Spring(a.kp * mul * mul, a.kd * math.pow( mul, DAMPER_LERP_POWER), a.minSpring * mul * mul, a.maxSpring * mul * mul);
        }
     

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateSpringBetaGamma(float k, float d, float h, out float gamma, out float beta)
        {
            if (k < 0) { gamma = 0; beta = 0; } // stiff joint gamma and beta is 0
            else if (k <EPSILON && d < EPSILON) { gamma = 1; beta = 0; } // disabled joint force K identity, too weak joints generate problems on inverse
            else
            {
                gamma = h * (d + h * k);
                
                gamma = gamma >= EPSILON ? 1.0f / gamma : 0.0f;
                beta = h * k * gamma;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CalculateSpring(float err, float velErr, float h, out float gamma, out float bias)
        {
            var beta = 0f;
            var F = err * kp + velErr * kd;
            if (maxSpring != 0 && F>maxSpring)
            {
                var scale = re.SoftClamp(F, maxSpring) / F;
                scale = math.max(scale, .01f); // prevent generating too small kp/kd
                CalculateSpringBetaGamma(kp * scale, kd * math.pow(scale, DAMPER_LERP_POWER), h, out gamma, out beta);
            }
            else if (minSpring != 0 && F<-minSpring)
            {
                var scale = re.SoftClamp(F, minSpring) / F;
                scale = math.max(scale, .01f); // prevent generating too small kp/kd
                CalculateSpringBetaGamma(kp * scale, kd * math.pow(scale, DAMPER_LERP_POWER), h, out gamma, out beta);
            }
            else
                CalculateSpringBetaGamma(kp, kd , h, out gamma, out beta);
            bias = beta * err + velErr;
        }

    }
}