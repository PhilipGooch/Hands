# NoodlePose

NoodlePose describes a target for physics based animation. It consists or two parts - kinematics described by poses and dynamics defined by muscles. 

## Muscles

Muscles are stored together with pose of the object that they are controlling. Muscle tonus defines how strong certain muscle is, there are following fields in **NoodlePose** hierarchy for controlling muscle strength:

* **suspensionTonus** controls hips height - y root position,
* **angularTonus** is used to hold hips or shoulder orientation depending on jump/climb states,
* **torso.tonus** bending torso
* **head.tonus** head orientation
* **[handL|handR].muscle.tonus** hand muscle strength
* **[handL|handR].muscle.ikDrive** when 1 hand is trying to reach final target position using linear trajectory instead of interpolating angles
* **[handL|handR].muscle.extraSteady** used only by certain carryables that need more damping, should not be used otherwise
* **legR/legR.tonus** leg muscles

## Pose Root Space

Root space origin is projection of character CoM to ground plane. Root space has rotation around Y axis corresponding to camera yaw.

## Torso/Head Poses

Torso and head is defined by:

* **hipsHeight** - y position of hips relative to root

* **torso.hips***, **torso.waist***, **torso.chest*** - rotations of torso joints relative to root orientation expressed as pitch, yaw and roll 

* **head.pitch**, **head.yaw**, **head.roll** - rotation of head relative to root orientation


## Hand Pose

Hand has 5 DOF. All values are for right hand, left hand pose with same values is a mirror copy or right hand:

* **pitch, yaw** - direction of fist when looking from shoulder, 0,0 corresponds to hand fully forward
* **bend** - bend angle at elbow
* **elbowAngle** - arm twist. 0 corresponds to elbow looking out (right), positive values bring elbow down, negative - up
* **wristAngle** - 0 wrist angle holds hand flat, with the the palm looking down, positive values raise thumb up, negative values point it down

## LegPose

Leg has 4 degrees of freedom, just like with arms, left leg pose is a mirror version of right leg:

* **pitch**, **stretch** -  direction from hip to foot. 0,0 corresponds to leg pointing down, positive stretch brings leg out
* **bend** - angle at knee
* **twist** - 0 twist point the knee forward, positive - knee out, negative - knee in

