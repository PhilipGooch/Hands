# Locomotion

The locomotion system is meant to handle movement on surfaces as well as steering behaviours. 
It is split into several component concepts - locomotion agent, locomotion data and locomotion handlers.

## BallLocomotion
The locomotion package provides the BallLocomotion agent which is moved around with a rigid sphere. It can jump and stand still on sloped terrain.
It has a facing direction which can be used to orient the agent visuals.
It comes with its own set of jobs and a BallLocomotionSystem that makes batching multiple agents simple.

Its main APIs are:
- **SetInput**: this is used to provide the wanted movement direction and to tell the agent to jump on command.
This should be called before updating the locomotion system.
- **SetRotation** this is used to make the agent face in a certain direction.
Regular transform rotation operations do not change the agent orientation, therefore this API is needed if you want to make an agent look in a certain direction when respawning or similar.

## BallLocomotionSystem
This is the system that handles multiple agents and the locomotion handler behaviours. Its main APIs are:
- **AddLocomotionHandler**: this is used to add a specific ILocomotionHandler to the agent group.
This is where you can customize the behaviour of the agents and add things like obstacle avoidance or flocking.
- **UpdateLocomotion**: this is used to execute the locomotion system logic. Call this in fixed update, after setting the inputs for all the agents

## LocomotionData
This is the main information about the agent locomotion. It contains a lot of read-only information like the ground normals, agent sphere radius, jump height as well as read/write data.
The main data outputs are:
- **targetVelocity** set this to influence the direction in which the agent should mode or turn towards.
- **strafeVelocity** set this to influence the strafing movement for the agent. Useful if you want the agent to move immediatelly, without turning.
- **jump** set this to make the agent jump.

## ILocomotionHandler
This is an interface that can be implemented to handle any form of locomotion behaviours - obstacle avoidance, flocking, constant jumping, moving in circles.
It uses a NativeList of LocomotionData that represents all the agents and expects the NativeList data to be changed, according to the wanted locomotion behaviour
The locomotion package contains three ILocomotionHandlers:
- **Flocking** - booids-based flocking behaviours.
- **ObstacleAvoidance** - steering away from walls.
- **EdgeAvoidance** - steering and strafing away from edges.

# How do I set this up?
- Add a BallLocomotion monobehaviour on the agent. 
- Create some form of agent manager script that has a reference to the BallLocomotion scripts in scene.
- Inside the manager script, create a BallLocomotionSystem with all the agents.
- Add the wanted ILocomotionHandlers to the system.
- Set the agent inputs somewhere.
- Call UpdateLocomotion on the BallLocomotionSystem in fixed update, once all the agent inputs are set.

# Implementing custom locomotion
You can create your own agents with their own locomotion logic. 
Make sure that they can provide and also read LocomotionData so that they can be influenced by ILocomotionHandlers. 
You would also need to implement your own LocomotionSystem to update the agents in the right order.
