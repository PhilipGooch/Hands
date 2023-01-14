using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleTreePhysics : ITreePhysics
{
    public bool Active { get; set; } = true;
    List<ReBody> bodies;

    public SimpleTreePhysics(List<ReBody> bodies)
    {
        this.bodies = bodies;
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        foreach (var body in bodies)
        {
            if (body.BodyExists)
            {
                Dynamics.AddAccelerationAtPosition(body, acc, worldPos);
            }
        }
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        CalculateTreeMoments(worldPos, out var i, out var m);
        var r = i.centerOfMass / i.mass;
        CalculateTreeMoments(worldPos + r, out i, out m);
        return new PluckerTranslate(-r).TransformVelocity(i.inverse * m);
    }

    void CalculateTreeMoments(Vector3 worldPos, out Inertia I, out Vector6 M)
    {
        I = Inertia.zero;
        M = Vector6.zero;

        foreach (var body in bodies)
        {
            if (body.BodyExists)
            {
                CalculateTreeMomentForBody(body, worldPos, out var i, out var m);
                I += i;
                M += m;
            }
        }
    }

    void CalculateTreeMomentForBody(ReBody body, Vector3 worldPos, out Inertia I, out Vector6 M)
    {
        I = Inertia.FromRigidAtPoint(body, worldPos);
        var v = new Vector6(body.angularVelocity, body.GetPointVelocity(worldPos));
        M = I * v;
    }

    public bool TreePhysicsActive()
    {
        return Active;
    }
}
