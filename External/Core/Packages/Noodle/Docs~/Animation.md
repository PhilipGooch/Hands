# Animation

Carryable implement GetCarryPose that returns:

* Torso pose

* Hand poses

* Weights (relative to Idle, or Idle/Grab) for torso and arms

State groups use combined weights, e.g. arms could use blend between 3 meta-states:

* Idle

* Reach/carry (uses carry poses)

* Climb/swing



# Animation

Animation uses several layers:

* **Core layer** directly selects animation based on main noodle state and uses aim pitch to find position in that animation to generate pose, carryables override core pose for certain states (jump, normal, *maybe fall* )

* **Locomotion layer** implements walk cycles, it blends many animations based on walk speed, direction, main state (e.g. jumping uses air kick),

* **Hand overrides** are used when hands don't match core pose, e.g. idle arm in reach or carry, or second carryable carried off-hand.

### Body parts

Torso and head use the following blending:

* Use locomotion animation as base,

* Blend core layer (set muscles, add pose)

Legs

* Use locomotion

Hands

* Locomotion if idle, but not overriden by carryable

* Core if not idle and matches main state

* Off-hand if does not match main (e.g. differenct carryable)

### Handfull (engaged) states

Pose transitions between engaged and disengaged states should be fast, transitions within engaged states can be slower. Engaged states are:

* Grab

* Hold (also carryables), what is "hold" animation depends on context

* Climb

* Swing

* *possibly add swing/reach animation*

Disengaged states:

* Idle

* Slide

* Jump

* Fall

* Freefall

* Wake, etc












