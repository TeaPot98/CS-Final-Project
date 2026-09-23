using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class SlipAngleTests
    {
        private TireSO InitializeTire()
        {
            return ScriptableObject.CreateInstance<TireSO>();
        }

        [Test]
        public void ZeroLoad_GivesZeroLateralForce()
        {
            const float slipAngle = 15f;
            const float tireLoad = 0f;
            TireSO tire = InitializeTire();

            float lateralForce = Utils.ComputeLateralPacejkaMagicFormula(slipAngle, tireLoad, 0f,
                tire.GetPacejkaLateralMagicFormulaParams());

            Assert.AreEqual(0f, lateralForce);
        }

        [Test]
        public void OutputValues_AreFiniteWithinTheSupportedRange()
        {
            const float largeSlipAngle = 90f;
            const float smallSlipAngle = 5f;
            const float zeroSlipAngle = 0f;
            const float tireLoad = 3f;

            TireSO tire = InitializeTire();

            float largeSlipAngleLateralForce = Utils.ComputeLateralPacejkaMagicFormula(largeSlipAngle, tireLoad, 0f,
                tire.GetPacejkaLateralMagicFormulaParams());
            float smallSlipAngleLateralForce = Utils.ComputeLateralPacejkaMagicFormula(smallSlipAngle, tireLoad, 0f,
                tire.GetPacejkaLateralMagicFormulaParams());
            float zeroSlipAngleLateralForce = Utils.ComputeLateralPacejkaMagicFormula(zeroSlipAngle, tireLoad, 0f,
                tire.GetPacejkaLateralMagicFormulaParams());

            Assert.Less(largeSlipAngleLateralForce, 5000f);
            Assert.Greater(largeSlipAngleLateralForce, -5000f);

            Assert.Less(smallSlipAngleLateralForce, 5000f);
            Assert.Greater(smallSlipAngleLateralForce, -5000f);

            Assert.Less(zeroSlipAngleLateralForce, 5000f);
            Assert.Greater(zeroSlipAngleLateralForce, -5000f);
        }

        [Test]
        public void OutputForce_IncreasesInitiallyWithSlip()
        {
            List<float> slipAngles = new() { 1f, 2f, 3f };
            const float tireLoad = 3f;

            TireSO tire = InitializeTire();

            float prevForce = 0f;

            for (int i = 0; i < slipAngles.Count; i++)
            {
                float lateralForce = Utils.ComputeLateralPacejkaMagicFormula(slipAngles[i], tireLoad, 0f,
                    tire.GetPacejkaLateralMagicFormulaParams());

                Assert.Greater(lateralForce, prevForce);
                prevForce = lateralForce;
            }
        }

        [Test]
        public void OutputForce_DependsOnTireLoad()
        {
            const float slipAngle = 15f;
            List<float> tireLoads = new() { 1f, 4f, 7f };
            TireSO tire = InitializeTire();

            float prevForce = 0f;

            for (int i = 0; i < tireLoads.Count; i++)
            {
                float lateralForce = Utils.ComputeLateralPacejkaMagicFormula(slipAngle, tireLoads[i], 0f,
                    tire.GetPacejkaLateralMagicFormulaParams());

                Assert.Greater(lateralForce, prevForce);
                prevForce = lateralForce;
            }
        }
    }
}