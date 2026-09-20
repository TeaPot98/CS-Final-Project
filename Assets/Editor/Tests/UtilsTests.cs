using GLTFast.Schema;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class UtilsTests
    {
        [Test]
        public void EngineRpm_IsCorrectlySynced()
        {
            float currentEngineRpm = 1000f;
            float prevWheelAngularVelocity = 0f;
            int gear = 1;
            float throttle = 0f;

            TransmissionSO transmission = ScriptableObject.CreateInstance<TransmissionSO>();
            EngineSO engine = ScriptableObject.CreateInstance<EngineSO>();

            float maxEngineTorque = engine.GetTorque(currentEngineRpm);


            float expectedEngineRpm =
                Utils.ComputeExpectedRpmAtWheelAngularVelocity(prevWheelAngularVelocity, transmission,
                    gear);

            Debug.Log(expectedEngineRpm);

            currentEngineRpm = Utils.ComputeEngineDeltaRpm(.5f, maxEngineTorque, throttle, 0.01f, 0.01f,
                expectedEngineRpm,
                currentEngineRpm);
            currentEngineRpm = Mathf.Clamp(currentEngineRpm, engine.idleRpm, engine.maxRpm);

            Assert.AreEqual(expectedEngineRpm, currentEngineRpm);
        }

        [Test]
        public void WheelAngularVelocity_IsCorrectlySynced()
        {
            float currentEngineRpm = 1000f;
            float currentAngularVelocity = 0f;
            int gear = 1;
            float throttle = 1f;
            float brake = 0f;

            TransmissionSO transmission = ScriptableObject.CreateInstance<TransmissionSO>();
            EngineSO engine = ScriptableObject.CreateInstance<EngineSO>();

            float maxEngineTorque = engine.GetTorque(currentEngineRpm);


            float expectedWheelAngularVelocity =
                Utils.ComputeWheelAngularVelocityAtEngineRpm(currentEngineRpm, gear, transmission);

            Debug.Log(expectedWheelAngularVelocity);

            currentAngularVelocity = Utils.ComputeWheelDeltaAngularVelocity(1f, maxEngineTorque, throttle, 2f,
                .8f, brake,
                expectedWheelAngularVelocity, currentAngularVelocity);

            Assert.AreEqual(currentAngularVelocity, expectedWheelAngularVelocity);
        }
    }
}