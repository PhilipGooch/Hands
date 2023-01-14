using UnityEngine;

namespace NBG.Core
{
    /// <summary>
    /// Proportional-integral-derivative controller
    /// https://en.wikipedia.org/wiki/PID_controller
    ///
    /// Feedback based control where input only indirectly influences the controlled value.
    /// Example 1:
    ///     Oven temperature is a side-effect of the heating element being active.
    ///     Difference between the desired temperature and the current temperature is the error.
    ///     Output of the PID controller is the amount of heating the heating element should do.
    ///     Clamp to [0; 1] to emulate min-max control models.
    ///
    /// Example 2:
    ///     Car accelerator controls the speed of the motor, which results in some amount of torque and indirectly influences the final speed.
    ///     Difference between the desired speed and the current speed is the error.
    ///     Output of the PID controller is the amount the accelerator should be open to reach the desired speed.
    ///     Clamp to [0; 1] to emulate a physical pedal (i.e. closed to open).
    /// </summary>
    [System.Serializable]
    public class PIDController
    {
        [Tooltip("Proportional constant (counters current error)")]
        public float Kp = 2.0f;

        [Tooltip("Integral constant (counters cumulated error)")]
        public float Ki = 0.2f;

        [Tooltip("Derivative constant (fights oscillation)")]
        public float Kd = 0.05f;

        [Space]
        public bool Clamp = false;
        public float ClampMin = 0.0f;
        public float ClampMax = 1.0f;

        float _lastError;
        float _integral;

        float _lastOutput;

        public float DebugLastError => _lastError;
        public float DebugLastOutput => _lastOutput;

        /// <summary>
        /// Calculate the new control value, based on the current error, which was last updated dt seconds ago.
        /// </summary>
        /// <param name="error">Difference between current and desired values.</param>
        /// <param name="dt">Time step.</param>
        /// <returns>New control value.</returns>
        public float Update(float error, float dt)
        {
            float derivative = (error - _lastError) / dt;
            _lastError = error;

            _integral += error * dt;
            if (Clamp)
                _integral = Mathf.Clamp(_integral, ClampMin, ClampMax);

            var value = (Kp * error + Ki * _integral + Kd * derivative);
            if (Clamp)
                value = Mathf.Clamp(value, ClampMin, ClampMax);
            _lastOutput = value;
            return value;
        }

        public void Reset()
        {
            _lastError = 0;
            _integral = 0;
            _lastOutput = 0;
        }
    }
}
