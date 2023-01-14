using System;
using NBG.Core;
using NBG.Core.Streams;
using NBG.Net;
using Recoil;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBG.Recoil.Net
{
    public sealed class RigidbodyList : INetBehavior, INetStreamer
    {
        private const int rotRange = 180; //Full circle; //TODO: see NBG.Net.QConsts

        internal struct Entry
        {
            public int bodyID;
            public RigidTransform basePos;
            public NetPrecisionSettings settings;
            public NetRigidbodySettingsOverride overrides;
        }

        List<Entry> _entries = new List<Entry>();

        public int Count => _entries.Count;

        private RigidbodyList()
        {
        }

        public static RigidbodyList BuildFrom(Scene scene)
        {
            var newList = new RigidbodyList();

            var rootGOs = scene.GetRootGameObjects();
            for (var i = 0; i < rootGOs.Length; i++)
            {
                FindRecursive(rootGOs[i].transform, newList._entries);
            }

            return newList;
        }

        public static RigidbodyList BuildFrom(Transform transform)
        {
            var newList = new RigidbodyList();

            FindRecursive(transform, newList._entries);

            return newList;
        }

        static void FindRecursive(Transform current, List<Entry> bodies)
        {
            if (current.TryGetComponent<Rigidbody>(out var rb))
            {
                var bodyID = ManagedWorld.main.FindBody(rb, true);
                if (bodyID == World.environmentId)
                {
                    Debug.Log($"Ignoring unregistered body: {rb.gameObject.GetFullPath()}", rb);
                    return;
                }

                var basePosition = World.main.GetBodyPosition(bodyID);
                var settings = NetPrecisionSettings.DEFAULT;

                if (current.TryGetComponent<NetRigidbodySettingsOverride>(out var settingsOverride))
                {
                    settings = settingsOverride.Settings;
                    //TODO: Add validation test
                    if (settings.syncPosition == NetPrecisionPos.None && settings.syncRotation == NetPrecisionRot.None)
                    {
                        Debug.LogWarning($"Found body with neither rotation nor position at: {rb.gameObject.GetFullPath()}");
                        return;
                    }
                }

                bodies.Add(new Entry()
                {
                    bodyID = bodyID,
                    basePos = basePosition,
                    settings = settings,
                    overrides = settingsOverride,
                });
            }

            foreach (Transform childTransform in current)
            {
                FindRecursive(childTransform, bodies);
            }
        }

        public void SetKinematic()
        {
            var prev = ManagedWorld.enablePhysXDataValidation;
            ManagedWorld.enablePhysXDataValidation = false;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var rb = ManagedWorld.main.GetRigidbody(entry.bodyID); //TODO: may be null?
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.isKinematic = true;
            }

            ManagedWorld.enablePhysXDataValidation = prev;
        }

        public void ResetKinematic()
        {
            var prev = ManagedWorld.enablePhysXDataValidation;
            ManagedWorld.enablePhysXDataValidation = false;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var rb = ManagedWorld.main.GetRigidbody(entry.bodyID); //TODO: may be null?
                rb.isKinematic = World.main.PhysXKinematicState(entry.bodyID);
                rb.collisionDetectionMode = World.main.PhysXCollisionDetectionMode(entry.bodyID);
            }

            ManagedWorld.enablePhysXDataValidation = prev;
        }

        #region INetBehavior

        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Server:
                    ResetKinematic();
                    break;
                case NetworkAuthority.Client:
                    SetKinematic();
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        void INetStreamer.CollectState(IStreamWriter stream)
        {
            //Temp: emit entry count
            stream.Write((int)_entries.Count, 16);
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                CollectEntryState(entry, stream);
            }
        }

        void INetStreamer.AddDelta(IStreamReader state0, IStreamReader delta, IStreamWriter result)
        {
            //Temp: skip entry count
            int numEntries = state0.ReadInt32(16);
            Debug.Assert(numEntries == _entries.Count);
            result.Write(_entries.Count, 16);
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                AddDelta(entry.basePos, entry.settings, state0, delta, result);
            }
        }

        void INetStreamer.ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames)
        {
            Debug.Assert(state0 != null);
            Debug.Assert(state1 != null);

            //Temp: compare entry count
            int count0 = state0.ReadInt32(16);
            int count1 = state1.ReadInt32(16);
            Debug.Assert(count0 == count1, $"RigidbodyList entry count mismatch ({count0} and {count1}))!");

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                ApplyEntryLerpedState(entry, state0, state1, mix, timeBetweenFrames);
            }
        }

        void INetStreamer.ApplyState(IStreamReader state)
        {
            Debug.Assert(state != null);

            //Temp: compare entry count
            int count0 = state.ReadInt32(16);
            Debug.Assert(count0 ==  _entries.Count, $"RigidbodyList entry count mismatch ({count0} and { _entries.Count}))!");

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                ApplyEntry(entry, entry.basePos, entry.settings, state);
            }
        }
        
        void INetStreamer.CalculateDelta(IStreamReader state0, IStreamReader state1, IStreamWriter delta)
        {
            //Skip debug values
            int count0 = state0.ReadInt32(16);
            int count1 = state1.ReadInt32(16);
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                CalculateDelta(state0, state1, delta, entry.settings);
            }
        }

        #endregion


        static void CollectEntryState(in Entry entry, IStreamWriter stream)
        {
            var body = World.main.GetBodyPosition(entry.bodyID);

            // Position
            switch (entry.settings.syncPosition)
            {
                case NetPrecisionPos.None:
                    break;
                case NetPrecisionPos.RelativeToSpawnPos:
                {
                    stream.WritePos(body.pos - entry.basePos.pos, entry.settings);
                }
                    break;
                case NetPrecisionPos.RelativeToRigidBody:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but position wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    //TODO: This is the delta to relative pos plus base pos. Not sure if that is smart. Maybe just relative to?
                    stream.WritePos((body.pos - relativeBody.pos) - entry.basePos.pos, entry.settings);
                }
                    break;
                case NetPrecisionPos.WorldSpace:
                    stream.WritePos(body.pos, entry.settings);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            var baseRotInv = math.inverse(entry.basePos.rot); //TODO: This is worthwhile to cache,

            // Rotation
            switch (entry.settings.syncRotation)
            {
                case NetPrecisionRot.None:
                    break;
                case NetPrecisionRot.RelativeToSpawnPos:
                {
                    var deltaQuat = math.mul(body.rot, baseRotInv);
                    stream.WriteRot(deltaQuat, entry.settings);
                }
                    break;
                case NetPrecisionRot.EulerX:
                {
                    var deltaQuat = math.mul(baseRotInv, body.rot);
                    var rot = math.degrees(-re.SignedAngleBetween(math.mul(deltaQuat, re.up), re.up, re.right));
                    stream.WriteAngle(rot, entry.settings);
                }
                    break;
                case NetPrecisionRot.EulerY:
                {
                    var deltaQuat = math.mul(baseRotInv, body.rot);
                    var rot = math.degrees(-re.SignedAngleBetween(math.mul(deltaQuat, re.forward), re.forward, re.up));
                    stream.WriteAngle(rot, entry.settings);
                }
                    break;
                case NetPrecisionRot.EulerZ:
                {
                    var deltaQuat = math.mul(baseRotInv, body.rot);
                    var rot = math.degrees(-re.SignedAngleBetween(math.mul(deltaQuat, re.right), re.right, re.forward));
                    stream.WriteAngle(rot, entry.settings);
                }
                    break;
                case NetPrecisionRot.RelativeTo:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but rotation wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    var deltaQuat = math.mul(body.rot, math.inverse(relativeBody.rot));
                    stream.WriteRot(deltaQuat, entry.settings);
                    break;
                }

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }
        
        static void ApplyEntry(in Entry entry, in RigidTransform bodyBase, in NetPrecisionSettings settings, IStreamReader stream)
        {
            ref var body = ref World.main.GetBody(entry.bodyID);

            switch (settings.syncPosition)
            {
                case NetPrecisionPos.None:
                    break;
                case NetPrecisionPos.RelativeToSpawnPos:
                {
                    body.x.pos = stream.ReadPos(settings) + bodyBase.pos;
                }
                    break;
                case NetPrecisionPos.RelativeToRigidBody:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but position wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    body.x.pos = stream.ReadPos(settings) + relativeBody.pos + bodyBase.pos;
                }
                    break;
                case NetPrecisionPos.WorldSpace:
                    body.x.pos = stream.ReadPos(settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            //Rotation:
            switch (settings.syncRotation)
            {
                case NetPrecisionRot.None:
                    break;
                case NetPrecisionRot.RelativeToSpawnPos:
                {
                    var uncompressedQuad = stream.ReadRot(settings);
                    body.x.rot = math.mul(uncompressedQuad, bodyBase.rot);
                }
                    break;
                case NetPrecisionRot.EulerX:
                {
                    var rot = stream.ReadAngle(settings);
                    body.x.rot = math.mul(bodyBase.rot, quaternion.AxisAngle(re.right, math.radians(rot)));
                }
                    break;
                case NetPrecisionRot.EulerY:
                {
                    var rot = stream.ReadAngle(settings);
                    body.x.rot = math.mul(bodyBase.rot, quaternion.AxisAngle(re.up, math.radians(rot)));
                }
                    break;
                case NetPrecisionRot.EulerZ:
                {
                    var rot = stream.ReadAngle(settings);
                    body.x.rot = math.mul(bodyBase.rot, quaternion.AxisAngle(re.forward, math.radians(rot)));
                }
                    break;
                case NetPrecisionRot.RelativeTo:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but rotation wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    var result = stream.ReadRot(settings);
                    body.x.rot = math.mul(result, relativeBody.rot);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        static void ApplyEntryLerpedState(in Entry entry, IStreamReader frame0, IStreamReader frame1, float mix, float timeBetweenFrames)
        {
            ref var body = ref World.main.GetBody(entry.bodyID);

            switch (entry.settings.syncPosition)
            {
                case NetPrecisionPos.None:
                    break;
                case NetPrecisionPos.RelativeToSpawnPos:
                {
                    var frame0Pos = frame0.ReadPos(entry.settings) + entry.basePos.pos;
                    var frame1Pos = frame1.ReadPos(entry.settings) + entry.basePos.pos;
                    body.x.pos = math.lerp(frame0Pos, frame1Pos, mix);
                    body.v4.linear = ((frame1Pos - frame0Pos) / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionPos.RelativeToRigidBody:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but position wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    var frame0Pos = frame0.ReadPos(entry.settings) + relativeBody.pos + entry.basePos.pos;
                    var frame1Pos = frame1.ReadPos(entry.settings) + relativeBody.pos + entry.basePos.pos;
                    body.x.pos = math.lerp(frame0Pos, frame1Pos, mix);
                    body.v4.linear = ((frame1Pos - frame0Pos) / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionPos.WorldSpace:
                {
                    var frame0Pos = frame0.ReadPos(entry.settings);
                    var frame1Pos = frame1.ReadPos(entry.settings);
                    body.x.pos = math.lerp(frame0Pos, frame1Pos, mix);
                    body.v4.linear = ((frame1Pos - frame0Pos) / timeBetweenFrames).To4D();
                }
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            //Rotation:
            switch (entry.settings.syncRotation)
            {
                case NetPrecisionRot.None:
                    break;
                case NetPrecisionRot.RelativeToSpawnPos:
                {
                    var frame0Rot = frame0.ReadRot(entry.settings);
                    var frame1Rot = frame1.ReadRot(entry.settings);

                    frame0Rot = math.mul(frame0Rot, entry.basePos.rot);
                    frame1Rot = math.mul(frame1Rot, entry.basePos.rot);
                    body.x.rot = math.slerp(frame0Rot, frame1Rot, mix);
                    body.v4.angular = (math.mul(frame0Rot, math.inverse(frame1Rot)).ToAngleAxis() / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionRot.EulerX:
                {
                    var stream0Rot = frame0.ReadAngle(entry.settings);
                    var stream1Rot = frame1.ReadAngle(entry.settings);
                    var stream0Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.right, math.radians(stream0Rot)));
                    var stream1Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.right, math.radians(stream1Rot)));
                    body.x.rot = math.slerp(stream0Quat, stream1Quat, mix);
                    body.v4.angular = (math.mul(stream0Quat, math.inverse(stream1Quat)).ToAngleAxis() / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionRot.EulerY:
                {
                    var stream0Rot = frame0.ReadAngle(entry.settings);
                    var stream1Rot = frame1.ReadAngle(entry.settings);
                    var stream0Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.up, math.radians(stream0Rot)));
                    var stream1Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.up, math.radians(stream1Rot)));
                    body.x.rot = math.slerp(stream0Quat, stream1Quat, mix);
                    body.v4.angular = (math.mul(stream0Quat, math.inverse(stream1Quat)).ToAngleAxis() / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionRot.EulerZ:
                {
                    var stream0Rot = frame0.ReadAngle(entry.settings);
                    var stream1Rot = frame1.ReadAngle(entry.settings);
                    var stream0Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.forward, math.radians(stream0Rot)));
                    var stream1Quat = math.mul(entry.basePos.rot, quaternion.AxisAngle(re.forward, math.radians(stream1Rot)));
                    body.x.rot = math.slerp(stream0Quat, stream1Quat, mix);
                    body.v4.angular = (math.mul(stream0Quat, math.inverse(stream1Quat)).ToAngleAxis() / timeBetweenFrames).To4D();
                }
                    break;
                case NetPrecisionRot.RelativeTo:
                {
                    var relativeToBodyId = entry.overrides != null ? entry.overrides.RelativeToBodyId : World.environmentId;
                    Debug.Assert(relativeToBodyId >= 0, $"Relative bodyID to not set, but rotation wants to encode relative");
                    var relativeBody = World.main.GetBodyPosition(relativeToBodyId);
                    var frame0Rot = frame0.ReadRot(entry.settings);
                    var frame1Rot = frame1.ReadRot(entry.settings);

                    frame0Rot = math.mul(frame0Rot, relativeBody.rot);
                    frame1Rot = math.mul(frame1Rot, relativeBody.rot);
                    body.x.rot = math.slerp(frame0Rot, frame1Rot, mix);
                    body.v4.angular = (math.mul(frame0Rot, math.inverse(frame1Rot)).ToAngleAxis() / timeBetweenFrames).To4D();
                    break;
                }

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        static void CalculateDelta(IStreamReader frame0, IStreamReader frame1, IStreamWriter delta, in NetPrecisionSettings settings)
        {
            bool posChanged = false;
            int3 deltaPosQuant = int3.zero;
            //Position
            if (settings.syncPosition != NetPrecisionPos.None)
            {
                var frame0Quant = frame0.ReadPosQuantized(settings);
                var frame1Quant = frame1.ReadPosQuantized(settings);
                posChanged |= (math.any(frame0Quant != frame1Quant));

                if (posChanged)
                {
                    deltaPosQuant = frame1Quant - frame0Quant;
                }
            }

            //Rotation
            switch (settings.syncRotation)
            {
                case NetPrecisionRot.None: //Object has no rotation -> We only write Pos
                    delta.Write(posChanged);
                    if (posChanged)
                    {
                        delta.WritePosQuantized(deltaPosQuant, settings);
                    }

                    break;
                //Note: This uses fallthrough because the Delta does not care about absolute or relativeTo, its only the delta between old and new frame
                case NetPrecisionRot.RelativeTo:
                case NetPrecisionRot.RelativeToSpawnPos:
                {
                    var frame0Quant = frame0.ReadRotQuantized(settings);
                    var frame1Quant = frame1.ReadRotQuantized(settings);
                    if (math.any(frame0Quant != frame1Quant) || posChanged)
                    {
                        delta.Write(true);
                        if (settings.syncPosition != NetPrecisionPos.None) //Write Position first
                        {
                            delta.WritePosQuantized(deltaPosQuant, settings);
                        }

                        var quant = frame1Quant - frame0Quant;
                        quant.w = frame1Quant.w; 
                        delta.WriteRotQuantized(quant, settings);
                    }
                    else
                    {
                        delta.Write(false);
                    }
                }
                    break;
                //Note: This uses fall through, which axis is pre-shared, and we don't need to understand the data to calculate a delta
                case NetPrecisionRot.EulerX:
                case NetPrecisionRot.EulerY:
                case NetPrecisionRot.EulerZ:
                {
                    var frame0Quant = frame0.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull);
                    var frame1Quant = frame1.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull);

                    if (frame0Quant != frame1Quant || posChanged)
                    {
                        delta.Write(true);
                        if (settings.syncPosition != NetPrecisionPos.None)
                        {
                            delta.WritePosQuantized(deltaPosQuant, settings);
                        }
                        var quant = frame1Quant - frame0Quant;
                        delta.Write(quant, settings.rotsmall, settings.rotlarge, settings.rotfull);
                    }
                    else
                    {
                        delta.Write(false);
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void AddDelta(in RigidTransform basePos, in NetPrecisionSettings settings, IStreamReader baseStream, IStreamReader deltaStream, IStreamWriter result)
        {
            if (settings.syncPosition != NetPrecisionPos.None || settings.syncRotation != NetPrecisionRot.None)
            {
                bool changed = deltaStream.ReadBool();

                if (changed)
                {
                    if (settings.syncPosition != NetPrecisionPos.None)
                    {
                        var pos0Quant = baseStream.ReadPosQuantized(settings);
                        var posDeltaQuant = deltaStream.ReadPosQuantized(settings);
                        result.WritePosQuantized(pos0Quant + posDeltaQuant, settings);
                    }

                    switch (settings.syncRotation)
                    {
                        case NetPrecisionRot.None:
                            break;
                    
                        case NetPrecisionRot.EulerX:
                        case NetPrecisionRot.EulerY:
                        case NetPrecisionRot.EulerZ:
                            var angle0Quat = baseStream.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull);
                            var deltaAngleQuat = deltaStream.ReadInt32(settings.rotsmall, settings.rotlarge, settings.rotfull);
                            result.Write(angle0Quat + deltaAngleQuat, settings.rotsmall, settings.rotlarge, settings.rotfull);
                            break;

                        //Note: This uses fallthrough because the Delta does not care about absolute or relativeTo, its only the delta between old and new frame
                        case NetPrecisionRot.RelativeTo:
                        case NetPrecisionRot.RelativeToSpawnPos:
                        {
                            var rot0Quant = baseStream.ReadRotQuantized(settings);
                            var rotDeltaQuant = deltaStream.ReadRotQuantized(settings);
                            var rotSum = rot0Quant + rotDeltaQuant;
                            rotSum.w = rotDeltaQuant.w;
                            result.WriteRotQuantized(rotSum, settings);
                        }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    if (settings.syncPosition != NetPrecisionPos.None)
                    {
                        var pos0 = (baseStream == null) ? basePos.pos : baseStream.ReadPos(settings);
                        result.WritePos(pos0, settings);
                    }

                    switch (settings.syncRotation)
                    {
                        case NetPrecisionRot.None:
                            break;
                        case NetPrecisionRot.RelativeToSpawnPos:
                        case NetPrecisionRot.RelativeTo:
                            var rot0 = (baseStream == null) ? basePos.rot : baseStream.ReadRot(settings);
                            result.WriteRot(rot0, settings);
                            break;
                        case NetPrecisionRot.EulerX:
                        {
                            var angle = (baseStream == null) ? math.degrees(-re.SignedAngleBetween(math.mul(basePos.rot, re.up), re.up, re.right)) : baseStream.ReadAngle(settings);
                            result.WriteAngle(NormalizeSignedDegrees(angle), settings);
                            break;
                        }
                        case NetPrecisionRot.EulerY:
                        {
                            var angle = (baseStream == null) ? math.degrees(-re.SignedAngleBetween(math.mul(basePos.rot, re.forward), re.forward, re.up)) : baseStream.ReadAngle(settings);
                            result.WriteAngle(NormalizeSignedDegrees(angle), settings);
                            break;
                        }
                        case NetPrecisionRot.EulerZ:
                        {
                            var angle = (baseStream == null) ? math.degrees(-re.SignedAngleBetween(math.mul(basePos.rot, re.right), re.right, re.forward)) : baseStream.ReadAngle(settings);
                            result.WriteAngle(NormalizeSignedDegrees(angle), settings);
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        static float NormalizeSignedDegrees(float angle)
        {
            var newAngle = angle;
            while (newAngle <= -180) newAngle += 360;
            while (newAngle > 180) newAngle -= 360;
            return newAngle;
        }
    }
}