# Coordinate System

For better physics performance recoil uses body position at center of mass. Body stores it's RigidTransform in x field. Recoil body position will match unity Transform position only when Rigidbody.centerOfMass is zero. BodyPosition methods operate in body space, TransformPosition methods remap to unity frame of reference. World exposes the following methods:

* GetBodyPosition -  returns body center of mass transform, 
* SetBodyPosition - writes body center of mass position transform. Unity bodies will be synced after game step,
* IntegrateBodyPosition - integrates body center of mass motion using current velocity and specified dt,
* GetTransformPosition (bodyId) - returns RigidTransform of body mapped to unity transfrom coordinates,
* SetTransfromPosition(bodyId, RigidTransform) - moves body in recoil world to match transform position. Unity bodies will be synced after game step,
* IntegrateTransformPosition - integrated transform motion using current velocity and specified dt, used to interpolate rigidbody positions for lateupdate.

