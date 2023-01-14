### Conversion

To2D/To3D - converts between XZ float3 and XY float3

To3D/To4D - converts between XYZ float3 and XYZ float4

### Interpolation

InverseLerp - implements Mathf.InverseLerp 

### Projection and Intersection

Project - projects vector to axis.

ProjectOnPlane -   projects vector to plane defined by normal.

ProjectPointOnSegment - finds closest point on line segment

OrthoNormalize - makes 3 basis vectors orthonormal

### Plane Coordinates

Plane coordinates are used to "anchor" a point to a triangle, after that triangle is moved, a world point can be recreated that is preserves relative location. Useful for to hook points to chest triangle in IK.    

GetPlaneTransform - finds RigidTransform, where YZ plane goes through 3 world points

ToPlaneCoords - gets plane coordinates of a world point based on 3 world points

FromPlaneCoords - reconstruct world point from plane coordinates

### Move To

MoveTorwards - moves one vector (1-3d) to another by maximum delta

MoveTowardsExp - exponentially approach target vector, expressed as half-life (time to cover half of the distance) and timestep dt

Clamp - clamps magnitude of a vector

SoftClamp - limits magnitude of a vector which is greater than limit, making sure it never exceeds 2*limit

## Angular Methods

### Angles

NormalizeAngle - wraps angle to [-PI, +PI] range

SolveTriangleAngle - use cosine rule to find angle c given edges a,b,c

SolveTriangleEdge - use consine rule to find edge c given angle C and edges a,b



AngleBetween - returns angle between float3 vectors

SignedAngleBetween - returns from to rotation around axis

### Angle Axis vs Quaternion

ToAngleAxis - converts quaternion to float3 angle*axis representation

ToQuaternion - converts float3 angle axis representation back to quaternion

### Rotation

Rotate,RotateCW90,RotateCCW90 - rotates planar float2 vector

Rotate <X|Y|Z> - rotates float3 around primary axis by given angle 

Rotate <X|Y|Z> <CW90|CCW90> rotates float3 around primary axis by right angle



