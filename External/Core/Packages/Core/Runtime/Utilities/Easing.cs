using UnityEngine;

namespace NBG.Core.Easing
{
    //MIT https://github.com/tomtc123/ugui-Tween-Tool/blob/master/uGUI/Assets/uGUI/Scripts/Tween/Tools/EaseManager.cs
    public enum EaseType
    {
        none,
        easeInQuad,
        easeOutQuad,
        easeInOutQuad,
        easeInCubic,
        easeOutCubic,
        easeInOutCubic,
        easeInQuart,
        easeOutQuart,
        easeInOutQuart,
        easeInQuint,
        easeOutQuint,
        easeInOutQuint,
        easeInSine,
        easeOutSine,
        easeInOutSine,
        easeInExpo,
        easeOutExpo,
        easeInOutExpo,
        easeInCirc,
        easeOutCirc,
        easeInOutCirc,
        linear,
        spring,
        easeInBounce,
        easeOutBounce,
        easeInOutBounce,
        easeInBack,
        easeOutBack,
        easeInOutBack,
        easeInElastic,
        easeOutElastic,
        easeInOutElastic,
        punch
    }

    public static class Ease
    {

        public static float linear(float start, float end, float value)
        {
            return Mathf.Lerp(start, end, value);
        }

        public static float clerp(float start, float end, float value)
        {
            float min = 0.0f;
            float max = 360.0f;
            float half = Mathf.Abs((max - min) / 2.0f);
            float retval = 0.0f;
            float diff = 0.0f;
            if ((end - start) < -half)
            {
                diff = ((max - start) + end) * value;
                retval = start + diff;
            }
            else if ((end - start) > half)
            {
                diff = -((max - end) + start) * value;
                retval = start + diff;
            }
            else retval = start + (end - start) * value;
            return retval;
        }

        public static float spring(float start, float end, float value)
        {
            value = Mathf.Clamp01(value);
            value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
            return start + (end - start) * value;
        }

        public static float easeInQuad(float start, float end, float value)
        {
            end -= start;
            return end * value * value + start;
        }

        public static float easeOutQuad(float start, float end, float value)
        {
            end -= start;
            return -end * value * (value - 2) + start;
        }

        public static float easeInOutQuad(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end / 2 * value * value + start;
            value--;
            return -end / 2 * (value * (value - 2) - 1) + start;
        }

        public static float easeInCubic(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value + start;
        }

        public static float easeOutCubic(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value + 1) + start;
        }

        public static float easeInOutCubic(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end / 2 * value * value * value + start;
            value -= 2;
            return end / 2 * (value * value * value + 2) + start;
        }

        public static float easeInQuart(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value * value + start;
        }

        public static float easeOutQuart(float start, float end, float value)
        {
            value--;
            end -= start;
            return -end * (value * value * value * value - 1) + start;
        }

        public static float easeInOutQuart(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end / 2 * value * value * value * value + start;
            value -= 2;
            return -end / 2 * (value * value * value * value - 2) + start;
        }

        public static float easeInQuint(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value * value * value + start;
        }

