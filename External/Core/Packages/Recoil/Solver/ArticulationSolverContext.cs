using NBG.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

namespace Recoil
{
    public interface ISolverContext : IGetBody
    {
        bool isArticulation(int link);
        ref ArticulatedBodyInertia GetInverseArticulatedInertia(int link); // for acticulations
    }

    public unsafe struct SolverBodies : IGetBody
    {
        public int count { get; set; }
        public int* links;
        public World worldCache;

        public SolverBodies(int* links, int nLinks)
        {
            this.links = links;
            this.worldCache = World.main;
            this.count = nLinks;
        }
        public ref Body GetBody(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref worldCache.GetBody(links[link]);
        }
        public ref BodyPhysXData GetBodyPhysXData(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref worldCache.GetBodyPhysXData(links[link]);
        }
        public ref Velocity4 GetVelocity4(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref worldCache.GetVelocity4(links[link]);
        }
    }

    // provides block solver for articulation
    public unsafe struct ArticulationSolverContext : ISolverContext, IDisposable
    {
        // data from Articulation
        public int count { get; set; }
        public int* links;

        // data from world
        public World worldCache;
        
        // own state
        public NativeArray<Velocity4> v;

        public ArticulationSolverContext(int* links, int nLinks, NativeArray<Velocity4> v)
        {
            this.links = links;
            this.count = nLinks;
            this.worldCache = World.main; 
            this.v = v;
        }

        public ref Body GetBody(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref worldCache.GetBody(links[link]);
        }
        public ref Velocity4 GetVelocity4(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref v.ItemAsRef(link);
        }

        public void Dispose()
        {
            Articulation.DisposeContext(this);
        }

        public bool isArticulation(int link) => false;

     
        public ref ArticulatedBodyInertia GetInverseArticulatedInertia(int link)
        {
            throw new NotImplementedException();
        }
     
    }
    public unsafe struct BlockSolverContext : ISolverContext, IDisposable
    {
        // data from Block
        public int count { get; set; }
        public int* links;
        public ArticulationLinkReference* bodiesToLinkReferences;
        public ArticulatedBodyInertia* articulatedInertias;

        // data from world
        public World worldCache;

        // own state
        public NativeArray<Velocity4> v;

        public BlockSolverContext(int* links, int nLinks, ArticulationLinkReference* bodiesToLinkReferences, ArticulatedBodyInertia* articulatedInertias, NativeArray<Velocity4> v)
        {
            this.links = links;
            this.count = nLinks;
            this.bodiesToLinkReferences = bodiesToLinkReferences;
            this.articulatedInertias = articulatedInertias;
            this.worldCache = World.main;
            this.v = v;
        }


        public ref Body GetBody(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref worldCache.GetBody(links[link]);
        }
        public ref Velocity4 GetVelocity4(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref v.ItemAsRef(link);
        }

        public void Dispose()
        {
            ConstraintBlock.DisposeContext(this);
        }

        public bool isArticulation(int link)
        {
            Unsafe.CheckIndex(link, count);
            return !bodiesToLinkReferences[link].isEmpty;
        }

        public ref ArticulatedBodyInertia GetInverseArticulatedInertia(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref articulatedInertias[link];
        }
       

    }
}
