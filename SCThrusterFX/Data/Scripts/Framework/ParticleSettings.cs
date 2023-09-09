using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using SC.Framework;
namespace SC.Framework
{

    public class ParticleSettings
    {
        //General
        public string subeTypeName = "";//used for reusing existing "subeTypeName|Dummies"
        public bool HideVanillaFlame = false;
        public bool UseHeatup = false;
        public float ThrustRateGain = 0;
        public float ThrustRateLoss = 0;
        public float EmissiveRateGain = 0;
        public float EmissiveRateLoss = 0;

        //Recolorable Thruster Support
        public bool Isrecolorable = false;
        public string RecolorableParticleNameOverride = "Missing Input"; //is you want to use a backup particle that works better with recolorable


        //Particle
        public string particleName = "Missing Input";

        public string particleType = "PARTICLE";
        public SC_Thruster.MexEmitterHandler_v2.EmitterType emitterType = SC_Thruster.MexEmitterHandler_v2.EmitterType.Particle;
        public float cullTimer = 90;

        public float ThrustMin=0;
        public float ThrustMax=1;

        public bool AtmoOnly = false;
        public float AtmoDensityLimitMin = 0.25f;
        public float AtmoDensityLimitMax = 1;
        public int AltitudeLimitMin = -int.MaxValue;
        public int AltitudeLimitMax = int.MaxValue;
        public float VelScalingFrom = -1; //-1 disable, min
        public float VelScalingTo = 2;

        public float Size = 1;
        public float Offset = 0;
        public float OffsetScaling = 0;

        public string Dummies = "thruster_flame_01";
        public IMyModelDummy dummyEmpty;

        
        



    }
}