        public static float easeOutQuint(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value * value * value + 1) + start;
        }

        public static float easeInOutQuint(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end / 2 * value * value * value * value * value + start;
            value -= 2;
            return end / 2 * (value * value * value * value * value + 2) + start;
        }

        public static float easeInSine(float start, float end, float value)
        {
            end -= start;
            return -end * Mathf.Cos(value / 1 * (Mathf.PI / 2)) + end + start;
        }

        public static float easeOutSine(float start, float end, float value)
        {
            end -= start;
            return end * Mathf.Sin(value / 1 * (Mathf.PI / 2)) + start;
        }

        public static float easeInOutSine(float start, float end, float value)
        {
            end -= start;
            return -end / 2 * (Mathf.Cos(Mathf.PI * value / 1) - 1) + start;
        }

        public static float easeInExpo(float start, float end, float value)
        {
            end -= start;
            return end * Mathf.Pow(2, 10 * (value / 1 - 1)) + start;
        }

        public static float easeOutExpo(float start, float end, float value)
        {
            end -= start;
            return end * (-Mathf.Pow(2, -10 * value / 1) + 1) + start;
        }

        public static float easeInOutExpo(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end / 2 * Mathf.Pow(2, 10 * (value - 1)) + start;
            value--;
            return end / 2 * (-Mathf.Pow(2, -10 * value) + 2) + start;
        }

        public static float easeInCirc(float start, float end, float value)
        {
            end -= start;
            return -end * (Mathf.Sqrt(1 - value * value) - 1) + start;
        }

        public static float easeOutCirc(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * Mathf.Sqrt(1 - value * value) + start;
        }

        public static float easeInOutCirc(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return -end / 2 * (Mathf.Sqrt(1 - value * value) - 1) + start;
            value -= 2;
            return end / 2 * (Mathf.Sqrt(1 - value * value) + 1) + start;
        }

        public static float easeInBounce(float start, float end, float value)
        {
            end -= start;
            float d = 1f;
            return end - easeOutBounce(0, end, d - value) + start;
        }

        public static float easeOutBounce(float start, float end, float value)
        {
            value /= 1f;
            end -= start;
            if (value < (1 / 2.75f))
            {
                return end * (7.5625f * value * value) + start;
            }
            else if (value < (2 / 2.75f))
            {
                value -= (1.5f / 2.75f);
                return end * (7.5625f * (value) * value + .75f) + start;
            }
            else if (value < (2.5 / 2.75))
            {
                value -= (2.25f / 2.75f);
                return end * (7.5625f * (value) * value + .9375f) + start;
            }
            else
            {
                value -= (2.625f / 2.75f);
                return end * (7.5625f * (value) * value + .984375f) + start;
            }
        }

        public static float easeInOutBounce(float start, float end, float value)
        {
            end -= start;
            float d = 1f;
            if (value < d / 2) return easeInBounce(0, end, value * 2) * 0.5f + start;
            else return easeOutBounce(0, end, value * 2 - d) * 0.5f + end * 0.5f + start;
        }

        public static float easeInBack(float start, float end, float value)
        {
            end -= start;
            value /= 1;
            float s = 1.70158f;
            return end * (value) * value * ((s + 1) * value - s) + start;
        }

        public static float easeOutBack(float start, float end, float value)
        {
            float s = 1.70158f;
            end -= start;
            value = (value / 1) - 1;
            return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
        }

        public static float easeInOutBack(float start, float end, float value)
        {
            float s = 1.70158f;
            end -= start;
            value /= .5f;
            if ((value) < 1)
            {
                s *= (1.525f);
                return end / 2 * (value * value * (((s) + 1) * value - s)) + start;
            }
            value -= 2;
            s *= (1.525f);
            return end / 2 * ((value) * value * (((s) + 1) * value + s) + 2) + start;
        }

        public static float punch(float amplitude, float value)
        {
            float s = 9;
            if (value == 0)
            {
                return 0;
            }
            if (value == 1)
            {
                return 0;
            }
            float period = 1 * 0.3f;
            s = period / (2 * Mathf.PI) * Mathf.Asin(0);
            return (amplitude * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * 1 - s) * (2 * Mathf.PI) / period));
        }

        public static float easeInElastic(float start, float end, float value)
        {
            end -= start;

            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return start;

            if ((value /= d) == 1) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            return -(a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
        }

        public static float easeOutElastic(float start, float end, float value)
        {
            //Thank you to rafael.marteleto for fixing this as a port over from Pedro's UnityTween
            end -= start;

            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return start;

            if ((value /= d) == 1) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + end + start);
        }

        public static float easeInOutElastic(float start, float end, float value)
        {
            end -= start;

            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return start;

            if ((value /= d / 2) == 2) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            if (value < 1) return -0.5f * (a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
            return a * Mathf.Pow(2, -10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
        }

        public static float EasingFromType(float start, float end, float t, EaseType type)
        {
            switch (type)
            {
                case EaseType.easeInQuad:
                    return easeInQuad(start, end, t);

                case EaseType.easeOutQuad:
                    return easeOutQuad(start, end, t);

                case EaseType.easeInOutQuad:
                    return easeInOutQuad(start, end, t);

                case EaseType.easeInCubic:
                    return easeInCubic(start, end, t);

                case EaseType.easeOutCubic:
                    return easeOutCubic(start, end, t);

                case EaseType.easeInOutCubic:
                    return easeInOutCubic(start, end, t);

                case EaseType.easeInQuart:
                    return easeInQuart(start, end, t);

                case EaseType.easeOutQuart:
                    return easeOutQuart(start, end, t);

                case EaseType.easeInOutQuart:
                    return easeInOutQuart(start, end, t);

                case EaseType.easeInQuint:
                    return easeInQuint(start, end, t);

                case EaseType.easeOutQuint:
                    return easeOutQuint(start, end, t);

                case EaseType.easeInOutQuint:
                    return easeInOutQuint(start, end, t);

                case EaseType.easeInSine:
                    return easeInSine(start, end, t);

                case EaseType.easeOutSine:
                    return easeOutSine(start, end, t);

                case EaseType.easeInOutSine:
                    return easeInOutSine(start, end, t);

                case EaseType.easeInExpo:
                    return easeInExpo(start, end, t);

                case EaseType.easeOutExpo:
                    return easeOutExpo(start, end, t);

                case EaseType.easeInOutExpo:
                    return easeInOutExpo(start, end, t);

                case EaseType.easeInCirc:
                    return easeInCirc(start, end, t);

                case EaseType.easeOutCirc:
                    return easeOutCirc(start, end, t);

                case EaseType.easeInOutCirc:
                    return easeInOutCirc(start, end, t);

                case EaseType.linear:
                    return linear(start, end, t);

                case EaseType.spring:
                    return spring(start, end, t);

                case EaseType.easeInBounce:
                    return easeInBounce(start, end, t);

                case EaseType.easeOutBounce:
                    return easeOutBounce(start, end, t);

                case EaseType.easeInOutBounce:
                    return easeInOutBounce(start, end, t);

                case EaseType.easeInBack:
                    return easeInBack(start, end, t);

                case EaseType.easeOutBack:
                    return easeOutBack(start, end, t);

                case EaseType.easeInOutBack:
                    return easeInOutBack(start, end, t);

                case EaseType.easeInElastic:
                    return easeInElastic(start, end, t);

                case EaseType.easeOutElastic:
                    return easeOutElastic(start, end, t);

                case EaseType.easeInOutElastic:
                    return easeInOutElastic(start, end, t);
            }
            return linear(start, end, t);
        }

        #region 0-1 ease
        public static float linear(float value)
        {
            return value;
        }

        public static float clerp(float value)
        {
            float min = 0.0f;
            float max = 360.0f;
            float half = Mathf.Abs((max - min) / 2.0f);
            float retval = 0.0f;
            float diff = 0.0f;
            if ((1 - 0) < -half)
            {
                diff = ((max - 0) + 1) * value;
                retval = 0 + diff;
            }
            else if ((1 - 0) > half)
            {
                diff = -((max - 1) + 0) * value;
                retval = 0 + diff;
            }
            else retval = 0 + (1 - 0) * value;
            return retval;
        }

        public static float spring(float value)
        {
            value = Mathf.Clamp01(value);
            value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
            return 0 + (1 - 0) * value;
        }

        public static float easeInQuad(float value)
        {
            return 1 * value * value + 0;
        }

        public static float easeOutQuad(float value)
        {
            return -1 * value * (value - 2) + 0;
        }

        public static float easeInOutQuad(float value)
        {
            value /= .5f;
            if (value < 1) return 1 / 2 * value * value + 0;
            value--;
            return -1 / 2 * (value * (value - 2) - 1) + 0;
        }

        public static float easeInCubic(float value)
        {
            return 1 * value * value * value + 0;
        }

        public static float easeOutCubic(float value)
        {
            value--;
            return 1 * (value * value * value + 1) + 0;
        }

        public static float easeInOutCubic(float value)
        {
            value /= .5f;
            if (value < 1) return 1 / 2 * value * value * value + 0;
            value -= 2;
            return 1 / 2 * (value * value * value + 2) + 0;
        }

        public static float easeInQuart(float value)
        {
            return 1 * value * value * value * value + 0;
        }

        public static float easeOutQuart(float value)
        {
            value--;
            return -1 * (value * value * value * value - 1) + 0;
        }

        public static float easeInOutQuart(float value)
        {
            value /= .5f;
            if (value < 1) return 1 / 2 * value * value * value * value + 0;
            value -= 2;
            return -1 / 2 * (value * value * value * value - 2) + 0;
        }

        public static float easeInQuint(float value)
        {
            return 1 * value * value * value * value * value + 0;
        }

        public static float easeOutQuint(float value)
        {
            value--;
            return 1 * (value * value * value * value * value + 1) + 0;
        }

        public static float easeInOutQuint(float value)
        {
            value /= .5f;
            if (value < 1) return 1 / 2 * value * value * value * value * value + 0;
            value -= 2;
            return 1 / 2 * (value * value * value * value * value + 2) + 0;
        }

        public static float easeInSine(float value)
        {
            return -1 * Mathf.Cos(value / 1 * (Mathf.PI / 2)) + 1 + 0;
        }

        public static float easeOutSine(float value)
        {
            return 1 * Mathf.Sin(value / 1 * (Mathf.PI / 2)) + 0;
        }

        public static float easeInOutSine(float value)
        {
            return -1 / 2 * (Mathf.Cos(Mathf.PI * value / 1) - 1) + 0;
        }

        public static float easeInExpo(float value)
        {
            return 1 * Mathf.Pow(2, 10 * (value / 1 - 1)) + 0;
        }

        public static float easeOutExpo(float value)
        {
            return 1 * (-Mathf.Pow(2, -10 * value / 1) + 1) + 0;
        }

        public static float easeInOutExpo(float value)
        {
            value /= .5f;
            if (value < 1) return 1 / 2 * Mathf.Pow(2, 10 * (value - 1)) + 0;
            value--;
            return 1 / 2 * (-Mathf.Pow(2, -10 * value) + 2) + 0;
        }

        public static float easeInCirc(float value)
        {
            return -1 * (Mathf.Sqrt(1 - value * value) - 1) + 0;
        }

        public static float easeOutCirc(float value)
        {
            value--;
            return 1 * Mathf.Sqrt(1 - value * value) + 0;
        }

        public static float easeInOutCirc(float value)
        {
            value /= .5f;
            if (value < 1) return -1 / 2 * (Mathf.Sqrt(1 - value * value) - 1) + 0;
            value -= 2;
            return 1 / 2 * (Mathf.Sqrt(1 - value * value) + 1) + 0;
        }

        public static float easeInBounce(float value)
        {
            float d = 1f;
            return 1 - easeOutBounce(0, 1, d - value) + 0;
        }

        public static float easeOutBounce(float value)
        {
            value /= 1f;
            if (value < (1 / 2.75f))
            {
                return 1 * (7.5625f * value * value) + 0;
            }
            else if (value < (2 / 2.75f))
            {
                value -= (1.5f / 2.75f);
                return 1 * (7.5625f * (value) * value + .75f) + 0;
            }
            else if (value < (2.5 / 2.75))
            {
                value -= (2.25f / 2.75f);
                return 1 * (7.5625f * (value) * value + .9375f) + 0;
            }
            else
            {
                value -= (2.625f / 2.75f);
                return 1 * (7.5625f * (value) * value + .984375f) + 0;
            }
        }

        public static float easeInOutBounce(float value)
        {
            float d = 1f;
            if (value < d / 2) return easeInBounce(0, 1, value * 2) * 0.5f + 0;
            else return easeOutBounce(0, 1, value * 2 - d) * 0.5f + 1 * 0.5f + 0;
        }

        public static float easeInBack(float value)
        {
            value /= 1;
            float s = 1.70158f;
            return 1 * (value) * value * ((s + 1) * value - s) + 0;
        }

        public static float easeOutBack(float value)
        {
            float s = 1.70158f;
            value = (value / 1) - 1;
            return 1 * ((value) * value * ((s + 1) * value + s) + 1) + 0;
        }

        public static float easeInOutBack(float value)
        {
            float s = 1.70158f;
            value /= .5f;
            if ((value) < 1)
            {
                s *= (1.525f);
                return 1 / 2 * (value * value * (((s) + 1) * value - s)) + 0;
            }
            value -= 2;
            s *= (1.525f);
            return 1 / 2 * ((value) * value * (((s) + 1) * value + s) + 2) + 0;
        }

        public static float easeInElastic(float value)
        {
            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return 0;

            if ((value /= d) == 1) return 0 + 1;

            if (a == 0f || a < Mathf.Abs(1))
            {
                a = 1;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
            }

            return -(a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + 0;
        }

        public static float easeOutElastic(float value)
        {
            //Thank you to rafael.marteleto for fixing this as a port over from Pedro's UnityTween
            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return 0;

            if ((value /= d) == 1) return 0 + 1;

            if (a == 0f || a < Mathf.Abs(1))
            {
                a = 1;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
            }

            return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + 1 + 0);
        }

        public static float easeInOutElastic(float value)
        {

            float d = 1f;
            float p = d * .3f;
            float s = 0;
            float a = 0;

            if (value == 0) return 0;

            if ((value /= d / 2) == 2) return 0 + 1;

            if (a == 0f || a < Mathf.Abs(1))
            {
                a = 1;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
            }

            if (value < 1) return -0.5f * (a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + 0;
            return a * Mathf.Pow(2, -10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + 1 + 0;
        }
        #endregion

        public static float EasingFromType(float t, EaseType type)
        {
            switch (type)
            {
                case EaseType.easeInQuad:
                    return easeInQuad(t);

                case EaseType.easeOutQuad:
                    return easeOutQuad(t);

                case EaseType.easeInOutQuad:
                    return easeInOutQuad(t);

                case EaseType.easeInCubic:
                    return easeInCubic(t);

                case EaseType.easeOutCubic:
                    return easeOutCubic(t);

                case EaseType.easeInOutCubic:
                    return easeInOutCubic(t);

                case EaseType.easeInQuart:
                    return easeInQuart(t);

                case EaseType.easeOutQuart:
                    return easeOutQuart(t);

                case EaseType.easeInOutQuart:
                    return easeInOutQuart(t);

                case EaseType.easeInQuint:
                    return easeInQuint(t);

                case EaseType.easeOutQuint:
                    return easeOutQuint(t);

                case EaseType.easeInOutQuint:
                    return easeInOutQuint(t);

                case EaseType.easeInSine:
                    return easeInSine(t);

                case EaseType.easeOutSine:
                    return easeOutSine(t);

                case EaseType.easeInOutSine:
                    return easeInOutSine(t);

                case EaseType.easeInExpo:
                    return easeInExpo(t);

                case EaseType.easeOutExpo:
                    return easeOutExpo(t);

                case EaseType.easeInOutExpo:
                    return easeInOutExpo(t);

                case EaseType.easeInCirc:
                    return easeInCirc(t);

                case EaseType.easeOutCirc:
                    return easeOutCirc(t);

                case EaseType.easeInOutCirc:
                    return easeInOutCirc(t);

                case EaseType.linear:
                    return linear(t);

                case EaseType.spring:
                    return spring(t);

                case EaseType.easeInBounce:
                    return easeInBounce(t);

                case EaseType.easeOutBounce:
                    return easeOutBounce(t);

                case EaseType.easeInOutBounce:
                    return easeInOutBounce(t);

                case EaseType.easeInBack:
                    return easeInBack(t);

                case EaseType.easeOutBack:
                    return easeOutBack(t);

                case EaseType.easeInOutBack:
                    return easeInOutBack(t);

                case EaseType.easeInElastic:
                    return easeInElastic(t);

                case EaseType.easeOutElastic:
                    return easeOutElastic(t);

                case EaseType.easeInOutElastic:
                    return easeInOutElastic(t);
            }
            return linear(t);
        }
    }

}
