using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class DrivetrainTests
    {
        private TransmissionSO InitializeTransmission()
        {
            return ScriptableObject.CreateInstance<TransmissionSO>();
        }

        [Test]
        public void RpmToAngularVelocityAndBack_ReturnsOriginalValue()
        {
            const float expectedRpm = 4000f;

            float angularVelocity = Utils.FromRpmToAngularVelocity(expectedRpm);
            float resultingRpm = Utils.FromAngularVelocityToRpm(angularVelocity);

            Assert.AreEqual(expectedRpm, resultingRpm);
        }

        [Test]
        public void VehicleSpeedToWheelRpmAndBack_ReturnsOriginalValue()
        {
            const float wheelRadius = 0.3f;
            const float expectedSpeed = 20f;

            float wheelRpm = Utils.ComputeWheelRpmAtCarSpeed(expectedSpeed, wheelRadius);
            float resultingSpeed = Utils.ComputeCarSpeedAtWheelRpm(wheelRpm, wheelRadius);

            Assert.AreEqual(expectedSpeed, resultingSpeed);
        }

        [Test]
        public void EngineRpmToWheelRpmAndBack_ReturnsOriginalValue()
        {
            const float expectedEngineRpm = 4000f;
            const int gear = 1;

            TransmissionSO transmission = InitializeTransmission();

            float wheelRpm = Utils.ComputeWheelRpm(expectedEngineRpm, gear, transmission);
            float engineRpm = Utils.ComputeExpectedRpmAtWheelRpm(wheelRpm, transmission, gear);

            Assert.AreEqual(expectedEngineRpm, engineRpm);
        }
    }
}