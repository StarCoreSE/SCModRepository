using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard;


/*
 * 
 *           NOTE: Im not a programmer so this might be a traumatic experience for some
 *           [in progress] Proper spring cleaning of 2y old code from a rookei

             * TODO
             * 
             *              
             * 
             * 
             * [x]Make the code/setup more user friendly
             * [x]Add simple mode (simple setup and less particles)
             * [X]Setup thrusters
             * [x]Smoke at thrust control
             * [x]Add Multi particle support(bit of rework..)(parser)
             * [X]Add parser
             * [x]Add dummy support
             * [X]Rewrite / Optimization Pass #neverending
             * [x]Addative particles(kinda in)
             * [x]Add experimental particle setup
             * [x]Add "Idle" particle type- min is -1 
             * [x]Add scaling?
             * [x]Add dummy list support(parser)
             * 
             * ----The Above before update----
             * 
             * 
             * 
             * ----Low Prio-----
             * [ ]Add cull distance  per thruster
             *
             * [ ]Add coloring support 
             * [ ]Support for color thruster mod(check if terminal button exists?)
             * 
             *     
            */




namespace MWI.Thruster
{

    

    public class MWI_Thruster : MyGameLogicComponent
    {

        //Add in separate script
        //public  void LoadData() //replace model
        //{
        //    IMyThrust temp  = (IMyThrust)Entity;
        //    var id = temp.BlockDefinition.SubtypeId;
        //    var defId = new MyDefinitionId(typeof(MyObjectBuilder_Thrust), id);
        //    var blockDef = new MyCubeBlockDefinition();
        //    string path = "";
        //    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDef) && path != "")
        //    {
        //        blockDef.Model = path;
        //    }


        //    // amogst the earliest execution points, but not everything is available at this point.

        //    // main entry point: MyAPIGateway
        //    // entry point for reading/editing definitions: MyDefinitionManager.Static
        //    // these can be used anywhere as they're types not fields.

        //    //Instance = this;
        //}

        

        private IMyThrust block;
        private MyThrust thruster;

        private IMyCubeGrid myGrid;
        bool heavyDebug = false;

        public  static bool debugPrint=false; 

        public static ThrusterSession session = ThrusterSession.Instance;
        
        public List<ParticleSettings> pSettingList = new List<ParticleSettings>();

        public static Dictionary<string, IMyModelDummy> dummy = new Dictionary<string, IMyModelDummy>();
        public IMyModelDummy fxDummy;

        public List<MexEmitterHandler_v2> EmitterList = new List<MexEmitterHandler_v2>();




        readonly private int _turns = 0;





        //Sync Stuff
        public float lastSyncValue = 0.0f;

        //Sound
        /*Settings
         * Sound ID,
         * Loopable,
         * thrust output Range,
         * when to trigger( when output is in range,increase,decrease), volume,
         * 
         * 
         * 
         * 
         * 
         * */
        //private void CreateSound(ref IMyCharacter charac)
        //{
        //    var soundPair = new MySoundPair("Sin440"); //SubtypeID
        //    var soundEmitter = new MyEntity3DSoundEmitter((MyEntity)charac);
        //}


        public BlockParameters blockParameters = new BlockParameters();

        protected string partName = "Particle";
        
        private MatrixD particleLocalMatrix;

        private Vector3D particleWorldPosition;
        public VRageMath.Vector3D subpartStartPos;
        private MyEntitySubpart theSubpart;
        
        //static Dictionary<string,int> 




        private bool _thrusting = false;
        
        readonly private bool _pIsRunning = false;

        int retriggerDelay = 10;
        int currentDelay = 0;
        int currentIntensityDelay = 0;

        public float _thrust = 0;
        public float _maxThrust = 0;
        public float _thrustValue = 0;
        public float _oldThrustValue = 0;
        public float _ThrustValueLastTrigger = 0;

        public float _changeRate = 0.0099f;
        public float _flameIntensity = 0.00f;
        public float _flameIntensityLast = 0.00f;

        public float _currentIntensity = 0.00f;
        public float _EmissiveIntensity = 0f;
        public float _lastThrust = 0;
        
        Color heatColor = new Color(0, 0 - 0.5f, 0 - 0.5f, 0);
        Color lastheatColor = new Color(0, 0, 0, 0);
        bool heatupIsDone = false;

        //private bool _inAtmo = true;



        int _waitForStart = 0;
        int _lowUpdate = 100;
        int _lowUpdateTimer = 0;

        private MatrixD particleWorldMatrix;
        


        
        
        
        //bool _isInRange = false;

        bool firstRun = true;
        bool turningVisibleTrigger = true;


        float averageThrust=0;
        float averageIntensity = 0;
        
        bool _isMediumRange = false;
        bool _isCloseRange = false;
        bool thrusterIdle = false;

        bool switchedToZero = false;


        public bool IsVisible = false;
        public bool IsClose = false;
        public bool InAtmo = false;
        public bool IsWorking = false;
        public float FlameIntensity = 0;
        public float HeatupIntensity = 0;
        public float LastColorIntensity = 0.2f;

        public float ThrustRateGain = 0.15f;
        public float ThrustRateLoss = 0.3f;

        public string emissiveMat = "heatup";
        public float EmissiveRateGain = 0.005f;
        public float EmissiveRateLoss = 0.01f;






        float cullTimer = 90;
        float _cullDist = 1500.0f;
        
        bool _setupDone = false;
        bool _checkRecolorable = false;
        ITerminalProperty property_hide_flame;
        ITerminalProperty property_full_flame_color;
        ITerminalProperty property_idle_flame_color;


        public bool hideVanillaFlame = false;
         public bool _useEmissive = false;
        



        


        
        public bool runIntensityUpdate = false;
        public bool runHeatup = false;






        public class MexEmitterHandler_v2
        {
            
            public bool _isActive = false;
            
            public int statusLeve = 0;
            
            bool heavyDebug = false;
            

            public bool visible = false;
            public bool atmo = false;
            public bool close = false;
            public bool works = false;

            bool stateCheck = false;

            public bool destroyObject = false;

            bool validParticleSpawn = false;

            bool gotEmitter = false;

            // Sprite- flipbook particle like mwi flame
            // Particle - normal particle. no fancy scaling just  
            public enum EmitterType { Sprite, Particle }

            //EmitterType _emitterType = EmitterType.Particle;

            string dummyName;
         

            int forcedUpdateTimer = 180;

            public int totalEmitterCount = 0;
            public struct ThrustData {
                public IMyThrust Block;
                public BlockParameters BlockData;
                public IMyModelDummy Dummy;
                public EmitterType EmitterType;
                //Recolorable
                public bool hideEffect;
                public Color FlameFullColor;
                public Color FlameIdleColor;
                public bool Recolorable; // this effect should be affected by recolorable
                //
                public uint BlockRenderID;
                public String ParticleName;
                public int MaxLife;
                public int CurrentLife;
                public float PositionOffset;
                public float PositionOffsetScaling;
                public float Size;
                public float MinLimit;
                public float MaxLimit;

                public bool AtmoOnly;
                public bool DensityRange;
                public bool AltitudeRange;
                public float AtmoDensityLimitMin;
                public float AtmoDensityLimitMax;
                public int AtmoAltitudeLimitMin;
                public int AtmoAltitudeLimitMax;


                public float VelocityScalingFrom;
                public bool UseScaling;
                public float VelocityScalingTo;

                public MatrixD DummyMatrix;
                public Vector3D TranslationOffset;
                public MyParticleEffect Emitter;
                public float ThrustValue;

            };
            public ThrustData thrustData = new ThrustData();

            struct debugDrawLineSettings
            {
                //Make global as value in dict so that every draw related to an event could be grouped and removed together
                public MyStringId material;
                public Vector4 color;
                public Vector3D startPosition;
                public Vector3D vector;
                public float lineDistance;
                public float lineWidth;

            }

            public MexEmitterHandler_v2(IMyThrust _thisBlock, int life, BlockParameters blockValues, ParticleSettings particleSetup)  //could be remade to look for specific slots in the handler list, Smoke_1 = [1] and so on
            {
                
                thrustData.BlockData = blockValues;
                thrustData.Block = _thisBlock;
                //_thisBlock.
                //MyLog.Default.WriteLineAndConsole("_block: " + _block.BlockDefinition.SubtypeName);
                thrustData.ParticleName = particleSetup.particleName;
                //MyLog.Default.WriteLineAndConsole("particleName: " + particleName);
                thrustData.MaxLife = life;
                thrustData.CurrentLife = 0;
                //MyLog.Default.WriteLineAndConsole("_maxLife: " + _maxLife);
                thrustData.PositionOffset = particleSetup.Offset;
                //MyLog.Default.WriteLineAndConsole("_positionOffset: " + _positionOffset);
                thrustData.PositionOffsetScaling = particleSetup.OffsetScaling;
                //MyLog.Default.WriteLineAndConsole("_OffsetScaling: " + _OffsetScaling);
                thrustData.Size = particleSetup.Size;
                //MyLog.Default.WriteLineAndConsole("_Size: " + _Size);
                thrustData.MinLimit = particleSetup.ThrustMin;
                //MyLog.Default.WriteLineAndConsole("_LimitMin: " + _LimitMin);
                thrustData.MaxLimit = particleSetup.ThrustMax;
                //MyLog.Default.WriteLineAndConsole("_LimitMax: " + _LimitMax);
                thrustData.Dummy = particleSetup.dummyEmpty;
                dummyName = particleSetup.Dummies;
                //MyLog.Default.WriteLineAndConsole("_dummy: " + _dummy);
                thrustData.AtmoOnly = particleSetup.AtmoOnly;
                //MyLog.Default.WriteLineAndConsole("atmoOnly: " + atmoOnly);
                thrustData.VelocityScalingFrom = particleSetup.VelScalingFrom;
                thrustData.UseScaling = particleSetup.VelScalingFrom != -1;
                thrustData.VelocityScalingTo = particleSetup.VelScalingTo;
                thrustData.EmitterType = particleSetup.emitterType;
                thrustData.MaxLife = MathHelper.RoundToInt(particleSetup.cullTimer);
                //if(_maxLife!=90) MyLog.Default.WriteLineAndConsole("Setting CullingTime: " + particleSetup.cullTimer);

                thrustData.BlockRenderID = thrustData.Block.Render.GetRenderObjectID();
                thrustData.DummyMatrix = MatrixD.Normalize(thrustData.Dummy.Matrix);
                //MyLog.Default.WriteLineAndConsole("DummyMatrix Before: " + thrustData.DummyMatrix.Translation.ToString());
                thrustData.TranslationOffset = thrustData.DummyMatrix.Translation + thrustData.DummyMatrix.Forward * thrustData.PositionOffset;

                if(particleSetup.AltitudeLimitMin != -int.MaxValue || particleSetup.AltitudeLimitMax != int.MaxValue)
                {
                    //MyLog.Default.WriteLineAndConsole("AltLimit!!!!!");
                    thrustData.AltitudeRange = true;
                    
                }
                thrustData.AtmoAltitudeLimitMin = particleSetup.AltitudeLimitMin;
                thrustData.AtmoAltitudeLimitMax = particleSetup.AltitudeLimitMax;

                if (particleSetup.AtmoDensityLimitMin != 0.25f || particleSetup.AtmoDensityLimitMax != 1.0f)
                {
                    //MyLog.Default.WriteLineAndConsole("DenseLimit!!!!!");
                    thrustData.DensityRange = true;

                }
                thrustData.AtmoDensityLimitMin = particleSetup.AtmoDensityLimitMin;
                thrustData.AtmoDensityLimitMax = particleSetup.AtmoDensityLimitMax;

                thrustData.Recolorable = particleSetup.Isrecolorable;
                if (thrustData.Recolorable)
                {
                    thrustData.hideEffect = thrustData.Block.GetProperty("HideThrustFlames").AsBool().GetValue(thrustData.Block);
                    thrustData.FlameFullColor = thrustData.Block.GetProperty("FlameFullColor").AsColor().GetValue(thrustData.Block);
                    thrustData.FlameIdleColor = thrustData.Block.GetProperty("FlameIdleColor").AsColor().GetValue(thrustData.Block);
                }

                //PrintStruct(thrustData);
                AddEmitterEvents();
            }

