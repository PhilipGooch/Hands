# Carryables

Examples for all carryables at their latest state is always located in Core/CoreSample/Assets/Samples/Noodle/Proto.unity. If something does not work in your project, check corresponding carryable in CoreSample project.

Carryables should not exceed 15-20kg, and single handed ones should preferably be under 10kg. Carryables are operated by character wrists, so designing them keep values in realistic ranges - if you could not handle comfortably an object of some mass and size, character won't be able to do that either. Character can still push and pull heavier objects, but it should not be carryable. Crate is the only carryable type that works reasonably well for masses up to 50kg. 

### Alignment

![gun](resources/gun.png)

Objects are carried with Z looking forward and X looking right. Some carryables ignore twist around Z axis (e.g. sticks) and will preserve orientation around twist axis as it was at the moment of grab. Others don't align objects at all, and maintain orientation or object relative to character or camera space as it was during the grab (e.g. crate).

If carryable gameobject is not properly, AlignTransform may be set to a child that has correct orientation.

## Core Carryables

Carryables in this category should cover 95% of use-cases and provide well balanced and understandable dynamics to players. Try making puzzles based on core carryables and only if they don't work go to more specialized categories.

### Stone

Stone carryable is used for small, throwable objects. Stone will be oriented so that it's center of mass is placed under the characters palm.

### Crate

Crate is two handed carryable, designed to be manipulated to construct structures - it can be used for bigger bricks, crates, planks etc. If an object is big enough to be carried with two hands and within reasonable mass, most probably it's a crate.

### Stick

Stick is a short stick, designed to flail around without precise aiming. Stick should be oriented along Z-Axis. Define single grip marker with either GripAxis or GripPoint at the ent of the stick to be grabbed.

### Axe

Axe is yet another stick, but more suitable to axe, pickaxe or hammer action. The shaft goes along Z-Axis, and striking edge is supported on both positive and negative Y-Axis. 

Axe supports two handed carrying: first GripPoint defines location of front hand (it's also used in single handed carrying), place one more GripPoint behind the first one for second arm.

## Special One Handed Carryables 

### Gun

Gun provides a stable carryable suitable for aiming, it will be locked to aiming on all axes with Z looking forward and X pointing right. Place GripAxis or GripPoint to define gun handle. 

### Tool

Tool is similar to gun - it is locked on all axes with Z pointing forward, but keep forward axis parallel to the ground without aiming up or down too much. It will move also move in planar trajectory, so it is well suited for tools that operate on walls (wall cutters, blowtorches, etc). Define handle by placing GripPoint.

### Torch

Torch is similar to stick, but is designed to carry with it's Z-Axis upright. Can be used to implement umbrella, flag, etc. 

### Coin

Coin is a small, lightweight carryable that will orient it's X left or right and will be carried with it's center of mass in front of the player. CircleGrip is designed to allow grab coins by their edge.  

### Shuriken

Shuriken is similar to coin but is used for objects that are bit larger and need swing type of action (shuriken, boomerang, throwable knife). Like the coin shuriken will align X axis to point either left or right, and axis running from grip point to center of mass will be used to align along the carrying axis (similar to Z in stick). Grip points must be placed at possible grab locations.    

### Shield

Shield is overly specific carryable designed for shields - Z axis will point forward and X will point perpendicular to forearm when being carried.

## Special Two Handed Carryables 

### Pole

Pole is long, two handed stick. It will align Z-Axis so that center of mass is forward from the grip, aiming the base of the direction of pole when carried by the tip. Typical setup is having single Axis Grip running full length of the pole, when grabbing it will space hands by comfortable distance automatically.

### Jackhammer

Jackhammer uses two point grips to determine which way it was picked up (which handle becomes left and which right). It will align Z-Axis to aim the operating tip. 

### Microchip

Microchip should have two grips at it's sides. Just like Jackhammer it will detect which one becomes left and right based on grab, but aims down instead of forward - to allow inserting into upright slots.

