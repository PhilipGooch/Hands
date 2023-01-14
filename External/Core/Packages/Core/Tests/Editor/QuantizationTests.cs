using NUnit.Framework;

namespace NBG.Core.Editor.Tests
{
    public class QuantizationTests
    {
        const float RANGE = 1;
        const int BITS = 4;

        [Test]
        public void FloatMinExtremumTest()
        {
            float startingValue = -RANGE;
            var intValue = Quantization.Quantize(in startingValue, RANGE, BITS);
            float result = Quantization.Dequantize(intValue, RANGE, BITS);
            Assert.IsTrue(startingValue.Equals(result), "Starting value is not the same as end value");
        }

        [Test]
        public void FloatMaxExtremumTest()
        {
            float startingValue = RANGE;
            var intValue = Quantization.Quantize(in startingValue, RANGE, BITS);
            float result = Quantization.Dequantize(intValue, RANGE, BITS);
            Assert.IsTrue(startingValue.Equals(result), "Starting value is not the same as end value");
        }
    }
}