            public IMyModelDummy GetTheDummy(string thisDummy)
            {

                //make to a cached list
                var temp = new Dictionary<string, IMyModelDummy>();
                temp.Clear();


                thrustData.Block.Model.GetDummies(temp);

                //Expand Usage
                foreach (var item in temp)
                {
                    //MyLog.Default.WriteLineAndConsole("[Dummies: key|value:]" + item.Key + " | " + item.Value);
                    if (item.Key.Contains(thisDummy))
                    {
                        //var t = item.Value;
                        //t.Matrix.SetRotationAndScale(1 / t.Matrix.);
                        return item.Value;
                    }
                }

                return null;
            }

            public void ReInitDummy()
            {
                thrustData.Dummy = GetTheDummy(dummyName);
                //thrustData.Dummy.Matrix.
                //MyLog.Default.WriteLineAndConsole("DummyMatrix ID: " + thrustData.Dummy.GetHashCode().ToString());
                thrustData.DummyMatrix = MatrixD.Normalize(thrustData.Dummy.Matrix);
                thrustData.TranslationOffset = thrustData.Dummy.Matrix.Translation + thrustData.DummyMatrix.Forward * thrustData.PositionOffset;
                //MyLog.Default.WriteLineAndConsole("DummyMatrix After: " + thrustData.DummyMatrix.Translation.ToString());
                //thrustData.BlockRenderID = thrustData.Block.Render.GetRenderObjectID();
            }

            public void DrawLineSettings()
            {
                List<debugDrawLineSettings> drawSettings = new List<debugDrawLineSettings>();
                drawSettings = new List<debugDrawLineSettings>();

                drawSettings.Add(new debugDrawLineSettings
                {
                    material = MyStringId.GetOrCompute("Square"),
                    color = new Vector4(1, 0, 1, 1),
                    startPosition = thrustData.Block.GetPosition(),
                    vector = Vector3.Normalize(thrustData.Block.GetPosition() - Vector3D.Transform( thrustData.Emitter.WorldMatrix.Translation, thrustData.Block.WorldMatrix) ),
                    lineDistance = Vector3.Distance(Vector3D.Transform(thrustData.Emitter.WorldMatrix.Translation, thrustData.Block.WorldMatrix), thrustData.Block.WorldMatrix.Translation),
                    lineWidth = 0.5f
                });

                foreach (var item in drawSettings)
                {
                    MyTransparentGeometry.AddLineBillboard(item.material, item.color, item.startPosition, item.vector, item.lineDistance, item.lineWidth, BlendTypeEnum.Standard);
                }
            }

            public ThrustData GetThrusterData()
            {
                ThrustData data = thrustData;
                return data;
            }

            public void AddEmitterEvents()
            {
                thrustData.BlockData.Visible += RangeCheck;
                thrustData.BlockData.Working += UpdateWorking;
                thrustData.BlockData.Atmo += UpdateAtmo;
                thrustData.BlockData.Remove += DestroyEmitters;
                thrustData.BlockData.Destroy += EnableDestroy;
                //If Recolorable == true
                if(thrustData.Recolorable) thrustData.BlockData.SetColor += UpdateEmitterColor;
                session.RemoveEmitter += RemoveThis;

                thrustData.BlockData.NewIntensity += UpdateTheEmitter;

            }
            void UpdateEmitterColor(BlockParameters.RecolorableSettings newEmitterColors)
            {
                //MyLog.Default.WriteLineAndConsole("Atmo is: " + t);
                if (true)
                {
                    bool hideEffect = newEmitterColors.HideThrustFlames;
                    Color FlameFullColor = newEmitterColors.FlameFullColor;
                    Color FlameIdleColor = newEmitterColors.FlameIdleColor;

                    if (thrustData.Emitter != null)
                    {                        
                        UpdateTheEmitter(thrustData.ThrustValue);
                    }
                }

            }
            public void EnableDestroy()
            {
                destroyObject = true;
            }
            public void RemoveEmitterEvents()
            {


                thrustData.BlockData.Visible -= RangeCheck;
                thrustData.BlockData.Working -= UpdateWorking;
                thrustData.BlockData.Atmo -= UpdateAtmo;
                //If Recolorable == true
                if (thrustData.Recolorable) thrustData.BlockData.SetColor -= UpdateEmitterColor;
                thrustData.BlockData.Remove -= DestroyEmitters;
                thrustData.BlockData.Destroy -= EnableDestroy;

                thrustData.BlockData.NewIntensity -= UpdateTheEmitter;
                
            }
            /// <summary>
            /// Handles and keep track of particle setups
            /// </summary>
            /// 
            
            


            public void UpdateTheEmitter(float newIntensity)
            {
                //MyLog.Default.WriteLineAndConsole(thrustData.BlockRenderID.ToString());
                thrustData.ThrustValue = newIntensity;

                //MyLog.Default.WriteLineAndConsole("Updating Emitter");
                
                stateCheck = false;
                if (((atmo && thrustData.AtmoOnly)
                    || !thrustData.AtmoOnly)
                    && visible && works)
                    stateCheck = true;

                
                
                
                if (stateCheck)
                {
                    //MyLog.Default.WriteLineAndConsole("---State True:"+ thrustData.ThrustValue +" > "+ thrustData.MinLimit + " && " + thrustData.ThrustValue+ " <= " + thrustData.MaxLimit);
                    validParticleSpawn = (thrustData.ThrustValue > thrustData.MinLimit && thrustData.ThrustValue <= thrustData.MaxLimit);
                }
                else
                {
                    validParticleSpawn = false;

                }

                if (validParticleSpawn && visible && works)
                {
                    //MyLog.Default.WriteLineAndConsole("!!!!!!!!!!!!!!!!!!!!!!!!!!!!IS VALID!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    _isActive = true;
                    if (!gotEmitter)
                    {
                        
                        //gotEmitter = true;
                        CreateParticle();
                    }
                    

                }
                else
                {
                    //MyLog.Default.WriteLineAndConsole("!!!!!!!!!!!!!!!!!!!!!!!!!!!!IS NOT VALID!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    _isActive = false;
                    
                }
                //if (thrustData.Emitter != null)
                //{
                //    if (newIntensity > 0.0f && (thrustData.Emitter.IsEmittingStopped || thrustData.Emitter.IsStopped) && validParticleSpawn) MyLog.Default.WriteLineAndConsole("!!!!!!!!!!!!!!!!!!!!!!!!!!!!SMELLS LIKE FISH YA!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                //}
                if (thrustData.Emitter == null) return;
                if (!_isActive)
                {
                    
                    if (thrustData.CurrentLife >= thrustData.MaxLife)
                    {//add before switch
                        if (destroyObject)
                        {
                            RemoveThis();
                            return;
                        }
                        //MyLog.Default.WriteLineAndConsole("Destroy");
                        DestroyEmitters();
                        return;
                    }


                }
                if(thrustData.EmitterType == EmitterType.Sprite)
                {                    
                    IsBillboad(); //Easier to track crashes for now
                }
                if(thrustData.EmitterType == EmitterType.Particle)
                {
                    IsParticle(); //Easier to track crashes for now
                }


                
            }

            void IsParticle()
            {
                if (!_isActive)
                {
                    if (!thrustData.Emitter.IsEmittingStopped)
                        thrustData.Emitter.StopEmitting();

                }
                if (_isActive)
                {
                    if (thrustData.VelocityScalingFrom != -1)
                    {                        

                        thrustData.Emitter.UserVelocityMultiplier = MathHelper.SmoothStep(thrustData.VelocityScalingFrom, thrustData.VelocityScalingTo, thrustData.ThrustValue);
                    }

                    if (thrustData.Emitter.IsEmittingStopped) thrustData.Emitter.Play();

                }
            }
            void IsBillboad()
            {
                if (!_isActive)
                {
                    thrustData.Emitter.UserScale = 0;
                }
                else if (_isActive)
                {

                    if (thrustData.Emitter.IsEmittingStopped) thrustData.Emitter.Play();
                    var expoMulti = (1 - (float)Math.Pow(2.0f, -4 * thrustData.ThrustValue)); //flame Intensity

                    thrustData.Emitter.UserScale = thrustData.Size * expoMulti;
                    float offsetValue = (thrustData.PositionOffsetScaling * expoMulti);
                    var offsetFlame = thrustData.TranslationOffset + (thrustData.DummyMatrix.Forward * offsetValue);

                    thrustData.Emitter.SetTranslation(ref offsetFlame);
                    //var poss = thrustData.Block.WorldMatrix.Translation + offsetFlame;


                }
            }

            public void RemoveThis()
            {
               // MyLog.Default.WriteLineAndConsole("[TFX]Running late Removal....");
                RemoveEmitterEvents();
                if(thrustData.Emitter!=null) session.liveParticles -= 1;
                thrustData.Emitter?.Stop(true);
                thrustData.Emitter?.StopLights();
                thrustData.Emitter = null;
                gotEmitter = false;
                thrustData.CurrentLife = 0;
                //thrustEvents.Timer -= UpdateEmitterLife;
                //session.RemoveEmitter -= RemoveThis;
                session.TickTime -= UpdateEmitterLife;
                if (destroyObject) {
                    //MyLog.Default.WriteLineAndConsole("[TFX]Running late Removal....");

                    session.EmitterRemoval(this);
                }
                
            }
            
