using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.VehicleSystem
{
    class PhysicalWheelHubAssembly : IPhysicalWheelHubAssembly
    {
        //TODO@OPTIONAL: here we could also attach a visible and physical wheel hub rigidbody so that players could see where wheels can be attached. TBD!

        ConfigurableJoint _configurableJoint;
        Vector3 _relativeStartSuspensionPosition;
        HingeJoint _hingeJoint;

        IPhysicalWheelHubAttachment _attachment;

        IPhysicalChassis _chassis;
        PhysicalAxleSettings _settings;

        #region IWheelHubAssembly
        public IWheelHubAttachment Attachment => _attachment;

        public void TransmitPower(float speedRads, float torqueNm)
        {
            if (_hingeJoint == null)
            {
                return;
            }

            var motor = _hingeJoint.motor;

            float shaftSpeed = speedRads * Mathf.Rad2Deg;
            float maxWheelSpeed = Mathf.Sign(speedRads) * _chassis.MaxSpeed * 180 / (Mathf.PI * (_attachment != null ? _attachment.Radius : 1));
            //Don't work with dynamic targetVelocities. Expecially in hills. Relies on iteration count and we cant increase it too much because of performance
            motor.targetVelocity = Mathf.Abs(shaftSpeed) > Mathf.Abs(maxWheelSpeed) ? shaftSpeed : maxWheelSpeed;
            motor.freeSpin = true;
            motor.force = torqueNm;

            _hingeJoint.motor = motor;
        }

        public void TransmitBrakes(float brakesValue)
        {
            if (_hingeJoint == null)
            {
                return;
            }

            var motor = _hingeJoint.motor;
            var brakeForce = brakesValue * _settings.MaxBrakeTorqueNm;
            if (motor.force > brakeForce && motor.freeSpin)
            {
                motor.force -= brakeForce;
                motor.freeSpin = true;
            }
            else
            {
                motor.targetVelocity = 0;
                motor.force = brakeForce;
                motor.freeSpin = false;
            }
            _hingeJoint.motor = motor;
        }

        public void TransmitSteering(float steeringAngleDegrees)
        {
            if (_configurableJoint == null)
            {
                return;
            }

            _configurableJoint.targetRotation = Quaternion.Euler(0, steeringAngleDegrees, 0);
        }

        public float CurrentLoad
        {
            get
            {
                if (_settings.SuspensionSettings == null)
                {
                    return 1;
                }
                var relativeCurrentSuspentionPosition = _configurableJoint.connectedBody.transform.InverseTransformPoint(_configurableJoint.transform.position);
                var angle = Vector3.Angle(_relativeStartSuspensionPosition, relativeCurrentSuspentionPosition);
                var a = Vector3.Distance(_relativeStartSuspensionPosition, Vector3.zero);
                var b = Vector3.Distance(relativeCurrentSuspentionPosition, Vector3.zero);
                var c = Mathf.Sqrt(a * a + b * b - 2 * a * b * Mathf.Cos(angle * Mathf.Deg2Rad));

                var currentLength = c;
                if (relativeCurrentSuspentionPosition.y > _relativeStartSuspensionPosition.y)
                {
                    currentLength = 0;
                }
                var t = Mathf.InverseLerp(0, _settings.SuspensionSettings.SuspensionLength, currentLength);

                float load = Mathf.Clamp01(1 - t);
                return load;
            }
        }

        public float CurrentSpeedRads
        {
            get
            {
                if (_hubReBody != null)
                {
                    return _hubReBody.angularVelocity.x * Mathf.Deg2Rad;
                }
                return 0;
            }
        } 

        public float CurrentXAngle
        {
            get
            {
                if (_attachment != null && _attachment.Rigidbody != null)
                {
                    _attachment.Rigidbody.transform.localRotation.ToAngleAxis(out var angle, out var axis);
                    return Mathf.Sign(axis.x) * angle;
                }
                return 0;
            }
        }

        public float CurrentYAngle
        {
            get
            {
                if (_hubRigidbody != null)
                {
                    return -_configurableJoint.targetRotation.eulerAngles.y;
                }
                return 0;
            }
        }
        #endregion


        private Transform _hubTransform;
        public Transform HubTransform { get => _hubTransform; }
        private Rigidbody _hubRigidbody;
        private ReBody _hubReBody;

        public PhysicalWheelHubAssembly(IPhysicalChassis chassis, PhysicalAxleSettings settings, Vector3 globalPosition, Quaternion globalRotation, int hubIndex)
        {
            _chassis = chassis;
            _settings = settings;

            _hubTransform = settings.HubsWithWheels[hubIndex].Hub.transform;
            _hubRigidbody = _hubTransform.GetComponent<Rigidbody>();
            if (_hubRigidbody == null)
            {
                _hubTransform.position = globalPosition;
                _hubTransform.rotation = globalRotation;
            }
            else
            {
                _hubReBody = new ReBody(_hubRigidbody);
                var bodyID = ManagedWorld.main.FindBody(_hubRigidbody);
                ManagedWorld.main.SetBodyPlacementImmediate(bodyID, new RigidTransform(globalRotation, globalPosition));
            }
        
            if (settings.IsSteerable || settings.SuspensionSettings != null)
            {
                _configurableJoint = CreateConfigurableJoint(_hubTransform, chassis.Rigidbody);
                if (settings.IsSteerable)
                    SetupSteering(_configurableJoint);
                if (settings.SuspensionSettings != null)
                    SetupSuspension(_configurableJoint, chassis);
            }

            if (settings.HubsWithWheels[hubIndex].Wheel != null)
            {
                var attachement = settings.HubsWithWheels[hubIndex].Wheel.GetComponent<IPhysicalWheelHubAttachment>();
                if (attachement != null)
                    Attach(attachement);
            }
        }

        public void Attach(IPhysicalWheelHubAttachment attachment)
        {
            this._attachment = attachment;

            var rb = new ReBody(attachment.Rigidbody);
            rb.SetBodyPlacementImmediate(_hubTransform.position, _hubTransform.rotation);

            _hingeJoint = SetupHingeJoint(attachment.Rigidbody, _hubRigidbody != null ? _hubRigidbody : _chassis.Rigidbody);

            attachment.Rigidbody.maxAngularVelocity = _chassis.MaxSpeed * 180 * Mathf.Deg2Rad / (Mathf.PI * attachment.Radius);
            attachment.Rigidbody.solverVelocityIterations = 10;
            ManagedWorld.main.ResyncPhysXBody(attachment.Rigidbody);

            attachment?.OnAttach(_chassis, this);
        }

        public void Detach()
        {
            if (_hingeJoint != null)
            {
                UnityEngine.Object.Destroy(_hingeJoint);
                _hingeJoint = null;
            }

            if (_attachment != null)
            {
                _attachment.OnDetach();
                _attachment = null;
            }
        }

        private ConfigurableJoint CreateConfigurableJoint(Transform transform, Rigidbody attachToRigidbody)
        {
            var joint = transform.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = attachToRigidbody;

            joint.axis = new Vector3(1, 0, 0);
            joint.secondaryAxis = new Vector3(0, 1, 0);
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            return joint;
        }

        private void SetupSuspension(ConfigurableJoint joint, IPhysicalChassis chassis)
        {
            joint.yMotion = ConfigurableJointMotion.Limited;
            var limit = joint.linearLimit;
            limit.limit = _settings.SuspensionSettings.SuspensionLength;
            joint.linearLimit = limit;

            joint.targetPosition = new Vector3(0, _settings.SuspensionSettings.SuspensionLength, 0);

            var yDrive = joint.yDrive;
            yDrive.positionSpring = _settings.SuspensionSettings.SpringForce;
            yDrive.positionDamper = _settings.SuspensionSettings.SpringDampening;
            joint.yDrive = yDrive;

            var globalPosition = joint.transform.TransformPoint(Vector3.zero);
            _relativeStartSuspensionPosition = chassis.Rigidbody.transform.InverseTransformPoint(globalPosition);
        }

        private void SetupSteering(ConfigurableJoint joint)
        {
            joint.angularYMotion = ConfigurableJointMotion.Free;

            var jointDrive = joint.angularYZDrive;
            jointDrive.positionSpring = float.MaxValue;
            jointDrive.positionDamper = 0;
            joint.angularYZDrive = jointDrive;
        }

        private HingeJoint SetupHingeJoint(Rigidbody jointRigidbody, Rigidbody attachToRigidbody)
        {
            var joint = jointRigidbody.gameObject.AddComponent<HingeJoint>();

            joint.connectedBody = attachToRigidbody;
            joint.axis = new Vector3(1, 0, 0);

            joint.useMotor = true;
            var jointMotor = joint.motor;
            jointMotor.freeSpin = true;
            joint.motor = jointMotor;

            return joint;
        }
    }

}
