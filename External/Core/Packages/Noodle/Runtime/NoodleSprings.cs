using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    [Serializable]
    public struct NoodleSprings
    {
        [Header("Torso")]
        public Spring angular;
        public Spring waist;
        public Spring chest;
        public Spring head;
        [Space(5)]
        public Spring cg;
        public Spring cgFeet;
        public Spring preserveAngular;

        [Header("Arms")]
        public Spring handIK;
        public Spring handIKV;
        public Spring upperArm;
        public Spring lowerArm;
        public Spring upperArmIdle;
        public Spring lowerArmIdle;

        [Header("Feet")]
        public Spring upperLeg;
        public Spring lowerLeg;

        //public static NoodleSprings defaultSprings =>
        //   new NoodleSprings()
        //   {
        //       angular = new Spring(2000, 300),
        //       waist = new Spring(1500, 250),
        //       chest = new Spring(1000, 200),
        //       head = new Spring(150, 20),

        //       cg = new Spring(10000, 500),
        //       cgFeet = new Spring(25000, 1500),
        //       preserveAngular = new Spring(500, 5),

        //       handIK = new Spring(2000, 50),
        //       upperArm = new Spring(500, 50, 750),
        //       lowerArm = new Spring(200, 20, 750),
        //       upperArmIdle = new Spring(0, 2, 0),
        //       lowerArmIdle = new Spring(0, 1, 0),

        //       upperLeg = new Spring(500, 50),
        //       lowerLeg = new Spring(200, 20),

        //   };
    }
}