            public string Debug()
            {
                var d = " ";
                if (thrustData.Emitter != null) d = "X";
                if (_isActive) d += "X";
                return validParticleSpawn +"|"+ d + "|" +"|T:"+ thrustData.BlockData.FlameIntensity;
            }

            void RangeCheck(bool t, uint renderID)
            {
                //MyLog.Default.WriteLineAndConsole("Range is: "+t);
                //inRange = s;
                visible = t;
                if (t == false)
                {
                    //MyLog.Default.WriteLineAndConsole("OUT OF RANGE!!!!!!!");
                    _isActive = false;
                    //DestroyEmitters();
                }
                if (thrustData.BlockRenderID != renderID)
                {
                    //MyLog.Default.WriteLineAndConsole("Range|New RID: "+ renderID);
                    thrustData.BlockRenderID = renderID;
                }
                //else { if(!_isActive)}



                //else { UpdateValidParicle(); }
            }
            void UpdateAtmo(int alt,float atmoDensity)
            {
                if (!thrustData.AtmoOnly) return;
                //MyLog.Default.WriteLineAndConsole("Atmo is: " + t);
                bool rUpdate = false;
                //Is atmo density in range
                bool aD = true;
                aD = (atmoDensity > thrustData.AtmoDensityLimitMin && atmoDensity <= thrustData.AtmoDensityLimitMax);

                //Is atmo altitude in range
                bool aA = true;
                if (thrustData.AltitudeRange)
                {
                    aA = (alt > thrustData.AtmoAltitudeLimitMin && alt <= thrustData.AtmoAltitudeLimitMax);
                    rUpdate = atmo != (aD&&aA);
                    //MyLog.Default.WriteLineAndConsole("Alt:" + "{" + thrustData.AtmoAltitudeLimitMin + "< " + alt + " >" + thrustData.AtmoAltitudeLimitMax + "}"+aA);
                    //MyLog.Default.WriteLineAndConsole("Den:" + "{" + thrustData.AtmoDensityLimitMin + "< " + atmoDensity + " >" + thrustData.AtmoDensityLimitMax + "}"+aD);

                    //                    MyLog.Default.WriteLineAndConsole("Ranges:"+ (aD && aA)+" | "+ rUpdate+"||"+atmo+ " | " + aA+"{"+ thrustData.AtmoAltitudeLimitMin + " "+ thrustData.AtmoAltitudeLimitMax + "}");
                }
                else
                {
                    rUpdate = atmo != aD;
                }
                //MyLog.Default.WriteLineAndConsole("CheckingATmo "+ atmoDensity+" | " + t);
                if (rUpdate)
                {
                    //MyLog.Default.WriteLineAndConsole("Running ATMO!!!!!!!!!!!!!!!!!!");
                    atmo = (aD && aA);
                    UpdateTheEmitter(thrustData.ThrustValue);
                }
            }
            void UpdateWorking(bool t,uint renderID)
            {
                //MyLog.Default.WriteLineAndConsole("Working is: " + t);
                works = t;
                //MyLog.Default.WriteLineAndConsole("UpToDate ID = "+ thrustData.BlockRenderID.ToString() + " New ID from BLock:" + renderID.ToString());
                {
                    //MyLog.Default.WriteLineAndConsole("Working|New RID: " + renderID);
                    thrustData.BlockRenderID = renderID;
                }
                if(works != t) UpdateTheEmitter(thrustData.ThrustValue);
                
            }

            void UpdateEmitterLife(int timeTick)//Event from session
            {
                //DrawLineSettings();
                //MyLog.Default.WriteLineAndConsole("TICK THE TIME");

                


                if (!_isActive)
                {
                    //MyLog.Default.WriteLineAndConsole("TICK THE TIME "+ timeTick);
                    thrustData.CurrentLife += timeTick;
                    if (thrustData.CurrentLife >= thrustData.MaxLife) {
                        if (destroyObject)
                        {
                            RemoveThis();
                            return;
                        }
                        //MyLog.Default.WriteLineAndConsole("[Destroyiing Time]");
                        DestroyEmitters();
                    }
                        
                }
                else
                {
                    thrustData.CurrentLife = 0;
                    //DestroyEmitters();
                }

                forcedUpdateTimer -= timeTick;
                if (forcedUpdateTimer <= 0)
                {
                    UpdateTheEmitter(thrustData.ThrustValue);
                    forcedUpdateTimer = 180;
                }
                //Debug
                //if (_currentLife > 0 && _currentLife < _maxLife)
                //{

                //    MyLog.Default.WriteLineAndConsole("Is: [" + _isActive + "] | " + _currentLife);
                //}

            }


            void  CreateParticle()
            {
                //MyLog.Default.WriteLineAndConsole("Create Particle...");

                //if (thrustData.Emitter == null) { ReInitDummy(); }

                if (MyParticlesManager.TryCreateParticleEffect(thrustData.ParticleName, ref thrustData.DummyMatrix, ref thrustData.TranslationOffset, thrustData.BlockRenderID, out thrustData.Emitter))
                {
                    
                    //if (totalEmitterCount == 0) totalEmitterCount = thrustData.Emitter.Data.GetGenerations().Count;
                    session.TickTime += UpdateEmitterLife;
                    thrustData.BlockData.Remove += DestroyEmitters;

                    gotEmitter = true;
                    thrustData.Emitter.Play();
                    if (thrustData.EmitterType == EmitterType.Sprite) thrustData.Emitter.UserScale = 0;
                    else thrustData.Emitter.UserScale = thrustData.Size;
                    session.liveParticles++;
                    //MyLog.Default.WriteLineAndConsole("Emitter Before: " + thrustData.Emitter.WorldMatrix.Translation.ToString());

                }
    
            }

            public void DestroyEmitters()
            {if (destroyObject) return;
                if(thrustData.Emitter!=null) session.liveParticles -= 1;
                //MyLog.Default.WriteLineAndConsole("[Destroyiing]");
                thrustData.Emitter?.Stop();
                thrustData.Emitter?.StopLights();
                thrustData.Emitter = null;
                //thrustData.Emitter.Clear();
                gotEmitter = false;
                thrustData.CurrentLife = 0;
                //thrustEvents.Timer -= UpdateEmitterLife;
                session.TickTime -= UpdateEmitterLife;

                //if (thrustData.Emitter != null) MyParticlesManager.RemoveParticleEffect(_emitter);
            }

        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
           // if (MyAPIGateway.Utilities.IsDedicated) return;
            block = (IMyThrust)Entity;
            thruster = block as MyThrust;

            


            //(block as IMyTerminalBlock).GetProperty() HideThrustFlames FlameFullColor
            //Test early effectInit


            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME ;
            
        }

        public override void OnAddedToScene()
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (session == null) return;
            //MyAPIGateway.Utilities.ShowNotification("Is Valid: " + session.DefExtensions.MODID, 1, MyFontEnum.Blue);
            if (block.CubeGrid != null)
            {
                if (true)
                    
                   if (!_setupDone && !MyAPIGateway.Utilities.IsDedicated)
                    {
                    
                    firstRun = true;
                    //AddEvents();

                    //Can I make it check for existing setups instead of looping through the sama subtypes?
                    //bool setupAlreadyDone = false;

                    //setupAlreadyDone = session.settingCache.ContainsKey(block.BlockDefinition.SubtypeId);

                    ModExtensionSetup();


                    //setupAlreadyDone=true
                    block.IsWorkingChanged += UpdateWorking;
                    block.CubeGrid.OnGridSplit += UpdateGrid;

                    //block.ThrustOverrideChanged += updateThrust;

                    if (hideVanillaFlame)
                    {

                        thruster.BlockDefinition.FlameFullColor = new Vector4(0);
                        thruster.BlockDefinition.FlameIdleColor = new Vector4(0);
                        thruster.BlockDefinition.FlameVisibilityDistance = 0;
                        thruster.Render.UpdateFlameAnimatorData();
                    }


                    //Init block in session
                    var blockSID = block.BlockDefinition.SubtypeId;


                    //Valid subtypes for the session when scaning nearby blocks
                    if (!session.blockSubTypes.ContainsKey(blockSID))
                    {
                        //MyLog.Default.WriteLineAndConsole("[TFX] Adding Subtype: " + blockSID);
                        session.blockSubTypes.Add(blockSID, 1);
                    }
                    else
                    {
                        //update hwo many blocks exist, waste.. maybe :D
                        var value = 0;
                        session.blockSubTypes.TryGetValue("blockSID", out value);
                        //MyLog.Default.WriteLineAndConsole("[Adding Value]");
                        session.blockSubTypes["blockSID"] = value++;

                    }

                    myGrid = block.CubeGrid;
                    BoundingSphereD theSphere = new BoundingSphereD { Radius = 200 };
                    theSphere.Center = myGrid.WorldAABB.Center;//; WorldAABB.Center;

                    if (!session.gridCache.ContainsKey(myGrid))
                    {
                        //MyLog.Default.WriteLineAndConsole("[Adding Grid]");
                        session.gridCache.Add(myGrid,0);
                    }

                    //_thrust = block.CurrentThrust;
                    _maxThrust = block.MaxThrust;

                    try
                    {
                        dummy = new Dictionary<string, IMyModelDummy>();
                        dummy.Clear();
                        var tmp1 = new Dictionary<string, IMyModelDummy>();

                        block.Model.GetDummies(tmp1);

                        //Expand Usage
                        foreach (var item in tmp1)
                        {
                            //MyLog.Default.WriteLineAndConsole("[Dummies: key|value:]" + item.Key + " | " + item.Value);
                            if (item.Key.Contains("flame"))
                            {
                                //var t = item.Value;
                                //t.Matrix.SetRotationAndScale(1 / t.Matrix.);
                                fxDummy = item.Value;
                                dummy.Add(item.Key, item.Value);
                            }
                        }

                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    
                    //Add el particle handler 2
                    //MyLog.Default.WriteLineAndConsole("On Todays menue we have a grand total of: "+pSettingList.Count+" Emitter Configs");

                    if (true)
                    {
                        int cRun = 0;
                        foreach (var myParticleSetup in pSettingList)
                        {

                            cRun++;
                            //MyLog.Default.WriteLineAndConsole("Emitter: " + cRun);
                            //if ((item.particleType == "FLAME" && _useFlame) || (item.particleType == "SMOKE" && _useSmoke))
                            {
                                if (myParticleSetup.particleName == "Missing Input") return;
                                //MyLog.Default.WriteLineAndConsole("[TFX] Adding Emitter... " + myParticleSetup.particleName);

                                if (myParticleSetup.particleType == "PARTICLE")
                                { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Particle; }
                                if (myParticleSetup.particleType == "SPRITE")
                                { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Sprite; }

                                var myDummy = GetTheDummy(myParticleSetup.Dummies);
                                if (myDummy == null)
                                {
                                    MyLog.Default.WriteLineAndConsole("[TFX] ERROR: Invalid Dummy. Cant find: " + myParticleSetup.Dummies);
                                    return;
                                }
                                myParticleSetup.dummyEmpty = myDummy;
                                if (myParticleSetup.ThrustMin == 0) myParticleSetup.ThrustMin += float.Epsilon;

                                //MyLog.Default.WriteLineAndConsole(" Offset: " + item.Offset);

                                //Add config to handler instead of each parameter
                                var temp = new MexEmitterHandler_v2(block, 90, blockParameters, myParticleSetup);

                                //MyLog.Default.WriteLineAndConsole("Made handler for " + item.particleName + "|" + item.Dummies);
                                EmitterList.Add(temp);
                            }
                        }
                    }

                    UpdateHeatup(0);
                    SetHeatEmissive(0);
                }

                //|| (MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE && MyAPIGateway.Session.IsServer)
                if (MyAPIGateway.Utilities.IsDedicated )
                {
                    if (!session.gridCache.ContainsKey(block.CubeGrid))
                    {
                        session.gridCache.Add(block.CubeGrid, 0);
                    }
                    block.CubeGrid.OnGridSplit += UpdateGrid;
                    NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
                    //MyLog.Default.WriteLineAndConsole(block.EntityId +" :Started on Server....");
                }
                else if (_setupDone)
                {
                    
                    NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
                    //if ((MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE && MyAPIGateway.Session.IsServer))
                    //{
                    //    MyLog.Default.WriteLineAndConsole(block.EntityId + " :Started on Host....");
                    //}
                    //else
                    //{
                    //    MyLog.Default.WriteLineAndConsole(block.EntityId + " :Started on Client....");
                    //}
                }

            }

            //thrustEvent.newRange += testPrintEvent;
            //UpdateHeatup(); //?

        }
        








