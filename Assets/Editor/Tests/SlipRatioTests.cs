using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    internal class SlipRatioTests
    {
        [Test]
        public void StationaryWheelAndVehicle_ResultsZeroSlipRatio()
        {
            float slipRatio = Utils.ComputeSlipRatio(0f, 0f);

            Assert.AreEqual(slipRatio, 0f);
        }

        [Test]
        public void MatchingGroundAndWheelSpeed_ResultsZeroSlipRatio()
        {
            float slipRatio = Utils.ComputeSlipRatio(15f, 15f);

            Assert.AreEqual(slipRatio, 0f);
        }

        [Test]
        public void WheelSpinsFasterThanGround_ResultsPositiveSlipRatio()
        {
            float slipRatio = Utils.ComputeSlipRatio(15f, 10f);

            Assert.Greater(slipRatio, 0f);
        }

        [Test]
        public void WheelSpinsSlowerThanGround_ResultsNegativeSlipRatio()
        {
            float slipRatio = Utils.ComputeSlipRatio(10f, 15f);

            Assert.Less(slipRatio, 0f);
        }

        [Test]
        public void SLipRatio_IsClampedBetweenMinusOneAndOne()
        {
            float slipRatio = Utils.ComputeSlipRatio(10f, 0f, 0.5f);

            Assert.LessOrEqual(slipRatio, 1f);
            Assert.GreaterOrEqual(slipRatio, -1f);
        }
    }
}