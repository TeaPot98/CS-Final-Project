using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    internal class TransmissionTests
    {
        private TransmissionSO InitializeTransmission()
        {
            return ScriptableObject.CreateInstance<TransmissionSO>();
        }

        private EngineSO InitializeEngine()
        {
            return ScriptableObject.CreateInstance<EngineSO>();
        }

        [Test]
        public void NeutralGear_ProducesZeroOutput()
        {
            TransmissionSO transmission = InitializeTransmission();

            float resultingTorque = Utils.ComputeTransmissionTorque(500f, 0, transmission);

            Assert.AreEqual(resultingTorque, 0f);
        }

        [Test]
        public void ReverseGear_ProducesNegativeOutput()
        {
            TransmissionSO transmission = InitializeTransmission();

            float resultingTorque = Utils.ComputeTransmissionTorque(500f, -1, transmission);

            Assert.Less(resultingTorque, 0f);
        }

        [Test]
        public void OutputTorque_IncludesEfficiencyAndDifferential()
        {
            const float inputTorque = 100f;
            const int gear = 1;

            TransmissionSO transmission = InitializeTransmission();

            float resultingTorque = Utils.ComputeTransmissionTorque(inputTorque, gear, transmission);

            Assert.AreEqual(71.75f, resultingTorque);
        }

        [Test]
        public void HigherGears_ProduceLessOutputTorqueForTheSameEngineTorque()
        {
            const float inputTorque = 100f;
            float prevTorque = Mathf.Infinity;

            TransmissionSO transmission = InitializeTransmission();

            for (int gear = 1; gear <= transmission.gears.Count; gear++)
            {
                float resultingTorque = Utils.ComputeTransmissionTorque(inputTorque, gear, transmission);

                Assert.Less(resultingTorque, prevTorque);

                prevTorque = resultingTorque;
            }
        }

        [Test]
        public void SpeedAtMaxRpm_IncreasesWithHigherGears()
        {
            const float wheelRadius = 0.3f;

            EngineSO engine = InitializeEngine();
            TransmissionSO transmission = InitializeTransmission();

            List<(float, float)> speedIntervals = Utils.GetGearsSpeedIntervals(engine, transmission, wheelRadius);

            float prevMaxSpeed = speedIntervals[0].Item2;

            for (int gearIndex = 1; gearIndex < speedIntervals.Count; gearIndex++)
            {
                Assert.Greater(speedIntervals[gearIndex].Item2, prevMaxSpeed);

                prevMaxSpeed = speedIntervals[gearIndex].Item2;
            }
        }
    }
}