        //ModExtension Settings
        #region Setting Parameters
        static readonly MyStringId defgroup = MyStringId.GetOrCompute("ThrusterCompData");
        
        static readonly MyStringId _sID = MyStringId.GetOrCompute("ID");
        
        static readonly MyStringId _bHideVanillaFlame = MyStringId.GetOrCompute("hideVanillaFlame");
        static readonly MyStringId _bUseSimpleSetup = MyStringId.GetOrCompute("useSimpleSetup");
        
        static readonly MyStringId _b_reverseDirection = MyStringId.GetOrCompute("_reverseDirection");
        static readonly MyStringId _bUseEmissive = MyStringId.GetOrCompute("UseEmissive");
        static readonly MyStringId _bEmissiveMaterial = MyStringId.GetOrCompute("EmissiveMaterial");
        static readonly MyStringId _bCulldist = MyStringId.GetOrCompute("CullDist");
        
        static readonly MyStringId _EmitterSetup = MyStringId.GetOrCompute("EmitterSetup");

        
        static readonly MyStringId _bThrustRateGain = MyStringId.GetOrCompute("ThrustRateGain");
        static readonly MyStringId _bThrustRateLoss = MyStringId.GetOrCompute("ThrustRateLoss");
        static readonly MyStringId _bEmissiveRateGain = MyStringId.GetOrCompute("EmissiveRateGain");
        static readonly MyStringId _bEmissiveRateLoss = MyStringId.GetOrCompute("EmissiveRateLoss");
        
        #endregion


