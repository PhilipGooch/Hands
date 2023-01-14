using UnityEngine;
using Recoil;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Mathematics;

namespace NBG.Recoil.Editor.Tests
{
    public class MathTests
    {
        [Test]
        public void lt3x3_inverse_WithZeroMatrixPrintsError()
        {
            lt3x3 mat = lt3x3.zero;

            LogAssert.Expect(LogType.Error, "Can't calculate inverse for matrix with 0 D");
            var ret = re.inverse(mat);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void lt3x3_inverse_WorksForSmallValues()
        {
            lt3x3 mat = lt3x3.Diagonal(0.0004f);
            mat.m22 = 0.00001f;
            
            var invI = re.inverse(mat);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
