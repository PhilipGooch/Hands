# Noodle Dynamics

**NoodleRig** represents dynamics of the character: applies joint targets and muscles based to Articulation based **NoodlePose**. NoodleRig uses **NoodleJoints** to construct **Articulation**. Controls Ball, Articulation and NoodleSuspension. Consists of NoodleArmRig, and NoodleLegRig

**Ball** implements locomotion, able to climb slopes and stairs, detects ground

**NoodleDimensions** stores measurements of the ragdoll, created by NoodleRig.

**NoodleSprings** settings for fully engaged muscle springs.

# Noodle Controller

**Noodle** implements character controller, using NoodleHand as sub-controllers for hands.

**NoodleHand** is controller for unity/physX related functionality of hand: it reacts to collisions, manages unity joints for carried objects, acquires targets to grab.

**Carry** sits between Noodle and NoodleRig to manage arms. It manages constraints between hands and carriables (Recoil not PhysX), provides poses for arms based on user input and carryables. 

**InputFrame** specifies user input.

**NoodleState** describes the state of character, including core state, variable for animations like moveYaw, moveMagnitude or blending between swing and climb. It is assembled by Noodle from InputFrame and feedback provided by subsystems like NoodleRig and NoodleHand. 

**NoodleAnimator** implements animation state machine capable of providing NoodlePose matching the state described by NoodleState.

**NoodleIK** implements full body IK based on FABRIK, it takes NoodlePose calculated by NoodleAnimator and applies IKTargets.

## Execution Flow

Execution is split in two stages - operations requiring access to PhysX are executed in main thread in FixedUpdate, the rest are done as part of NoodleExecute job.

Noodle.OnFixedUpdate

* hands are processed based on InputFrame:
  * NoodleArmRig calculates arm target,
  * NoodleHand scans for grab targets in scene, creates, destroys joints
* NoodleRig uses Ball to handle ground and calculates acceleration resulting from ground interaction and user input, 
* NoodleData is assembled to describe the situation (isGrounded, groundVelocity, IK targets, etc)

Noodle.Execute

* calculate NoodleState from InputFrame, Carry and NoodleRig information,
* NoodleAnimator calculates NoodlePose based on state,
* Carry overrides hands poses in NoodlePose based on hand  interaction,
* NoodleIK adjust pose to reach IK targets
* resulting pose (including muscle tonus) is applied to subsystems by NoodleRig
* if jumping, jump acceleration is added to desired ball acceleration
* finally velocity changes due to ball acceleration are applied to the rig 

## Carryables and grip markers

For setting up complex carryables or grip targets see:

* [Carryables](Carryables.md) - carryable introduction.
* [Grip markers](GripMarkers.md) - grip marker introduction.