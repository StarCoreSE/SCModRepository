using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
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
namespace MWI.Thruster
{
    
    public class BlockParameters
    {
        //Used to store data for EmitterHandler
        public string subeTypeName = "";
        IMyThrust block;
        

        //public BlockParameters()
        //{

        //}
        public enum SoundTrigger
        {
            Range, //run while in range
            FromLower, // only run when you get into the range from a lower value
            FromHigher // only run when you get into the range from a higher value
        }

        public struct SoundSetup
        {
            public MySoundPair sound;
            public Vector2 rangeLimit;
            public SoundTrigger trigger;
        }

        
        List<SoundSetup> BlockSound;



        public void GetTheValues(out bool visible,out bool atmo, out bool work, out bool close,out float flame, out float heat)
        {
            visible = IsVisible;
            atmo = InAtmo;
            work = IsWorking;
            close = IsClose;
            flame = FlameIntensity;
            heat = HeatupIntensity;
        }


       

        public struct RecolorableSettings
        {
            public bool HideThrustFlames;
            public Vector4 FlameIdleColor;
            public Vector4 FlameFullColor;
        }
        public RecolorableSettings currentColorSettings = new RecolorableSettings();
        public event Action<RecolorableSettings> SetColor;
        //Partial Support for recolorable thrusters
        //bool AllowRecolorable = false;
        public bool IsRecolorable = false;
        public void UpdateColor()
        {

            // Add fallback paricle of recolorable = true?
            if (IsRecolorable)
            {
                RecolorableSettings colors = new RecolorableSettings
                {
                    HideThrustFlames = block.GetProperty("HideThrustFlames").AsBool().GetValue(block),
                    FlameFullColor = block.GetProperty("FlameFullColorOverride").AsColor().GetValue(block),
                    FlameIdleColor = block.GetProperty("FlameIdleColorOverride").AsColor().GetValue(block)
                };
                currentColorSettings = colors;
                SetColor?.Invoke(colors);
            }
        }

        public event Action<bool, uint> Visible;
        public bool IsVisible = false;
        public bool UpdateVisible(bool b, uint rID)
        {
            //i//f (b != IsVisible)
            //{
                IsVisible = b;
                Visible?.Invoke(IsVisible, rID);
            //}
            return IsVisible;



        }

        public bool IsClose = false;
        public event Action<int,float> Atmo;
        public bool InAtmo = false;
        private int lastAlt = 0;
        private float lastAtmoDens = 0.0f;
        public void UpdateAtmo(ThrusterSession.GridAtmoData d)
        {
            //if (b != IsWorking)
            //{
            //InAtmo = b;
            //Only trigger if somthing changed
            if (lastAlt != d.Altitude || lastAtmoDens != d.AtmoDensity)
            {
                lastAlt = d.Altitude;
                lastAtmoDens = d.AtmoDensity;
                Atmo?.Invoke(lastAlt, lastAtmoDens);
            }
            //}

        }
        //{ get; set; }
        //public bool IsWorking = false;//{ get; set; }

        public event Action<bool,uint> Working;
        public bool IsWorking = false;
        public void UpdateWorking(bool b, uint rID)
        {
            //if (b != IsWorking)
            //{
                IsWorking = b;
                Working?.Invoke(b, rID);
            //}

        }



        public float lastThrust = 0;
        public event Action NewThrust;
        public float Thrust = 0;
        public float avgThrust = 0;
        public void UpdateThrust(float f)
        {
            Thrust = f;
            //newThrust?.Invoke();
            

        }

        public event Action<float> NewIntensity;
        public float FlameIntensity = 0;
        public float avgFlameIntensity = 0;
        public void UpdateIntensity(float f)
        {
            FlameIntensity = f;
            NewIntensity?.Invoke(f);

        }
        public event Action Remove;
        public void RemoveMe()
        {
            
            Remove?.Invoke();


        }
        public event Action Destroy;
        public void DestroyMe()
        {

            Destroy?.Invoke();


        }

        public float HeatupIntensity = 0;
        public float LastColorIntensity= 0;
        public float HeatupColor = 0;

        
        //public BlockParameters(string theBlock)
        //{
        //    subeTypeName = theBlock;
        //}
        
        //public bool IsVisible = false;
        //    public bool IsClose = false;
        //    public bool InAtmo = false;
        //    public bool IsWorking = false;
        //    public bool IsVisible = false;
        //    public float FlameIntensity = 0;
        //    public float HeatupIntensity = 0;
        //    public float LastColorIntensity = 0;
        




    }
}