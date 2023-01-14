using NBG.Entities;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Recoil
{
    public enum ArticulationJointType : int
    {
        Empty,
        Angular,
        Angular3,
        Linear,
        Linear3,
        CG,
        PreserveAngular,
        Fulcrum
    }

    public enum RotationTargetMode
    {
        SelfOffset,     // rotation in own frame, relative to rigged position:            rotation = parent.rotation * originalRelativeRotation * targetRotation
        ParentOffset,   // rotation in connected body frame, relative to rigged position: rotation = parent.rotation * targetRotation * originalRelativeRotation
        Relative,       // rotation relative to parent (like local rotation):             rotation = parent.rotation * targetRotation
        AbsolutePosRelativeVel,       // world rotation, but damper still on joint:       rotation = targetRotation
        Absolute,       // same as above but damper between body and the world - joint velocity target is -parentVel
        //AbsoluteNoFeedback,     // Not needed - works exactly as when parent =-1
    }

    public struct ArticulationJointArray : IDisposable
    {
        public UnsafeArray<ArticulationJoint> joints;


        public ArticulationJointArray(int nJoints, Allocator allocator)
        {
            joints = new UnsafeArray<ArticulationJoint>(nJoints, allocator);
        }

        public ArticulationJointArray(ArticulationJoint[] srcJoints, Allocator allocator) : this()
        {
            joints = new UnsafeArray<ArticulationJoint>(srcJoints, allocator);
        }

        public void Dispose()
        {
            joints.Dispose();
        }
        public void CheckType<T>(int index)
        {
            switch (joints[index].jointType)
            {
                case ArticulationJointType.Empty: throw new InvalidOperationException("Empty joint");
                case ArticulationJointType.Angular: Unsafe.CheckType<T, AngularArticulationJoint>(); break;
                case ArticulationJointType.Angular3: Unsafe.CheckType<T, Angular3ArticulationJoint>(); break;
                case ArticulationJointType.Linear: Unsafe.CheckType<T, LinearArticulationJoint>(); break;
                case ArticulationJointType.Linear3: Unsafe.CheckType<T, Linear3ArticulationJoint>(); break;
                case ArticulationJointType.CG: Unsafe.CheckType<T, CGArticulationJoint>(); break;
                case ArticulationJointType.PreserveAngular: Unsafe.CheckType<T, PreserveAngularArticulationJoint>(); break;
                case ArticulationJointType.Fulcrum: Unsafe.CheckType<T, FulcrumJoint>(); break;

            }
        }
        public unsafe ref ArticulationJoint GetJoint(int index)
        {
            Unsafe.CheckIndex(index, joints.Length);
            return ref joints.ElementAt(index);
        }
        public ArticulationJointType GetType(int index) 
        {
            Unsafe.CheckIndex(index, joints.Length);
            return joints[index].jointType;
        }
        public unsafe ref T GetJoint<T>(int index) where T:unmanaged
        {
            Unsafe.CheckIndex(index,joints.Length);
            CheckType<T>(index);
            return ref *(T*)joints.ElementAt(index).angular.AsPointer();
        }

    }


    [StructLayout(LayoutKind.Explicit)]
    public struct ArticulationJoint
    {
        [FieldOffset(0)] public ArticulationJointType jointType;
        [FieldOffset(4)] public int row;

        [FieldOffset(8)] public AngularArticulationJoint angular;
        [FieldOffset(8)] public Angular3ArticulationJoint angular3;
        [FieldOffset(8)] public LinearArticulationJoint linear;
        [FieldOffset(8)] public Linear3ArticulationJoint linear3;
        [FieldOffset(8)] public CGArticulationJoint cg;
        [FieldOffset(8)] public PreserveAngularArticulationJoint preserveAngular;
        [FieldOffset(8)] public FulcrumJoint fulcrum;

        public static unsafe ref ArticulationJoint FromSpecific(IntPtr ptrSpecific)
        {
            var offset = (int)((ArticulationJoint*)0)->angular.AsPointer();
            var ptr = (ArticulationJoint*)( ptrSpecific - offset);
            return ref *ptr;
        }
        public int rowCount => jointType switch
        {

            ArticulationJointType.Angular => 3,
            ArticulationJointType.Angular3 => 3,
            ArticulationJointType.Linear => 3,
            ArticulationJointType.Linear3 => 3,
            ArticulationJointType.CG => 3,
            ArticulationJointType.PreserveAngular => 3,
            ArticulationJointType.Fulcrum => 3,
            _ => 0
        };
        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            switch (jointType)
            {
                case ArticulationJointType.Angular: angular.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.Angular3: angular3.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.Linear: linear.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.Linear3: linear3.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.CG: cg.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.PreserveAngular: preserveAngular.FillJacobianSparcity(solver, startRow); break;
                case ArticulationJointType.Fulcrum: fulcrum.FillJacobianSparcity(solver, startRow); break;
            }
        }

        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            switch (jointType)
            {
                case ArticulationJointType.Angular: angular.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.Angular3: angular3.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.Linear: linear.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.Linear3: linear3.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.CG: cg.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.PreserveAngular: preserveAngular.FillJacobian(solver, context, startRow); break;
                case ArticulationJointType.Fulcrum: fulcrum.FillJacobian(solver, context, startRow); break;
            }
        }


        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            switch (jointType)
            {
                case ArticulationJointType.Angular: angular.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.Angular3: angular3.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.Linear: linear.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.Linear3: linear3.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.CG: cg.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.PreserveAngular: preserveAngular.CalculateErrors(solver, context, startRow); break;
                case ArticulationJointType.Fulcrum: fulcrum.CalculateErrors(solver, context, startRow); break;
            }
        }
        public override string ToString() => jointType.ToString();
    }

    public struct AngularArticulationJoint
    {
        public static ArticulationJoint Create(int link, int parent)
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.Angular,
                angular = new AngularArticulationJoint()
                {
                    connectedLink = parent,
                    link = link,
                    connectedRotation = quaternion.identity, 
                    targetRotation = quaternion.identity
                }
            };
        }

        public Spring spring;
        public int connectedLink;
        public int link;
        public quaternion connectedRotation;
        public quaternion targetRotation;
        public float3 targetVelocity;
        public RotationTargetMode rotationMode;

        public float relativeVelInfluence;
        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            if (connectedLink != -1)
                solver.MarkJacobian(startRow, 3, connectedLink, true, false);
            if (link != -1)
                solver.MarkJacobian(startRow, 3, link, true, false);
        }

        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var jMult = spring.isFree ? 0 : 1;
            if (connectedLink != -1)
                solver.WriteDiagonal(startRow + 0, connectedLink, 0, -1 * jMult);
            if (link != -1)
                solver.WriteDiagonal(startRow + 0, link, 0, 1 * jMult);
        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            JointErrors.CalculateAngularError(context, connectedLink, link, connectedRotation, targetRotation, targetVelocity, rotationMode, out var err, out var velErr, relativeVelInfluence);
            solver.WriteError(startRow, spring, err, velErr);
        }

    }
  
    public struct Angular3ArticulationJoint
    {
        public static ArticulationJoint Create(int link, int parent, float3 axisX, float3 axisY, float3 axisZ, RotationTargetMode mode = RotationTargetMode.SelfOffset)
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.Angular3,
                angular3 = new Angular3ArticulationJoint()
                {
                    rotationMode = mode,
                    connectedLink = parent,
                    link = link,
                    connectedRotation = quaternion.identity,
                    targetRotation = quaternion.identity,
                    axisX = axisX,
                    axisY = axisY,
                    axisZ = axisZ,
                    springY = Spring.free,
                    springZ = Spring.free
                }
            };
        }
        public Spring springX;
        public int connectedLink;
        public int link;
        public quaternion connectedRotation;
        public quaternion targetRotation;
        public float3 targetVelocity;
        public RotationTargetMode rotationMode;
        public float relativeVelInfluence;

        public Spring springY;
        public Spring springZ;
        public Spring spring { set { springX = springY = springZ = value; } }

        public float3 axisX;
        public float3 axisY;
        public float3 axisZ;
        private int _worldAxes;
        public bool worldAxes { get => _worldAxes != 0; set => _worldAxes = value ? 1 : 0; }

        float3 worldX;
        float3 worldY;
        float3 worldZ;


        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            if (connectedLink != -1)
                solver.MarkJacobian(startRow, 3, connectedLink, true, false);
            if (link != -1)
                solver.MarkJacobian(startRow, 3, link, true, false);
        }

        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            re.OrthoNormalize(ref axisX, ref axisY, ref axisZ);
            if (worldAxes)
            {
                worldX = axisX;
                worldY = axisY;
                worldZ = axisZ;
            }
            else
            {
                //var rot = quaternion.identity;
                var rot = link >= 0 ? context.GetBody(link).x.rot : quaternion.identity;

                worldX = math.rotate(rot, axisX);
                worldY = math.rotate(rot, axisY);
                worldZ = math.rotate(rot, axisZ);
            }
            var jMultX = springX.isFree ? 0 : 1;
            var jMultY = springY.isFree ? 0 : 1;
            var jMultZ = springZ.isFree ? 0 : 1;
            if (connectedLink != -1)
            {
                solver.WriteRow(startRow + 0, connectedLink, 0, -worldX * jMultX);
                solver.WriteRow(startRow + 1, connectedLink, 0, -worldY * jMultY);
                solver.WriteRow(startRow + 2, connectedLink, 0, -worldZ * jMultZ);
            }
            if (link != -1)
            {
                solver.WriteRow(startRow + 0, link, 0, worldX * jMultX);
                solver.WriteRow(startRow + 1, link, 0, worldY * jMultY);
                solver.WriteRow(startRow + 2, link, 0, worldZ * jMultZ);
            }
        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            JointErrors.CalculateAngularError(context, connectedLink, link, connectedRotation, targetRotation, targetVelocity, rotationMode, out var err, out var velErr, relativeVelInfluence);
            solver.WriteError(startRow + 0, springX, math.dot(err, worldX), math.dot(velErr, worldX));
            solver.WriteError(startRow + 1, springY, math.dot(err, worldY), math.dot(velErr, worldY));
            solver.WriteError(startRow + 2, springZ, math.dot(err, worldZ), math.dot(velErr, worldZ));
            //Debug.DrawRay(World.current.TransformPoint(linkB, anchorB), err, Color.green);
            //Debug.DrawRay(World.current.TransformPoint(linkB, anchorB), World.current.TransformDirection(linkB, re.right), Color.red);
            //Debug.DrawRay(World.current.TransformPoint(linkB, anchorB), World.current.TransformDirection(linkB, re.up), Color.green);
            //Debug.DrawRay(World.current.TransformPoint(linkB, anchorB), World.current.TransformDirection(linkB, re.forward), Color.blue);
        }

    
    }
    public struct LinearArticulationJoint
    {
        public Spring spring;
        public int connectedLink;
        public int link;
        public float3 connectedAnchor;
        public float3 anchor;
        public float3 targetPosition;
        public float3 targetVelocity;



        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            if (connectedLink != -1)
                solver.MarkJacobian(startRow, 3, connectedLink, true, true);
            if (link != -1)
                solver.MarkJacobian(startRow, 3, link, true, true);
        }
        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var jMult = spring.isFree ? 0 : 1;
            
            if (connectedLink != -1)
            {
                var rA = context.TransformDirection(connectedLink, connectedAnchor);
                solver.WriteMatrix(startRow + 0, connectedLink, 0, re.cross(rA * jMult));
                solver.WriteDiagonal(startRow + 0, connectedLink, 1, -1 * jMult);
            }
            if (link != -1)
            {
                var rB = context.TransformDirection(link, anchor);
                solver.WriteMatrix(startRow + 0, link, 0, -re.cross(rB * jMult));
                solver.WriteDiagonal(startRow + 0, link, 1, 1 * jMult);
            }
        }

        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            JointErrors.CalculateLinearError(context, connectedLink, connectedAnchor, link, anchor, targetPosition, targetVelocity, out var err, out var velErr);
            solver.WriteError(startRow, spring, err, velErr);
        }

    }


    public struct Linear3ArticulationJoint
    {
        public static ArticulationJoint Create(int link, int parent, bool feedbackAtConnectedAnchor=false) => Create(link, parent, re.right, re.up, re.forward, feedbackAtConnectedAnchor);
        public static ArticulationJoint Create(int link, int parent, float3 axisX, float3 axisY, float3 axisZ, bool feedbackAtConnectedAnchor = false)
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.Linear3,
                linear3 = new Linear3ArticulationJoint()
                {
                    connectedLink = parent,
                    link = link,
                    axisX = axisX,
                    axisY = axisY,
                    axisZ = axisZ,
                    feedbackAtConnectedAnchor= feedbackAtConnectedAnchor
                }
            };
        }

        public Spring springX;
        public int connectedLink;
        public int link;
        public float3 connectedAnchor;
        public float3 anchor;
        public float3 targetPosition;
        public float3 targetVelocity;

        public Spring springY;
        public Spring springZ;
        public float3 axisX;
        public float3 axisY;
        public float3 axisZ;
        private int _feedbackAtConnectedAnchor;
        public bool feedbackAtConnectedAnchor { get => _feedbackAtConnectedAnchor != 0; set => _feedbackAtConnectedAnchor = value ? 1 : 0; }

        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            if (connectedLink != -1)
                solver.MarkJacobian(startRow, 3, connectedLink, true, true);
            if (link != -1)
                solver.MarkJacobian(startRow, 3, link, true, true);
        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            JointErrors.CalculateLinearError(context, connectedLink, connectedAnchor, link, anchor, targetPosition, targetVelocity, out var err, out var velErr);
            solver.WriteError(startRow + 0, springX, math.dot(err, axisX), math.dot(velErr, axisX));
            solver.WriteError(startRow + 1, springY, math.dot(err, axisY), math.dot(velErr, axisY));
            solver.WriteError(startRow + 2, springZ, math.dot(err, axisZ), math.dot(velErr, axisZ));

        }

        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {

            re.OrthoNormalize(ref axisX, ref axisY, ref axisZ);
            var jMultX = springX.isFree ? 0 : 1;
            var jMultY = springY.isFree ? 0 : 1;
            var jMultZ = springZ.isFree ? 0 : 1;


            if (connectedLink != -1)
            {
                //feedbackAtConnectedAnchor = false;
                var rA = feedbackAtConnectedAnchor?
                    context.TransformDirection(connectedLink, connectedAnchor): // use connected anchor as force point
                    context.TransformPoint(link, anchor) - context.TransformPoint(connectedLink, float3.zero); // us anchor as force poin

                solver.WriteRow(startRow + 0, connectedLink, 0, math.cross(axisX, rA * jMultX));
                solver.WriteRow(startRow + 1, connectedLink, 0, math.cross(axisY, rA * jMultY));
                solver.WriteRow(startRow + 2, connectedLink, 0, math.cross(axisZ, rA * jMultZ));
                solver.WriteRow(startRow + 0, connectedLink, 1, -axisX * jMultX);
                solver.WriteRow(startRow + 1, connectedLink, 1, -axisY * jMultY);
                solver.WriteRow(startRow + 2, connectedLink, 1, -axisZ * jMultZ);
            }
            if (link != -1)
            {
                var rB = context.TransformDirection(link, anchor);
                solver.WriteRow(startRow + 0, link, 0, -math.cross(axisX, rB * jMultX));
                solver.WriteRow(startRow + 1, link, 0, -math.cross(axisY, rB * jMultY));
                solver.WriteRow(startRow + 2, link, 0, -math.cross(axisZ, rB * jMultZ));
                solver.WriteRow(startRow + 0, link, 1, axisX * jMultX);
                solver.WriteRow(startRow + 1, link, 1, axisY * jMultY);
                solver.WriteRow(startRow + 2, link, 1, axisZ * jMultZ);
            }
        }
    }

    public struct FulcrumJoint
    {
        const int linkCount= 4;
        public Spring springX;
        public int link;
        public float3 anchor;
        public int4 links;
        public float4 weights;
        public float3 anchor0;
        public float3 anchor1;
        public float3 anchor2;
        public float3 anchor3;
        public float3 targetPosition;
        float totalWeight;
        public Spring springY;
        public Spring springZ;

        public unsafe static ArticulationJoint Create(int link, float3 anchor, int link0, float3 anchor0, int link1, float3 anchor1, int link2=-1, float3 anchor2=default, int link3=-1, float3 anchor3=default)
        { 
            var res = new ArticulationJoint()
            {
                jointType = ArticulationJointType.Fulcrum,
                fulcrum = new FulcrumJoint()
                {
                    link = link,
                    anchor = anchor,
                }
            };
            var pLink = Unsafe .AsPointer(ref res.fulcrum.links.x);
            var pWeight = Unsafe.AsPointer(ref res.fulcrum.weights.x);
            var pAnchor = Unsafe.AsPointer(ref res.fulcrum.anchor0);
            *pLink++ = link0; *pAnchor++ = anchor0; *pWeight++ = 1;
            *pLink++ = link1; *pAnchor++ = anchor1; *pWeight++ = 1;
            *pLink++ = link2; *pAnchor++ = anchor2; *pWeight++ = 1;
            *pLink++ = link3; *pAnchor++ = anchor3; *pWeight++ = 1;
            return res;
        }

        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            var pLink = Unsafe.AsPointer(ref links.x);
            for (int i = 0; i < linkCount; i++)
                if(*pLink>=0)
                    solver.MarkJacobian(startRow + 0, 3, *pLink++, true, true);
            if (link != -1)
                solver.MarkJacobian(startRow + 0, 3, link, true, true);
        }
        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {

            totalWeight = 0f;
            var pWeight = Unsafe.AsPointer(ref weights.x);
            for (int i = 0; i < linkCount; i++)
                totalWeight += *pWeight++;

            var x = re.right;
            var y = re.up;
            var z = re.forward;
            var jMultX = springX.isFree || totalWeight==0 ? 0f : 1f;
            var jMultY = springY.isFree || totalWeight == 0 ? 0f : 1f;
            var jMultZ = springZ.isFree || totalWeight == 0 ? 0f : 1f;

            var pLink = Unsafe.AsPointer(ref links.x);
            pWeight = Unsafe.AsPointer(ref weights.x);
            var pAnchor = Unsafe.AsPointer(ref anchor0);


            if (link >= 0)
            {
                var rB = context.TransformDirection(link, anchor); 
                solver.WriteRow(startRow + 0, link, 0, -math.cross(x, rB * jMultX));
                solver.WriteRow(startRow + 1, link, 0, -math.cross(y, rB * jMultY));
                solver.WriteRow(startRow + 2, link, 0, -math.cross(z, rB * jMultZ));
                solver.WriteRow(startRow + 0, link, 1, x * jMultX);
                solver.WriteRow(startRow + 1, link, 1, y * jMultY);
                solver.WriteRow(startRow + 2, link, 1, z * jMultZ);

            }
            if (totalWeight > 0)
            {
                jMultX /= totalWeight;
                jMultY /= totalWeight;
                jMultZ /= totalWeight;
            }
            for (int i = 0; i < linkCount; i++)
            {
                var weight = *pWeight++;
                var connectedLink = *pLink++;
                var connectedAnchor = *pAnchor++;
                if (connectedLink >= 0)
                {
                    var rA = context.TransformDirection(connectedLink, connectedAnchor);
                    solver.WriteRow(startRow + 0, connectedLink, 0, math.cross(x, rA * jMultX * weight));
                    solver.WriteRow(startRow + 1, connectedLink, 0, math.cross(y, rA * jMultY * weight));
                    solver.WriteRow(startRow + 2, connectedLink, 0, math.cross(z, rA * jMultZ * weight));
                    solver.WriteRow(startRow + 0, connectedLink, 1, -x * jMultX * weight);
                    solver.WriteRow(startRow + 1, connectedLink, 1, -y * jMultY * weight);
                    solver.WriteRow(startRow + 2, connectedLink, 1, -z * jMultZ * weight);
                }
            }
        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var err = float3.zero;
            var velErr = float3.zero;
            if (totalWeight > 0)
            {
                if (link != -1)
                    err = context.TransformPoint(link, anchor);
                else
                    err = anchor;

                var pLink = Unsafe.AsPointer(ref links.x);
                var pWeight = Unsafe.AsPointer(ref weights.x);
                var pAnchor = Unsafe.AsPointer(ref anchor0);
                for (int i = 0; i < linkCount; i++)
                {
                    var weight = *pWeight++;
                    var connectedLink = *pLink++;
                    var connectedAnchor = *pAnchor++;
                    if(weight>0)
                        err -= context.TransformPoint(connectedLink, connectedAnchor) * weight / totalWeight;
                }
                

                err -= targetPosition;
                //velErr = -targetVelocity;

            }
            
            solver.WriteError(startRow + 0, springX * math.saturate(totalWeight), err.x, velErr.x);
            solver.WriteError(startRow + 1, springY * math.saturate(totalWeight), err.y, velErr.y);
            solver.WriteError(startRow + 2, springZ * math.saturate(totalWeight), err.z, velErr.z);
        }

        public unsafe void ApplyImpulseLimit<T>(in Solver solver, in T context, int startRow, float dt, ref float3 appliedLimit) where T : ISolverContext
        {
            return;
            var impulse = *(float3*)((float*)solver.impulse4 + startRow);
            var force = impulse / dt;
            var clamped = force; if (clamped.y > 200) clamped.y = 200;
            var undoForceDelta = -(force + appliedLimit- clamped);
            //if (undoForceDelta.y > 0) undoForceDelta.y = 0;// don't clamp
             appliedLimit += undoForceDelta;

            if (link != -1)
                context.ApplyForceAtLocalPoint(link, undoForceDelta, anchor);
            var pLink = Unsafe.AsPointer(ref links.x);
            var pWeight = Unsafe.AsPointer(ref weights.x);
            var pAnchor = Unsafe.AsPointer(ref anchor0);
            for (int i = 0; i < linkCount; i++)
            {
                var weight = *pWeight++;
                var connectedLink = *pLink++;
                var connectedAnchor = *pAnchor++;
                if (weight > 0)
                    context.ApplyForceAtLocalPoint(connectedLink, -undoForceDelta* weight / totalWeight, connectedAnchor);
            }
        }

    }
    public struct CGArticulationJoint
    {
        public static ArticulationJoint Create(int firstLink, int lastLink, int parent)
        {
            Debug.Assert(lastLink - firstLink + 1 <= 12, "CGArticulationJoint does not support more than 12 links");
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.CG,
                cg = new CGArticulationJoint()
                {
                    connectedLink = parent,
                    linkStart = firstLink,
                    linkCount = lastLink - firstLink + 1,
                    weights0 = 1,
                    weights1 = 1,
                    weights2 = 1,
                }
            };
        }
        public Spring spring;
        public int connectedLink;
        public int linkStart;
        public int linkCount;
        public float3 targetPosition;
        public Spring springY;

        public float3 attachment1pos;
        public float attachment1mass;
        public float3 attachment2pos;
        public float attachment2mass;

        public float4 weights0;
        public float4 weights1;
        public float4 weights2;
        float totalWeight;

        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            for (int i = 0; i < linkCount; i++)
                solver.MarkJacobian(startRow + 0, 3, linkStart + i, false, true);
            if (connectedLink != -1)
                solver.MarkJacobian(startRow + 0, 3, connectedLink, false, true);
        }

        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var totalMass = 0f;

            totalWeight = 0f;
            var pWeight = Unsafe.AsPointer(ref weights0.x);
            for (int i = 0; i < linkCount; i++)
            {
                var l = linkStart + i;
                var m = context.GetBody(l).m;
                totalWeight += *pWeight++ *m;
                totalMass += m;
            }
            
            pWeight = Unsafe.AsPointer(ref weights0.x);
            var jMult = spring.isFree || totalWeight == 0 ? 0f : 1f;
            var jMultY = springY.isFree || totalWeight == 0 ? 0f : 1f;

            if (connectedLink >= 0)
            {
                solver.WriteRow(startRow + 0, connectedLink, 1, new float3(-1, 0, 0) * jMult);
                solver.WriteRow(startRow + 1, connectedLink, 1, new float3(0, -1, 0) * jMultY);
                solver.WriteRow(startRow + 2, connectedLink, 1, new float3(0, 0, -1) * jMult);
            }
            if (totalWeight > 0)
            {
                jMult /= totalWeight;
                jMultY /= totalWeight;
            }
            //totalMass += attachment1mass;
            //adjustedMass += attachment1mass;
            for (int i = 0; i < linkCount; i++)
            {
                var weight = *pWeight++;
                var l = linkStart + i;
                var m = context.GetBody(l).m;
                m *= weight;
                solver.WriteRow(startRow + 0, l, 1, new float3(m, 0, 0) * jMult);
                solver.WriteRow(startRow + 1, l, 1, new float3(0, m, 0) * jMultY);
                solver.WriteRow(startRow + 2, l, 1, new float3(0, 0, m) * jMult);
            }

        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var err = float3.zero;

            //var totalMass = 0f;
            //for (int i = 0; i < linkCount; i++)
            //{
            //    var l = linkStart + i;
            //    ref var body = ref context.GetBody(l);
            //    var m = body.m;
            //    totalMass += m;
            //}
            var totalMass = totalWeight;
            totalMass += attachment1mass;
            totalMass += attachment2mass;
            err += attachment1pos * attachment1mass / totalMass;
            err += attachment2pos * attachment2mass / totalMass;
            var pWeight = Unsafe.AsPointer(ref weights0.x);
            for (int i = 0; i < linkCount; i++)
            {
                var weight = *pWeight++;
                var l = linkStart + i;
                ref var body = ref context.GetBody(l);
                var m = body.m;
                m *= weight;
                err += (body.x.pos) * m / totalMass;
            }
            if (connectedLink >= 0)
                err -= context.GetBody(connectedLink).x.pos;
            err -= targetPosition;

            solver.WriteError(startRow + 0, spring, err.x, 0);
            solver.WriteError(startRow + 1, springY, err.y, 0);
            solver.WriteError(startRow + 2, spring, err.z, 0);
        }
    }

    public struct PreserveAngularArticulationJoint
    {
        public static ArticulationJoint Create(int firstLink, int lastLink)
        {
            return new ArticulationJoint()
            {
                jointType = ArticulationJointType.PreserveAngular,
                preserveAngular = new PreserveAngularArticulationJoint()
                {
                    linkStart = firstLink,
                    linkCount = lastLink - firstLink + 1
                }
            };
        }

        public Spring spring;
        public int linkStart;
        public int linkCount;
        public float3 center;


        public unsafe void FillJacobianSparcity(in Solver solver, int startRow)
        {
            for (int i = 0; i < linkCount; i++)
                solver.MarkJacobian(startRow, 3, linkStart + i, true, true);
        }


        public unsafe void FillJacobian<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            var jMult = spring.isFree ? 0 : 1;
            center = context.CalculateCenterOfMass(linkStart, linkCount);

            for (int i = 0; i < linkCount; i++)
            {
                var l = linkStart + i;
                ref var body = ref context.GetBody(l);
                var r = body.x.pos - center;

                var mult = 1f;
                //if (i > 4 && i < 9) mult = .5f;
                solver.WriteMatrix(startRow, l, 0, body.I.ToFloat3x3() * mult * jMult);
                solver.WriteMatrix(startRow, l, 1, body.m * re.cross(r) * mult * jMult);

            }
        }
        public unsafe void CalculateErrors<T>(in Solver solver, in T context, int startRow) where T : ISolverContext
        {
            //center = BlockSolver.CalculateCenterOfMass(nodes, bodyStart, bodyCount);
            var L = float3.zero;
            for (int i = 0; i < linkCount; i++)
            {
                var l = linkStart + i;
                ref var body = ref context.GetBody(l);
                var r = body.x.pos - center;
                var mult = 1f;
                //if (i > 4 && i < 9) mult = .5f;
                var v = context.GetVelocity(l);
                L += mult * re.mul(body.I, v.angular) + mult * math.cross(r, body.m * v.linear);
            }

            if (spring.isFree) L = float3.zero;

            solver.WriteError(startRow, spring, 0, -L);
        }
    }

    
    public static class JointErrors
    {
        const bool drawDebug = false;
        public static void CalculateLinearError<T>(in T context, int connectedLink, float3 connectedAnchor, int link, float3 anchor, float3 targetPosition, float3 targetVelocity, out float3 err, out float3 velErr) where T : ISolverContext
        {
            if(link!=-1)
                err = context.TransformPoint(link, anchor);
            else
                err = anchor;

            if (connectedLink != -1)
                err -= context.TransformPoint(connectedLink, connectedAnchor);
            else
                err -= connectedAnchor;
            err -= targetPosition;
            velErr = -targetVelocity;

            if (drawDebug && link>=0)
            {
                Debug.DrawRay(context.TransformPoint(link, anchor), -err, Color.red);
//                Debug.DrawRay(s, targetPosition, Color.blue);
            }

        }

        public static void CalculateAngularError<T>(in T context, int connectedLink, int link, quaternion connectedRotation, quaternion targetRotation, float3 targetVelocity, RotationTargetMode mode, out float3 err, out float3 velError, float relativeVelInfluence) where T : ISolverContext
        {
            var parentRot = (mode != RotationTargetMode.Absolute && mode != RotationTargetMode.AbsolutePosRelativeVel && connectedLink >= 0) 
                ? context.GetBody(connectedLink).x.rot : quaternion.identity;
            var worldTarget = mode switch
            {
                //RotationTargetMode.AbsoluteNoFeedback => targetRotation,
                RotationTargetMode.Absolute => targetRotation,
                RotationTargetMode.AbsolutePosRelativeVel => targetRotation,
                RotationTargetMode.Relative => math.normalize(math.mul(parentRot, targetRotation)),
                RotationTargetMode.ParentOffset => math.normalize(math.mul(parentRot, math.mul(targetRotation, connectedRotation))),
                RotationTargetMode.SelfOffset => math.normalize(math.mul(parentRot, math.mul(connectedRotation, targetRotation))),
                _=> throw new InvalidOperationException()
            };
            if(link!=-1)
                err = math.mul(context.GetBody(link).x.rot, math.inverse(worldTarget)).ToAngleAxis();
            else
                err = math.inverse(worldTarget).ToAngleAxis();

            // for absolute joint need to ignore feedback velocity as jacobian is 0
            //targetAngularVel = (mode == RotationTargetMode.Absolute && connectedBody >= 0) ? -nodes[connectedBody].v.angular : float3.zero; // if relative to world, target velocity should be that of connected body
            //velError = (mode == RotationTargetMode.Absolute && connectedLink >= 0) ? context.GetVelocity(connectedLink).angular : float3.zero; // if relative to world, target velocity should be that of connected body
            //velError -= targetVelocity;


            var worldTargetVel = mode switch
            {
                RotationTargetMode.Absolute => connectedLink>=0? targetVelocity - (1-relativeVelInfluence)*context.GetVelocity(connectedLink).angular:targetVelocity,
                RotationTargetMode.AbsolutePosRelativeVel => targetVelocity,
                RotationTargetMode.Relative => math.mul(parentRot, targetVelocity),
                RotationTargetMode.ParentOffset => math.mul(parentRot, targetVelocity),
                RotationTargetMode.SelfOffset => math.mul(parentRot, math.mul(connectedRotation, targetVelocity)),
                _ => throw new InvalidOperationException()
            };
            velError = -worldTargetVel;
        }

    }
}