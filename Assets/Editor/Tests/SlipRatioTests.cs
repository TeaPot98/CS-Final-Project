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

            Assert.AreEqual(0f, slipRatio);
        }

        [Test]
        public void MatchingGroundAndWheelSpeed_ResultsZeroSlipRatio()
        {
            float slipRatio = Utils.ComputeSlipRatio(15f, 15f);

            Assert.AreEqual(0f, slipRatio);
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

        [TestCase(10f, 0f, 1f)]
        [TestCase(0f, 10f, -1f)]
        public void SLipRatio_IsClampedBetweenMinusOneAndOne(float surfaceSpeed, float groundSpeed,
            float expectedSlipRatio)
        {
            float slipRatio = Utils.ComputeSlipRatio(surfaceSpeed, groundSpeed);

            Assert.That(slipRatio, Is.EqualTo(expectedSlipRatio).Within(0.001f));
        }
    }
}