        public void ModExtensionSetup()
        {

            //Remove All prints / OPtimize
            //MyLog.Default.WriteLineAndConsole("\n Running Setup....");
            //Check for existing settings
            bool setup = true;
            pSettingList = new List<ParticleSettings>();
            if(setup){
                var error = "Doing setup";
                try{




                    string subID = "";
                    //                    Check valid config
                    if (session.DefExtensions.TryGetString((MyDefinitionId)block.BlockDefinition, defgroup, _sID, out subID) && setup )//remove?
                    {
                        //MyLog.Default.WriteLineAndConsole("\n Why HEre!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                        float tempFloat = 0;
                        bool tempBool = false;
                        string tempString = "";
                        

                        //Simplify or put into some list function

                        //if (session.DefExtensions.TryGetBool((MyDefinitionId)block.BlockDefinition, defgroup, _bFlame, out tempBool))
                        //    _useFlame = tempBool;

                        //if (session.DefExtensions.TryGetBool((MyDefinitionId)block.BlockDefinition, defgroup, _bSmoke, out tempBool))
                        //    _useSmoke = tempBool;

                        if (session.DefExtensions.TryGetBool((MyDefinitionId)block.BlockDefinition, defgroup, _bHideVanillaFlame, out tempBool))
                            hideVanillaFlame = tempBool;

                        if (session.DefExtensions.TryGetBool((MyDefinitionId)block.BlockDefinition, defgroup, _bUseEmissive, out tempBool))
                            _useEmissive = tempBool;
                        if (session.DefExtensions.TryGetText((MyDefinitionId)block.BlockDefinition, defgroup, _bEmissiveMaterial, out tempString))
                            emissiveMat = tempString;

                        if (session.DefExtensions.TryGetFloat((MyDefinitionId)block.BlockDefinition, defgroup, _bThrustRateGain, out tempFloat))
                            ThrustRateGain = tempFloat;

                        if (session.DefExtensions.TryGetFloat((MyDefinitionId)block.BlockDefinition, defgroup, _bThrustRateLoss, out tempFloat))
                            ThrustRateLoss = tempFloat;

                        if (session.DefExtensions.TryGetFloat((MyDefinitionId)block.BlockDefinition, defgroup, _bEmissiveRateGain, out tempFloat))
                            EmissiveRateGain = tempFloat;

                        if (session.DefExtensions.TryGetFloat((MyDefinitionId)block.BlockDefinition, defgroup, _bEmissiveRateLoss, out tempFloat))
                            EmissiveRateLoss = tempFloat;

                        


                        //session.DefExtensions.TryGetFloat((MyDefinitionId)block.BlockDefinition, defgroup, _bCulldist, out _cullDist);












                        //Fugly but it works :D
                        if (session.DefExtensions.TryGetText((MyDefinitionId)block.BlockDefinition, defgroup, _EmitterSetup, out tempString)){
                            string[] tmp;

                            //Dictionary<string, string> stringSetting = new Dictionary<string, string>();
                            //MyLog.Default.WriteLineAndConsole("\n EmitterSetup:\n " + tempString);
                            //remove whitespace
                            tempString = RemoveWhitespace(tempString);
                            //MyLog.Default.WriteLineAndConsole("\n EmitterSetup no White: " + tempString);
                            //Get EmitterSetup

                            tmp = ParserText(tempString, '=');
                            



                            //Get Setting


                            //MyLog.Default.WriteLineAndConsole("||Setups: " + tmp.Length);
                            int o = 0;
                            //for (int i = 0; i < tmp.length; i++)
                            
                            for (int i = 0; i < tmp.Length; i++) {
                                if (tmp[i].Length < 3){
                                    MyLog.Default.WriteLineAndConsole("[TFX] Removed empty config section '=' ");
                                    return;
                                }
                                ParticleSettings particleSettings = new ParticleSettings();
                                var item = tmp[i];
                                o++;
                                //MyLog.Default.WriteLineAndConsole("Running Setup " + i + ".....");
                                //stringSetting.Clear();

                                //string[] dummyList= { };
                                if (item.Length > 0){

                                    string[] tmpSetting = ParserText(item, ';');
                                    string s = "";
                                    foreach (var f in tmpSetting)
                                    {
                                        s += "|" + f + "|";

                                    }
                                    //MyLog.Default.WriteLineAndConsole("||Parameters: " + s);
                                    //MyLog.Default.WriteLineAndConsole("||Parameters: " + tmpSetting.Length);

                                    //Get Data from each setting
                                    foreach (var setting in tmpSetting){
                                        if (item.Length > 0)
                                        {
                                            //int u = 1;
                                            //string[] t = { };


                                            string[] t = ParserText(setting, ':');

                                            //var d = t.ElementAt(u);
                                            //MyLog.Default.WriteLineAndConsole("||Values: " + t.Length);

                                            int currentParam = 0;

                                            //Data to particleSettings
                                            for (int n = 0; n < t.Length; n++)
                                            {
                                                string thisString = t[n];
                                                if (n == 0)
                                                {
                                                    //run some functions?
                                                    switch (thisString)
                                                    {

                                                        case "Particle":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Particle Setting");
                                                            currentParam = 1;
                                                            break;
                                                        case "Type":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Type Setting");
                                                            currentParam = 2;
                                                            break;
                                                        case "ThrustMin":
                                                            //MyLog.Default.WriteLineAndConsole("Its A ThrustMin Setting");
                                                            currentParam = 3;
                                                            break;
                                                        case "ThrustMax":
                                                            //MyLog.Default.WriteLineAndConsole("Its A ThrustMax Setting");
                                                            currentParam = 4;
                                                            break;
                                                        case "Size":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Size Setting");
                                                            currentParam = 5;
                                                            break;
                                                        case "Offset":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Offset Setting");
                                                            currentParam = 6;
                                                            break;
                                                        case "Dummies":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Dummies Setting");
                                                            currentParam = 7;
                                                            break;
                                                        case "OffsetScaling":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Dummies Setting");
                                                            currentParam = 8;
                                                            break;
                                                        case "AtmoOnly":
                                                            //MyLog.Default.WriteLineAndConsole("Its A AtmoOnly Setting");
                                                            currentParam = 9;
                                                            break;
                                                        case "VelocityScalingFrom":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Dummies Setting");
                                                            currentParam = 10;
                                                            break;
                                                        case "VelocityScalingTo":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Dummies Setting");
                                                            currentParam = 11;
                                                            break;
                                                        case "CullingTime":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Culling Setting");
                                                            currentParam = 12;
                                                            break;
                                                        case "AtmoDensityLimitMin":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Culling Setting");
                                                            currentParam = 13;
                                                            break;
                                                        case "AtmoDensityLimitMax":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Culling Setting");
                                                            currentParam = 14;
                                                            break;
                                                        case "AltitudeLimitMin":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Culling Setting");
                                                            currentParam = 15;
                                                            break;
                                                        case "AltitudeLimitMax":
                                                            //MyLog.Default.WriteLineAndConsole("Its A Culling Setting");
                                                            currentParam = 16;
                                                            break;
                                                        default:
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    switch (currentParam)
                                                    {
                                                        case 1:
                                                            particleSettings.particleName = thisString;
                                                            break;
                                                        case 2:
                                                            particleSettings.particleType = thisString;
                                                            break;
                                                        case 3:
                                                            float.TryParse(thisString, out particleSettings.ThrustMin);
                                                            break;
                                                        case 4:
                                                            float.TryParse(thisString, out particleSettings.ThrustMax);
                                                            break;
                                                        case 5:
                                                            float.TryParse(thisString, out particleSettings.Size);
                                                            break;
                                                        case 6:
                                                            float.TryParse(thisString, out particleSettings.Offset);
                                                            break;
                                                        case 7:
                                                            if (thisString == "none") { particleSettings.Dummies = "flame"; }
                                                            else
                                                            {
                                                                particleSettings.Dummies = thisString;
                                                            }

                                                            break;
                                                        case 8:
                                                            float.TryParse(thisString, out particleSettings.OffsetScaling);
                                                            break;
                                                        case 9:
                                                            particleSettings.AtmoOnly = (thisString != "FALSE");
                                                            //if (thisString=="FALSE") particleSettings.AtmoOnly = false;
                                                            //if (thisString=="TRUE") particleSettings.AtmoOnly = true;
                                                            //MyLog.Default.WriteLineAndConsole("Setting AtmoOnly: "+particleSettings.AtmoOnly);
                                                            break;
                                                        case 10:
                                                            
                                                            float.TryParse(thisString, out particleSettings.VelScalingFrom);
                                                            //MyLog.Default.WriteLineAndConsole("Setting AtmoOnly: " + particleSettings.VelScalingFrom);
                                                            break;
                                                        case 11:
                                                            float.TryParse(thisString, out particleSettings.VelScalingTo);
                                                            //MyLog.Default.WriteLineAndConsole("Setting AtmoOnly: "+ thisString);
                                                            //MyLog.Default.WriteLineAndConsole("Setting AtmoOnly: " + particleSettings.VelScalingTo);
                                                            break;
                                                        case 12:
                                                            {
                                                                float.TryParse(thisString, out particleSettings.cullTimer);
                                                                //MyLog.Default.WriteLineAndConsole("Setting CullingTime: " + thisString);
                                                            }
                                                            break;
                                                        case 13:
                                                            float.TryParse(thisString, out particleSettings.AtmoDensityLimitMin);
                                                            break;
                                                        case 14:
                                                            float.TryParse(thisString, out particleSettings.AtmoDensityLimitMax);
                                                            break;
                                                        case 15:
                                                            int.TryParse(thisString, out particleSettings.AltitudeLimitMin);
                                                            break;
                                                        case 16:
                                                            int.TryParse(thisString, out particleSettings.AltitudeLimitMax);
                                                            break;
                                                        default:
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (particleSettings.Dummies == "none") particleSettings.Dummies = "thruster_flame_01"; //incase of missing setting in config use vanilla
                                //MyLog.Default.WriteLineAndConsole("\n Dummies: " + particleSettings.Dummies);
                                string[] dummyInUse = ParserText(particleSettings.Dummies, ',');
                                if (particleSettings.Dummies != "thruster_flame_01")//been edited
                                {

                                    foreach (var dummyName in dummyInUse)
                                    {
                                        var temp = new ParticleSettings
                                        {
                                            particleName = particleSettings.particleName,
                                            particleType = particleSettings.particleType,
                                            ThrustMin = particleSettings.ThrustMin,
                                            ThrustMax = particleSettings.ThrustMax,
                                            Size = particleSettings.Size,
                                            Offset = particleSettings.Offset,
                                            OffsetScaling = particleSettings.OffsetScaling,
                                            Dummies = dummyName,
                                            AtmoOnly = particleSettings.AtmoOnly,
                                            VelScalingFrom = particleSettings.VelScalingFrom,
                                            VelScalingTo = particleSettings.VelScalingTo,
                                            subeTypeName = block.BlockDefinition.SubtypeId,
                                            cullTimer = particleSettings.cullTimer,
                                            AtmoDensityLimitMin = particleSettings.AtmoDensityLimitMin,
                                            AtmoDensityLimitMax = particleSettings.AtmoDensityLimitMax,
                                            AltitudeLimitMin = particleSettings.AltitudeLimitMin,
                                            AltitudeLimitMax = particleSettings.AltitudeLimitMax,
                                        };
                                        pSettingList.Add(temp);

                                    }
                                }
                                else
                                {
                                    particleSettings.subeTypeName = block.BlockDefinition.SubtypeId;
                                    //session.ExistingSetups.Add(particleSettings.subeTypeName, particleSettings);
                                    pSettingList.Add(particleSettings);
                                }
                                particleSettings = null;
                                //if (stringSetting.ContainsKey("Dummies"))
                                //{
                                //    MyLog.Default.WriteLineAndConsole("\n FoundDummy");
                                //    string dum;
                                //    stringSetting.TryGetValue("Dummies", out dum);
                                //    dummyList = ParserText(dum, ',');
                                //    foreach (var y in dummyList)
                                //    {
                                //        MyLog.Default.WriteLineAndConsole("\n Dummies: " + y );
                                //    }
                                //}

                                //if (dummyList.Length<1)
                                //{

                                //}
                            }
                        }
                        //MyLog.Default.WriteLineAndConsole("We have: " + pSettingList.Count + " particle systems added");

                        ///smokeLimitList = ParserFloat(tempString, ',');

                        //if(staticSettingList.ContainsKey(block.BlockDefinition.SubtypeId))
                        //MyLog.Default.WriteLineAndConsole("\n WeGot The subtype, Skipping...");
                        //else{
                        //    MyLog.Default.WriteLineAndConsole("\n Does not exist,Adding new..."); 
                        //        staticSettingList.Add(block.BlockDefinition.SubtypeId, pSettingList);
                        //}

                        
                        _setupDone = true;
                    }
                    //session.ExistingSetups.Add(block.BlockDefinition.SubtypeId, pSettingList);
                    //MyLog.Default.WriteLineAndConsole("Adding new  list....");

                }
                catch
                {
                    MyAPIGateway.Utilities.ShowNotification("Error getting Settings: " + error, 10000, MyFontEnum.Blue);
                }
            }
        }

       
        //silly me
        //public void SetInRange(bool value)
        //{
        //    _isInRange = value;
        //    if (!_isInRange) notInRangeRemove = true;
        //}
        
        //public void RunIntensity(bool s)
        //{
        //    if (s) UpdateIntensity();
        //}

        public void AddEvents()//Run when doing Setup, true = add, false = remove
        {
            
            
        }

        public void RemoveEvents()//Run when doing Setup, true = add, false = remove
        {
           
        }

        


        public override void UpdateBeforeSimulation100()
        {
            if (block != null)
            {
                //List<ITerminalProperty> prop = new List<ITerminalProperty>();
                //block.GetProperties(prop, null);
                //MyLog.Default.WriteLineAndConsole("---Block---");
                //foreach (var item in prop)
                //{
                //    MyLog.Default.WriteLineAndConsole("--- " + item.Id + " | "+item.ToString());
                    
                //}
                //MyLog.Default.WriteLineAndConsole("---Block---");
                //if (block.GetProperty("HideThrustFlames") != null)
                //{
                //    MyLog.Default.WriteLineAndConsole("[Recolorable Detected]");
                //}
                //else
                //{
                //    MyLog.Default.WriteLineAndConsole("[Recolorable NotDetected]");
                //}
            }
            #region
            //if (!_setupDone)
            //{
            //    if (session == null ||
            //    MyAPIGateway.Utilities.IsDedicated &&
            //    (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer))
            //        return;

            //    //AddEvents();

            //    //Can I make it check for existing setups instead of looping through the sama subtypes?
            //    //bool setupAlreadyDone = false;

            //    //setupAlreadyDone = session.settingCache.ContainsKey(block.BlockDefinition.SubtypeId);

            //            ModExtensionSetup();


            //    //setupAlreadyDone=true







            //    if (hideVanillaFlame)
            //    {

            //        thruster.BlockDefinition.FlameFullColor = new Vector4(0);
            //        thruster.BlockDefinition.FlameIdleColor = new Vector4(0);
            //        thruster.BlockDefinition.FlameVisibilityDistance = 0;
            //        thruster.Render.UpdateFlameAnimatorData();
            //    }


            //    //Init block in session
            //    var blockSID = block.BlockDefinition.SubtypeId;


            //    //Valid subtypes for the session when scaning nearby blocks
            //    if (!session.blockSubTypes.ContainsKey(blockSID))
            //    {
            //        MyLog.Default.WriteLineAndConsole("[Adding Subtype]");
            //        session.blockSubTypes.Add(blockSID, 1);
            //    }
            //    else
            //    {
            //        //update hwo many blocks exist, waste.. maybe :D
            //        var value = 0;
            //        session.blockSubTypes.TryGetValue("blockSID", out value);
            //        //MyLog.Default.WriteLineAndConsole("[Adding Value]");
            //        session.blockSubTypes["blockSID"] = value++;

            //    }

            //    myGrid = block.CubeGrid;
            //    BoundingSphereD theSphere = new BoundingSphereD { Radius = 200 };
            //    theSphere.Center = myGrid.WorldAABB.Center;//; WorldAABB.Center;

            //    if (!session.gridCache.ContainsKey(myGrid))
            //    {
            //        //MyLog.Default.WriteLineAndConsole("[Adding Block]");
            //        session.gridCache.Add(myGrid, theSphere);
            //    }

            //    //_thrust = block.CurrentThrust;
            //    _maxThrust = block.MaxThrust;





            //    //session.listedSubtypes.Add(block.BlockDefinition.SubtypeId);

            //    try
            //    {
            //        dummy = new Dictionary<string, IMyModelDummy>();
            //        dummy.Clear();
            //        var tmp1 = new Dictionary<string, IMyModelDummy>();

            //        block.Model.GetDummies(tmp1);

            //        //Expand Usage
            //        foreach (var item in tmp1)
            //        {
            //            //MyLog.Default.WriteLineAndConsole("[Dummies: key|value:]" + item.Key + " | " + item.Value);
            //            if (item.Key.Contains("flame"))
            //            {
            //                //var t = item.Value;
            //                //t.Matrix.SetRotationAndScale(1 / t.Matrix.);
            //                fxDummy = item.Value;
            //                dummy.Add(item.Key, item.Value);
            //            }
            //        }

            //    }
            //    catch (Exception)
            //    {

            //        throw;
            //    }





            //    //Add el particle handler 2
            //    //MyLog.Default.WriteLineAndConsole("On Todays menue we have a grand total of: "+pSettingList.Count+" Emitter Configs");

            //    if (true)
            //    {
            //        int cRun = 0;
            //        foreach (var myParticleSetup in pSettingList)
            //        {

            //                cRun++;
            //            //MyLog.Default.WriteLineAndConsole("Emitter: " + cRun);
            //            //if ((item.particleType == "FLAME" && _useFlame) || (item.particleType == "SMOKE" && _useSmoke))
            //            {
            //                if (myParticleSetup.particleName == "Missing Input") return;


            //                if (myParticleSetup.particleType == "PARTICLE")
            //                { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Particle; }
            //                if (myParticleSetup.particleType == "SPRITE")
            //                { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Sprite; }

            //                var myDummy = GetTheDummy(myParticleSetup.Dummies);
            //                if (myDummy == null) {
            //                    MyLog.Default.WriteLineAndConsole("\n Invalid Dummy. Cant find: " + myParticleSetup.Dummies); 
            //                    return; 
            //                }
            //                myParticleSetup.dummyEmpty = myDummy;
            //                if (myParticleSetup.ThrustMin == 0) myParticleSetup.ThrustMin +=float.Epsilon ;

            //                //MyLog.Default.WriteLineAndConsole(" Offset: " + item.Offset);

            //                //Add config to handler instead of each parameter

            //                var temp = new MexEmitterHandler_v2(block,90,blockParameters,myParticleSetup);

            //                //MyLog.Default.WriteLineAndConsole("Made handler for " + item.particleName + "|" + item.Dummies);
            //                EmitterList.Add(temp);
            //            }
            //        }
            //    }



            //    UpdateHeatup(0);
            //    SetHeatEmissive(0);







            //}
            #endregion

            if (!_setupDone) return;

            bool tmp = false;
            tmp = (session.gidsInRange.ContainsKey(myGrid));
            if (tmp != IsVisible)
            {
                if (tmp)turningVisibleTrigger = true;
                IsVisible = tmp;
                blockParameters.UpdateVisible(tmp, block.Render.GetRenderObjectID());
                //blockParameters.IsClose = tmp;
            }
            if (IsVisible)
            {
                tmp = CheckIfClose();
                if (tmp != IsClose)
                {
                    IsClose = tmp;
                    blockParameters.IsClose = tmp;
                }
                tmp = session.gidsInAtmo.ContainsKey(myGrid);
                if (session.gidsInAtmo.ContainsKey(myGrid))
                {
                    var atmoData = session.gidsInAtmo[myGrid];
                    InAtmo = tmp;
                    blockParameters.UpdateAtmo(atmoData);
                }
                tmp = block.IsWorking;
                //if (tmp != IsWorking)
                //{
                //    IsWorking = tmp;
                //    blockParameters.UpdateWorking(tmp);
                //}
                if (!IsWorking && IsVisible)
                {
                    UpdateWorking(block);
                }
                if (IsWorking && IsVisible)
                {
                    //MyLog.Default.WriteLineAndConsole("[TFX] - Check AUtopilot....Grid:" + block.CubeGrid.EntityId.ToString());
                    //if(MyEntities.GetEntities().fin block.CubeGrid.EntityId) 
                    AutoControlled = session.AutoPilotGrids.ContainsKey(block.CubeGrid);
                    //MyLog.Default.WriteLineAndConsole("[TFX] - Check AUtopilot....Done - "+ AutoControlled);
                }
                
            }

            try //nani?!
            {
                
                if (blockParameters.IsVisible)
                {

                    if (!blockParameters.IsClose)
                    {

                        //UpdateHeatup();
                    }


                }
                //thrustEvent.newRange -= testPrintEvent;



            }
            catch { MyAPIGateway.Utilities.ShowNotification("[ " + " Failed 100up" + "]", 1000, MyFontEnum.Blue); }

            //MyAPIGateway.Utilities.ShowNotification("[ " +thrustEvent.oldIsWorking+" "+ thrustEvent.oldThrust+ " "+ _isInRange + " "+ thrustEvent.oldIsClose + " " +  "]", 1000, MyFontEnum.Blue);


            base.UpdateBeforeSimulation100();
        }
       public IMyModelDummy GetTheDummy(string thisDummy)
        {

            //make to a cached list
            var temp = new Dictionary<string, IMyModelDummy>();
            temp.Clear();
            

            block.Model.GetDummies(temp);

            //Expand Usage
            foreach (var item in temp)
            {
                //MyLog.Default.WriteLineAndConsole("[Dummies: key|value:]" + item.Key + " | " + item.Value);
                if (item.Key.Contains(thisDummy))
                {
                    //var t = item.Value;
                    //t.Matrix.SetRotationAndScale(1 / t.Matrix.);
                    return item.Value;
                }
            }

            return null;
        }

        
        public bool CheckIfClose()
        {
            //_isCloseRange = ;
            return session.CheckCloseRange(myGrid);
        }

        public void SpawnHandler()
        {
            if (true)
            {
                int cRun = 0;
                foreach (var myParticleSetup in pSettingList)
                {

                    cRun++;
                    //MyLog.Default.WriteLineAndConsole("Emitter: " + cRun);
                    //if ((item.particleType == "FLAME" && _useFlame) || (item.particleType == "SMOKE" && _useSmoke))
                    {
                        if (myParticleSetup.particleName == "Missing Input") return;
                        MyLog.Default.WriteLineAndConsole("[TFX] Adding Emitter... " + myParticleSetup.particleName);

                        if (myParticleSetup.particleType == "PARTICLE")
                        { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Particle; }
                        if (myParticleSetup.particleType == "SPRITE")
                        { myParticleSetup.emitterType = MexEmitterHandler_v2.EmitterType.Sprite; }

                        var myDummy = GetTheDummy(myParticleSetup.Dummies);
                        if (myDummy == null)
                        {
                            MyLog.Default.WriteLineAndConsole("[TFX] ERROR: Invalid Dummy. Cant find: " + myParticleSetup.Dummies);
                            return;
                        }
                        myParticleSetup.dummyEmpty = myDummy;
                        if (myParticleSetup.ThrustMin == 0) myParticleSetup.ThrustMin += float.Epsilon;

                        //MyLog.Default.WriteLineAndConsole(" Offset: " + item.Offset);

                        //Add config to handler instead of each parameter

                        var temp = new MexEmitterHandler_v2(block, 90, blockParameters, myParticleSetup);

                        MyLog.Default.WriteLineAndConsole("Made handler ");
                        EmitterList.Add(temp);
                    }
                }
            }
        }


        public void RunThrust()
        {
            //if (thrustEvent.OnThrustChanged(_thrustValue))
            {

                blockParameters.UpdateThrust(_thrustValue);
                _ThrustValueLastTrigger = _thrustValue;
                //session.eventThrustChangedCounter++;
                runIntensityUpdate = true;
                if (_useEmissive) runHeatup = true;
                if (_useEmissive) heatupIsDone = false;

            }
        }

        public void RunIntensity(float flame, float thrust)
        {

            float diff = 0;
            bool isdiff = false;

            if (turningVisibleTrigger||(ValueThresholdTotal(thrust, flame, 1, 0.05f, out isdiff, out diff)
                    || (thrust < 0.005f && flame > 0.005f)
                    || (thrust > 1 - 0.005f && flame < 1 - 0.005f)))
            {
                session.eventIntensityChangedCounter++;

                //float t = IntensityUpdater(_thrustValue, blockParameters.FlameIntensity, 40.0f / 60, 8.0f / 60);

                float currInten = flame;
                float  temp =0;
                {
                    float newIntensity = 0;
                    if (currInten < thrust)
                    {
                        newIntensity = VRageMath.MathHelper.Lerp(currInten, thrust, (8.0f / 60));
                    }

                    else if (currInten > thrust)
                    {
                        newIntensity = VRageMath.MathHelper.Lerp(currInten, thrust, (40.0f / 60));
                    }

                    temp = (float)MathHelper.Clamp(newIntensity, 0, 1); //Why does this not work? goes to -1.6e^18
                    if (temp < 0.01f) { temp = 0.0f; }
                    if (temp >= 0.995f) { temp = 1.0f; }
                    //MyAPIGateway.Utilities.ShowNotification("_flameIntensity: " + _flameIntensity , 1, MyFontEnum.Blue);
                }

                float t = temp;

                blockParameters.UpdateIntensity(t);

                _flameIntensityLast = flame;
            }
            else
            {
                //MyLog.Default.WriteLineAndConsole("Update FlameIntensity | Stopped ");
                runIntensityUpdate = false;
            }
        }
        
        public bool ValueThreshold(float newThrust, float lastThrust)
        {
            //10%  dif since last based on total current value
            //If its X %  or larger diff since last
            float threshold = 0.1f;
            bool b = false;
            float diff = (newThrust / lastThrust);
            if (diff >= 1 + threshold || diff <= 1 - threshold) b = true;

            return b;


        }
        public bool ValueThresholdTotal(float newThrust, float lastThrust, float maxValue,float threshold, out bool b,out float diff)
        {
            //10%  dif since last based on total
            //threshold = 0.05f;
             b = false;
            float thresholdMax = maxValue*threshold;
            diff = Math.Abs(newThrust - lastThrust);// ((newThrust + float.Epsilon) / (lastThrust+float.Epsilon));

            if (diff >=thresholdMax ) b = true;

            return b;
        }


        public void UpdateWorking(IMyCubeBlock b)
        {
            IsWorking = b.IsWorking;

            blockParameters.UpdateWorking(b.IsWorking, block.Render.GetRenderObjectID());
        }
        public void UpdateRecolorable(IMyTerminalBlock b)
        {
            bool recolorableChanged = false;


            recolorableChanged =
                property_hide_flame != b.GetProperty("HideThrustFlames")
                || property_full_flame_color != b.GetProperty("FlameFullColorOverride")
                || property_idle_flame_color != b.GetProperty("FlameIdleColorOverride");

            if (recolorableChanged)
            {
                //Run update action
                blockParameters.UpdateColor();
            }
        }
        public void UpdateGrid(IMyCubeGrid a, IMyCubeGrid b)
        {
            if (!session.gridCache.ContainsKey(a) && block.CubeGrid == a)
            {
                session.gridCache.Add(a, 0);
                MyLog.Default.WriteLineAndConsole("[Adding Grid]");
            }
            if (!session.gridCache.ContainsKey(b) && block.CubeGrid == b)
            {
                session.gridCache.Add(b, 0);
                MyLog.Default.WriteLineAndConsole("[Adding Grid]");
            }

        }
        public void UpdateStationGid(IMyCubeGrid a, bool b)
        {
            if (!session.gridCache.ContainsKey(a) && block.CubeGrid == a)
            {
                session.gridCache.Add(a, 0);
                MyLog.Default.WriteLineAndConsole("[Adding Grid]");
            }

        }

        bool AutoControlled = false;

        public override void UpdateBeforeSimulation()
        {
            _thrustValue = 0.0f;

            
            //Possible way to fix missig effect on ai ships due to how they update thrust
            //Just do thrust = intensity 
            //if(block.CubeGrid.ControlSystem.CurrentShipController.IsAutopilotControlled)

            //if (IsVisible && !IsWorking && _setupDone)
            //{
            //    if (!firstRun) firstRun = true; // to make it reset on 
            //}

            if (firstRun && _setupDone && IsWorking)
            {
                UpdateHeatup(FlameIntensity);
                SetHeatEmissive(HeatupIntensity);
                LastColorIntensity = 1;
                //firstRun = false;
                blockParameters.UpdateIntensity(0.0f);
                runIntensityUpdate = true;
                UpdateWorking(block);
                if (_useEmissive) runHeatup = true;
                if (_useEmissive) heatupIsDone = false;
                firstRun = false;

                if (_checkRecolorable)
                {
                    if (block.GetProperty("HideThrustFlames") != null)
                    {
                        property_hide_flame = block.GetProperty("HideThrustFlames");
                        property_full_flame_color = block.GetProperty("FlameFullColorOverride");
                        property_idle_flame_color = block.GetProperty("FlameIdleColorOverride");


                        block.PropertiesChanged += UpdateRecolorable;
                        blockParameters.UpdateColor();
                        //MyLog.Default.WriteLineAndConsole("[Recolorable Detected]");
                    }
                    else
                    {
                        //MyLog.Default.WriteLineAndConsole("[Recolorable NotDetected]");
                    }
                }


                //MyLog.Default.WriteLineAndConsole("---First RUN");

            }


            
            //(block as MyThrust).thrust
            //(block).GridThrustDirection
            //block.CubeGrid.ControlSystem.
            //(block.CubeGrid).ControlSystem.CurrentShipController.IsAutopilotControlled. .auto.FinalThrust .EntityThrustComponent.FinalThrust

            //add not working
            if (IsVisible && IsWorking && _setupDone)// || (currentDelay > 5))
            {
                bool forceTrigger = true;
                //MyLog.Default.WriteLineAndConsole("---Normal");
                //_ThrustValueLastTrigger = _thrustValue;
                //MyLog.Default.WriteLineAndConsole("UpdateValue!!!!!: " + f);
                if (AutoControlled)
                {
                    float autoPilotThrust = 0.0f;

                    if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Session.IsServer)
                    {   
                        //Synced thrust
                        if(session.ThrusterForce.ContainsKey(thruster.EntityId))
                            autoPilotThrust = session.ThrusterForce[thruster.EntityId] / 100;
                        //MyLog.Default.WriteLineAndConsole("ThrustMP: " + autoPilotThrust.ToString());
                    }
                    else
                    {
                        //Client thrust
                        autoPilotThrust = block.CurrentThrustPercentage / 100;
                        //MyLog.Default.WriteLineAndConsole("ThrustSP: " + autoPilotThrust.ToString());
                    }


                    //blockParameters.avgThrust = (autoPilotThrust * 0.1f + _thrustValue * 0.9f); //Average over 5 updates
                    _thrustValue = autoPilotThrust;
                    //MyAPIGateway.Utilities.ShowNotification("AvgThrust: " + blockParameters.avgThrust, 1, MyFontEnum.Blue);
                    //MyLog.Default.WriteLineAndConsole("AvgThrust: " + blockParameters.avgThrust.ToString());

                    //If player on dedi
                    //Get synced thruster value from session


                }
                else
                {
                    _thrustValue = block.CurrentThrustPercentage / 100;
                    
                    //MyLog.Default.WriteLineAndConsole("Thrust: " + block.CurrentThrust.ToString());
                }
                forceTrigger = (Math.Abs(_thrustValue - _ThrustValueLastTrigger) >= (0.01f));
                if (_thrustValue <= 0.01f) _thrustValue = 0;

                

                if (firstRun
                    || turningVisibleTrigger
                    || forceTrigger
                    || (_thrustValue < 0.005f && _ThrustValueLastTrigger > 0.005f)
                    || (_thrustValue > 1 - 0.005f && _ThrustValueLastTrigger < 1 - 0.005f))
                {

                    _ThrustValueLastTrigger = _thrustValue;
                    session.eventThrustChangedCounter++;
                    runIntensityUpdate = true;

                    currentDelay = 0;

                    
                }

                //Calc Thrust Intensity

                bool validLimit = false;
                validLimit = (Math.Abs(_thrustValue - FlameIntensity) > 0.005f) && currentIntensityDelay%5==0;// (1 * 0.025f));

                //Intensity
                if (turningVisibleTrigger
                    || firstRun
                    || validLimit
                    || (_thrustValue < 0.005f && FlameIntensity > 0.005f)
                    || (_thrustValue > 1 - 0.005f && FlameIntensity < 1 - 0.005f))
                {
                    currentIntensityDelay = 0;

                    float currentIntensity = FlameIntensity;
                    float temp = 0;
                    float newIntensity = 0;
                    if (currentIntensity < _thrustValue)
                    {

                        newIntensity = VRageMath.MathHelper.Lerp(currentIntensity, _thrustValue, ThrustRateGain);

                    }

                    else if (currentIntensity > _thrustValue)
                    {

                        newIntensity = VRageMath.MathHelper.Lerp(currentIntensity, _thrustValue, ThrustRateLoss);

                    }


                    temp = (float)MathHelper.Clamp(newIntensity, 0, 1); //Why does this not work? goes to -1.6e^18
                    if (temp < 0.01f) { temp = 0.0f; }
                    if (temp >= 0.995f) { temp = 1.0f; }
                    //MyAPIGateway.Utilities.ShowNotification("_flameIntensity: " + _flameIntensity , 1, MyFontEnum.Blue);




                    FlameIntensity = temp;


                    //MyLog.Default.WriteLineAndConsole("Updating Block Render ID:  " + block.Render.GetRenderObjectID().ToString());
                    _flameIntensityLast = FlameIntensity;
                    blockParameters.UpdateIntensity(FlameIntensity);
                }
                else
                {
                    //MyLog.Default.WriteLineAndConsole("Update FlameIntensity | Stopped ");
                    runIntensityUpdate = false;
                }

                currentIntensityDelay++;

                //Calc Emissive Intensity


                validLimit = false;
                validLimit = (Math.Abs(HeatupIntensity - FlameIntensity) >= (1 * 0.05f));

                //UpdateHeat
                //ValueThresholdTotal - true if difference is more than 0.05f of the total 1.0
                if ((_useEmissive&&(
                    turningVisibleTrigger
                    || validLimit
                    || (FlameIntensity < 0.005f && HeatupIntensity > 0.005f)
                    || (FlameIntensity > 1 - 0.005f && HeatupIntensity < 1 - 0.005f))))
                //if (runHeatup)
                {
                    runHeatup = false;
                    HeatupIntensity = IntensityUpdater(FlameIntensity, HeatupIntensity, EmissiveRateGain, EmissiveRateLoss);
                    
                    float heatIntensDiff = (float)Math.Round(Math.Abs(HeatupIntensity - FlameIntensity), 3);

                    blockParameters.HeatupIntensity = HeatupIntensity;
                }
                _oldThrustValue = (float)Math.Round(_thrustValue, 3);

                turningVisibleTrigger = false;
                
            } 
            else if (IsVisible && !IsWorking && _setupDone)
            {
                //MyLog.Default.WriteLineAndConsole("---NormalNotWorking");
                //if(!firstRun) firstRun = true; // to make it reset on 
                //blockParameters.RemoveMe();
                firstRun = true;
                if (blockParameters.FlameIntensity > 0.0f)
                {
                    FlameIntensity = 0.0f;
                    _flameIntensityLast = FlameIntensity;
                    blockParameters.UpdateIntensity(FlameIntensity);

                }

                if (_useEmissive && HeatupIntensity > 0.0f )
                {
                    HeatupIntensity = IntensityUpdater(0.0f, HeatupIntensity, EmissiveRateGain, EmissiveRateLoss);
                }
            }

            currentDelay++;

            

            //base.UpdateBeforeSimulation();
        }
        public void UpdateThrust(IMyThrust b,float f)
        {
            _ThrustValueLastTrigger = _thrustValue;
            MyLog.Default.WriteLineAndConsole("UpdateValue!!!!!: " + f);
            _thrustValue = b.CurrentThrust / _maxThrust;
            if (_thrustValue <= 0.01f) _thrustValue = 0;
        }
        

        public override void UpdateBeforeSimulation10()
        {
            //change to thrust.OnChanged
            if (MyAPIGateway.Utilities.IsDedicated || (MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE && MyAPIGateway.Session.IsServer))
            {
                //MyLog.Default.WriteLineAndConsole("Trying Syncing value.... ");
                if (session.AutoPilotGrids.ContainsKey(block.CubeGrid) && (block.CurrentThrustPercentage != lastSyncValue))
                {
                    lastSyncValue = block.CurrentThrustPercentage;
                    //MyLog.Default.WriteLineAndConsole("Syncing value!!!!!: "+ block.CurrentThrustPercentage);
                    session.Networking.RelayToClients(new PacketSimpleExample(block.EntityId, lastSyncValue), null);
                }
                if (MyAPIGateway.Utilities.IsDedicated) return;
            }

            //if (IsVisible)
            //{
            //    //MyLog.Default.WriteLineAndConsole("Update  Visible");
            //}
            

            if (IsVisible && !IsClose && IsWorking)
            {
                bool forceTrigger = false;
                //MyLog.Default.WriteLineAndConsole("---Normal");
                //_ThrustValueLastTrigger = _thrustValue;
                //MyLog.Default.WriteLineAndConsole("UpdateValue!!!!!: " + f);
                if (AutoControlled)
                {
                    float autoPilotThrust = 0.0f;

                    if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Session.IsServer)
                    {
                        //Synced thrust
                        if (session.ThrusterForce.ContainsKey(thruster.EntityId))
                            autoPilotThrust = session.ThrusterForce[thruster.EntityId] / 100;
                        //MyLog.Default.WriteLineAndConsole("ThrustMP: " + autoPilotThrust.ToString());
                    }
                    else
                    {
                        //Client thrust
                        autoPilotThrust = block.CurrentThrustPercentage / 100;
                        //MyLog.Default.WriteLineAndConsole("ThrustSP: " + autoPilotThrust.ToString());
                    }


                    //blockParameters.avgThrust = (autoPilotThrust * 0.1f + _thrustValue * 0.9f); //Average over 5 updates
                    _thrustValue = autoPilotThrust;
                    //MyAPIGateway.Utilities.ShowNotification("AvgThrust: " + blockParameters.avgThrust, 1, MyFontEnum.Blue);
                    //MyLog.Default.WriteLineAndConsole("AvgThrust: " + blockParameters.avgThrust.ToString());

                    //If player on dedi
                    //Get synced thruster value from session


                }
                else
                {
                    _thrustValue = block.CurrentThrustPercentage / 100;

                    //MyLog.Default.WriteLineAndConsole("Thrust: " + block.CurrentThrust.ToString());
                }

                forceTrigger = (Math.Abs(_thrustValue - _ThrustValueLastTrigger) >= (0.01f));

                if (firstRun
                    || turningVisibleTrigger
                    || forceTrigger
                    || (_thrustValue < 0.005f && _ThrustValueLastTrigger > 0.005f)
                    || (_thrustValue > 1 - 0.005f && _ThrustValueLastTrigger < 1 - 0.005f))
                {

                    _ThrustValueLastTrigger = _thrustValue;
                    session.eventThrustChangedCounter++;
                    runIntensityUpdate = true;

                    //runThrust();
                    currentDelay = 0;

                    firstRun = false;
                    //blockParameters.lastThrust = blockParameters.Thrust;

                }
            }
            else if (!IsWorking && IsVisible)
            {
                //MyLog.Default.WriteLineAndConsole("Update NotWorking Visible");
                if (blockParameters.FlameIntensity > 0.0f)
                {
                    FlameIntensity = 0.0f;
                    _flameIntensityLast = FlameIntensity;
                    //MyLog.Default.WriteLineAndConsole("---NormalSetIntensity");
                    blockParameters.UpdateIntensity(FlameIntensity);
                }

                if (_useEmissive && HeatupIntensity > 0.0f)
                {
                    HeatupIntensity = IntensityUpdater(0.0f, HeatupIntensity, EmissiveRateGain, EmissiveRateLoss);
                }

            }
            //float diffen = 0;
            //bool validDiff = false;
            //combine to one func
            bool validLimit = false;
            validLimit = (Math.Abs(LastColorIntensity - HeatupIntensity) >= (0.005f));


            if (IsVisible)
            if (_useEmissive 
                    && (validLimit
                    ||runHeatup
                    || (FlameIntensity < 0.005f && HeatupIntensity > 0.005f)
                    || (FlameIntensity > 1 - 0.005f && HeatupIntensity < 1 - 0.005f)))
            {
                    runHeatup = false;
                    SetHeatEmissive(HeatupIntensity);
                    LastColorIntensity = HeatupIntensity;


            }

        }

        public void SetHeatEmissive(float i)
        {
            //session.eventHeatUpChangedCounter++;
            if (i < 0.035f + float.Epsilon)
            {
                i = 0;
                heatupIsDone = true;
                //MyLog.Default.WriteLineAndConsole("heatupIsDone | Done");
            }
            //session.eventHeatUpChangedCounter++;
            heatColor = new Color(i, 0.9f * (i + i), 0.9f * (i + i), i);
            //if (lastheatColor != heatColor)
            //MyAPIGateway.Utilities.ShowNotification("heatColor: " + heatColor, 1, MyFontEnum.Blue);
            
            //_currentIntensity = 0.0001f;
            block.SetEmissiveParts(emissiveMat, heatColor, i * 0.9f);
            lastheatColor = heatColor;
        }
        
        //Unused
        public void UpdateHeatup(float tV)
        {
            //lowprio: optimize this mess bellow

            var temp = _currentIntensity;

            var t1 = MathHelper.Clamp(temp + (2.0f / 60), 0, tV);
            var t2 = MathHelper.Clamp(temp - (1.0f / 180), tV, 1);

            //Rework this... kinda messy if if if if if
            //It works but what did i do...
            if (temp < tV)
            {
                //MyLog.Default.WriteLineAndConsole("Update Heatup | 1");
                temp = VRageMath.MathHelper.Lerp(temp, tV, (0.005f));
                //temp = t1;
            }

            else if (temp > tV)
            {
                //MyLog.Default.WriteLineAndConsole("Update Heatup | 2");
                temp = VRageMath.MathHelper.Lerp(temp, tV, 0.005f);
                //temp = t2;
            }
            else {
                    //MyLog.Default.WriteLineAndConsole("Update Heatup | Failed");
                 }


            temp = (float)MathHelper.Clamp(temp, 0, 1); //Why does this not work? goes to -1.6e^18
            //if (temp >=0.975f) { temp = 1.0f; }
            if (temp > 0.999f) { temp = 1.0f; }
            if (temp <= 0.01f) { temp = 0.0f; }

            //if (Math.Abs(_currentIntensity - temp) >= 0.005f)
            //{
                _currentIntensity = temp;
        }

        //Unused
        public float IntensityUpdater(float refvalue, float currValue, float downRate, float upRate)
        {

            //add compare to last value



            //cache it
            float thrClamp = 0;
            if (currValue - (0.05f) < refvalue) thrClamp = refvalue;

            var t = MathHelper.Clamp(currValue + (0.1f), 0, refvalue);
            var t2 = MathHelper.Clamp(currValue - (0.05f), 0, 1);
            float old = currValue;

            float temp = 0;
            temp = currValue;
            {
                float newIntensity = 0;
                if (currValue < refvalue)
                {
                    //newIntensity = _flameIntensity+(4.0f / 60.0f);
                    //newIntensity = _flameIntensity + (8.0f / 60);
                    newIntensity = VRageMath.MathHelper.Lerp(currValue, refvalue, (upRate));
                    //newIntensity = t;
                }

                else if (currValue > refvalue)
                {
                    //newIntensity = _flameIntensity-(10.0f / 60.0f);
                    newIntensity = VRageMath.MathHelper.Lerp(currValue, refvalue, (downRate));
                    //newIntensity = t2;
                }
                //clamp ^ instead
                //MyLog.Default.WriteLineAndConsole(newIntensity.ToString());
                currValue = (float)MathHelper.Clamp(newIntensity, 0, 1); //Why does this not work? goes to -1.6e^18
                if (currValue < 0.005f+float.Epsilon) { currValue = 0.0f; }
                if (currValue > 0.995f) { currValue = 1.0f; }
                //MyAPIGateway.Utilities.ShowNotification("_flameIntensity: " + _flameIntensity , 1, MyFontEnum.Blue);
                //if (_flameIntensity > 1) { _flameIntensity = 1.0f; }
                //if (_flameIntensity < 0.00001f) { _flameIntensity = 0.0f; }


            }
            temp = currValue;

            try
            {

                return currValue;// thrustEvent.fIntensity(_flameIntensity);
            }
            catch
            {
                MyAPIGateway.Utilities.ShowNotification("Event");

            }
            return 0.0f;




            //thrustEvent.flame += upIntensity;



            //thrustEvent.flame += upIntensity;



        }

        public float UpdateIntensity(float thr)
        {

            //add compare to last value



            //cache it
            float thrClamp = 0;
            if (_flameIntensity - (0.05f) < thr) thrClamp = thr;

            var t = MathHelper.Clamp(_flameIntensity + (0.1f), 0, thr);
            var t2 = MathHelper.Clamp(_flameIntensity - (0.05f), 0, 1);
            float old = _flameIntensity;

            float temp = 0;
            temp = _flameIntensity;
            {
                float newIntensity = 0;
                if (_flameIntensity < thr)
                {
                    
                    newIntensity = VRageMath.MathHelper.Lerp(_flameIntensity, thr, (8.0f / 60));
                    
                }

                else if (_flameIntensity > thr)
                {
                    
                    newIntensity = VRageMath.MathHelper.Lerp(_flameIntensity, thr, (40.0f / 60));
                    
                }
                

                _flameIntensity = (float)MathHelper.Clamp(newIntensity, 0, 1); //Why does this not work? goes to -1.6e^18
                if (_flameIntensity < 0.01f) { _flameIntensity = 0.0f; }
                if (_flameIntensity >= 0.995f) { _flameIntensity = 1.0f; }
                //MyAPIGateway.Utilities.ShowNotification("_flameIntensity: " + _flameIntensity , 1, MyFontEnum.Blue);
                


            }
            temp = _flameIntensity;

            try
            {
                
                return _flameIntensity;
            }
            catch
            {
                MyAPIGateway.Utilities.ShowNotification("Event");

            }
            return 0.0f;




        }

       
        public string[] ParserText(string text, char c)
        {
            //MyLog.Default.WriteLineAndConsole("\n Got: " + text);
            string[] strings = { "" };
            if (text == null || text == "") return strings;
            string s = text;
            char[] x = { c };
            strings = s.Split(x);
            List<string> stringList = new List<string>();
            foreach (var word in strings)
            {
                //MyLog.Default.WriteLineAndConsole("\n Particles: " + word);
                stringList.Add(word);
            }
            //MyLog.Default.WriteLineAndConsole("\n stringsList: " + stringList.ToString());
            return stringList.ToArray();// strings;


        }
        public float[] ParserFloat(string text, char c)
        {
            float[] values = { 0 };
            if (text == null || text == "") return values;

            char[] x = { c };
            string[] strings = text.Split(x);
            List<float> floatList = new List<float>();
            for (int runs = 0; runs < strings.Length; runs++)
            {
                floatList.Add(float.Parse(strings[runs]));
            }


            values = floatList.ToArray();
            return values;


        }

        public string RemoveWhitespace(string s)
        {
            string str = s;
            str = Regex.Replace(str, @"\s+", "");
            return str;
        }


        public override void OnBeforeRemovedFromContainer()
        {
            
            //TerminateBlockAndParticles();

        }

        public override void Close()
        {
            if (block != null)
            {
                TerminateBlockAndParticles();
                block.IsWorkingChanged -= UpdateWorking;
                block.CubeGrid.OnGridSplit -= UpdateGrid;
                block.CubeGrid.OnGridMerge -= UpdateGrid;
                block.CubeGrid.OnIsStaticChanged -= UpdateStationGid;
                block.PropertiesChanged -= UpdateRecolorable;

            }

            
            
            //MyLog.Default.WriteLineAndConsole("TFX: Removed...");


            //if (session.gridCache.try ContainsKey(myGrid))
            //{ 
            //    MyLog.Default.WriteLineAndConsole("Close| Grid in dict");
            //    session.gridCache.Remove(myGrid);

            //}
            //MyLog.Default.WriteLineAndConsole("TFX: Grid Removed...");
            //called when block is removed for whatever reason (incl ship despawn, etc).
            base.Close();  
        }

        public void TerminateBlockAndParticles()
        {
            //thrustEvent.OnDestroy();
            //if (block != null)
            //{
                
            foreach (var item in EmitterList)
            {

                //Add to list on creation instead, just toogle destroy and keep counting time until "destroyObject" is triggered

                //if (item._emitter == null) { item.RemoveThis(); return; }
                item.destroyObject = true;
                //item._currentLife = 90000;
                //item.works = false;
                item.UpdateTheEmitter(0);
                
                //item._isActive = false;
                ////move to event
                ////item.DestroyEmitters();
                




            }
            //MyLog.Default.WriteLineAndConsole("[TFX]Adding to late Removal....");
            if(EmitterList.Count!=0 || EmitterList!=null) session.emittersToRemove.AddList (EmitterList);
            
            //move all emitterhandlers over to session so that they dont just pop if the block is destroyed
            //session.emittersToRemove.AddList<MexEmitterHandler_v2>(EmitterList);
            EmitterList.Clear();
            //EmitterList = null;
            pSettingList.Clear();
            //pSettingList = null;
                //block = null; //reeee to the past and back again




            //}
        }

    }



}