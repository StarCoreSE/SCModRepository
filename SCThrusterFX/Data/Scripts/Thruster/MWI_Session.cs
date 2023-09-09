using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using MWI.Thruster;

using System.Linq;
using Draygo.BlockExtensionsAPI;
using static VRageRender.MyBillboard;

namespace MWI.Thruster
{
    // This object is always present, from the world load to world unload.
    // NOTE: all clients and server run mod scripts, keep that in mind.
    // The MyUpdateOrder arg determines what update overrides are actually called.
    // Remove any method that you don't need, none of them are required, they're only there to show what you can use.
    // Also remove all comments you've read to avoid the overload of comments that is this file.

    /*
     * Todo
     * Way to many things
     * 
     * 
     * 
     */


    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation )]
    public class ThrusterSession : MySessionComponentBase
    {
        //This place is kinda dirty and need some cleanup, grab some gloves and a face mask before going into these parts of town

        public static ThrusterSession Instance; // the only way to access session comp from other classes and the only accepted static.

        //public ThrusterEvents myevents;

        
        //Networking
        public Networking Networking = new Networking(5872);

        IMyCubeGrid grid;
        
        
        
        
        //IMyEntity
        BoundingSphereD mySphere = new BoundingSphereD { Radius = 1500 };
        
        

        public Dictionary<string, int> blockSubTypes = new Dictionary<string, int>(); //subtype | amount
        public Dictionary<IMyCubeGrid, int> gridCache = new Dictionary<IMyCubeGrid, int>(); //block | boundingSphere
        public Dictionary<IMyEntity, int> gidsInRange = new Dictionary<IMyEntity, int>(); // block | distance

        public struct GridAtmoData
        {
            public int Altitude;
            public float AtmoDensity;
        }
        public Dictionary<IMyEntity, GridAtmoData> gidsInAtmo = new Dictionary<IMyEntity, GridAtmoData>(); // block | distance

        public Dictionary<long, float> ThrusterForce = new Dictionary<long, float>(); // block | distance
        public Dictionary<IMyCubeGrid, float> AutoPilotGrids = new Dictionary<IMyCubeGrid, float>(); // block | distance

        public List<MWI_Thruster.MexEmitterHandler_v2> emittersToRemove = new List<MWI_Thruster.MexEmitterHandler_v2>();
        //public Dictionary<string, ParticleSettings> ExistingSetups = new Dictionary<string, ParticleSettings>();

        //public Dictionary<string, List<ParticleSettings>> settingCache = new Dictionary<string, List<ParticleSettings>>();

        public struct GridThrusterBlocks
        {
            public Dictionary<IMyEntity, float> blockThrust;
        }
        public Dictionary<IMyEntity, GridThrusterBlocks> GridBlockMap = new Dictionary<IMyEntity, GridThrusterBlocks>(); // block | distance
        public Dictionary<IMyEntity, float> BlockThrustMap = new Dictionary<IMyEntity, float>(); // block | distance



        public event Action RemoveEmitter;
        public void EmitterRemoval(MWI_Thruster.MexEmitterHandler_v2 emitter)
        {
            //MyLog.Default.WriteLineAndConsole("RemoveTrigger");
            emittersToRemove.Remove(emitter);
        }
        public void RunEmitterCleanup()
        {
            RemoveEmitter?.Invoke();
        }
        public event Action<int> TickTime;

        public List<string> listedSubtypes = new List<string>();

        public virtual void SetupSubtypes() { }
        //List<string> subtypeList = new List<string>();

        bool regenList = true;

        public bool debugging = false;
        bool _setupDone=false;

        public int eventThrustChangedCounter = 0;
        public int eventIntensityChangedCounter = 0;
        public int eventHeatUpChangedCounter = 0;
        public int validSpawns = 0;
        int PastEventTrigger1 = 0;
        int PastEventTrigger2 = 0;
        int PastEventTrigger3 = 0;

        public int liveParticles = 0;
        public int liveEmitters = 0;

        static float _cullRange = 2500; //add cull per particle or per particle subtype
        static float _closeRange = 300;
        public double currentRange = 0;

        public int _activeParticleSystems =0;
        public int _activeThrusters = 0;
        public int _thrusterUpdates = 0;
        public int _Updates = 0;
        public int _lastActiveParticleSystems = 0;
        public int _lastActiveThrusters = 0;

        public int HandlerCreations = 0;
        public Vector3 cameraPos = new Vector3(0,0,0);
        int timer = 0;
        int blocksRemoved=0;

        //FutureImplementaion
        public DefinitionExtensionsAPI DefExtensions = new DefinitionExtensionsAPI(InitDefinitions);
        public static readonly MyStringId MwiThrusterComp = MyStringId.GetOrCompute("MwiComponent");

        private static void InitDefinitions()
        {
            Instance.DefExtensions.RegisterGameLogic<MWI_Thruster>(MwiThrusterComp, Instance.ModContext);
        }
        public ThrusterSession()
        {
            Instance = this;           
        }

        public void Tick() { }
        public void Tick100() { }

        public void UpdateAutopilotList()
        {
            AutoPilotGrids.Clear();
            foreach (var g in gridCache)
            {
                if (g.Key != null)
                {
                    
                    if (g.Key.ControlSystem != null)
                    {
                        if (g.Key.ControlSystem.CurrentShipController != null)
                        {
                            if(g.Key.ControlSystem.CurrentShipController.IsAutopilotControlled)
                            {
                                AutoPilotGrids.Add(g.Key, 0);
                            }
                        }
                    }
                }
            }
        }

        public bool AutoPilotCheck(IMyCubeGrid grid) {

            //(grid as MyCubeGrid).
            if(grid != null) {
                if (AutoPilotGrids.ContainsKey(grid))
                {
                    return true;
                }
                return false;
            }            
                return false;            
        }

        public void SyncedValue(long val, float t)
        {
            //add dict cleanup
            if (ThrusterForce.ContainsKey(val))
            {
                ThrusterForce[val] = t;
            }
            else
            {
                ThrusterForce.Add(val, t);
            }
            //MyLog.Default.WriteLineAndConsole("----Sycked: " + val +" | "+t);             
        }

        public void UpdateBlockThrustServer()
        {
            var tempThrustMap = new Dictionary<IMyEntity, float>();

            //HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
            //MyAPIGateway.Entities.GetEntities(ents);
            //foreach (var e in ents) {
            //    if ((e as IMyCubeGrid) != null)
            //        }
            foreach ( var grid in gidsInRange)
            {
                if (GridBlockMap.ContainsKey(grid.Key))
                {
                    var blocklist = GridBlockMap[grid.Key];
                    foreach (var block in blocklist.blockThrust)
                    {
                        blocklist.blockThrust[block.Key] = (block.Key as IMyThrust).CurrentThrustPercentage;
                        tempThrustMap.Add(block.Key, (block.Key as IMyThrust).CurrentThrustPercentage);
                        MyLog.Default.WriteLineAndConsole("----SessionThrust: " + (block.Key as IMyThrust).CurrentThrustPercentage.ToString());
                    }
                }
            }
            BlockThrustMap.Clear();
            BlockThrustMap = tempThrustMap;
        }

        public float GetBlockThrustServer(IMyEntity block)
        {
            if(BlockThrustMap.ContainsKey(block))
            {
                return BlockThrustMap[block];
            }

            return 0.0f;
        }

        public void InrangeCheck()
        {
            gidsInRange.Clear();
            gidsInAtmo.Clear();
            foreach (var pair in gridCache)  
            {
                //BoundingSphereD sphere = new BoundingSphereD { Radius = 100 };// pair.Value; // atm  hardcoded to 100m to allow nearby emitters. maybe just set a longer cull timer if they are nearby
                
                BoundingSphereD sphere = new BoundingSphereD {
                    Radius = pair.Key.WorldVolume.Radius*1.5f+100,
                    Center = pair.Key.WorldVolume.Center };

                //<= Math.Pow(pair.Key.WorldVolume.Radius * 1.5f + 100, 2))

                var dist = Vector3D.DistanceSquared(cameraPos, pair.Key.WorldVolume.Center);
                //bool closeDist = Vector3D.DistanceSquared(cameraPos, pair.Key.WorldVolume.Center);
                if (dist <= Math.Pow(_cullRange, 2)) { 
                    if (MyAPIGateway.Session.Camera.IsInFrustum(ref sphere))
                    {
                        //In view and in range
                        InAtmoCheck(pair.Key);                    
                        gidsInRange.Add(pair.Key, (int)dist);
                    }
                }
            }            
        }

        public void InAtmoCheck(IMyCubeGrid grid)
        {
            //hmm.. just make it (grid,density). just check ref and compare density vs value in block parameters. value in block parameters will be a default or set through a setting
            //var closestPlanet = MyGamePruningStructure.GetClosestPlanet(grid.WorldMatrix.Translation);
            //if (closestPlanet != null)
            //{
            //    bool inR = false;
            //    float at = 0.0f;
            //    //If in side planet influence range
            //    if(closestPlanet.AtmosphereRadius > Vector3.Distance(grid.GetPosition(), closestPlanet.WorldMatrix.Translation))
            //    {
            //        inR = true;
            //        at = closestPlanet.GetAirDensity(grid.WorldMatrix.Translation);
            //        MyAPIGateway.Utilities.ShowNotification("[PlanetData: " + inR +" | "+ at + "]", 1, MyFontEnum.Blue);

            //    }
            //}
            //if ()
            Vector3D gridPos = grid.GetPosition();

            MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(gridPos);
            if (closestPlanet != null)
            {
                GridAtmoData t = new GridAtmoData();
                t.Altitude = (int)Math.Round(Vector3.Distance(gridPos, closestPlanet.GetClosestSurfacePointGlobal(gridPos)));
                t.AtmoDensity = closestPlanet.GetAirDensity(cameraPos);
                gidsInAtmo.Add(grid, t);
                // MyAPIGateway.Utilities.ShowNotification("[PlanetData: " + inR + " | " + alt + " | " + at + "]", 1, MyFontEnum.Blue);
            }

            //Old
            //if (MyGamePruningStructure.GetClosestPlanet(grid.WorldMatrix.Translation)?.GetAirDensity(grid.WorldMatrix.Translation) > 0.25f)
            //{
            //    gidsInAtmo.Add(grid, 1);
            //}
        }

         public bool CheckRange(IMyCubeGrid grid)
        {
            //for update rate: visuals
            return gidsInRange.ContainsKey(grid);
        }

        public bool CheckCloseRange(IMyCubeGrid grid)
        {            
            int dist = 0;
            gidsInRange.TryGetValue(grid, out dist);

            if(dist<Math.Pow(_closeRange,2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        public int GetActiveEmitters(int newvalue)
        {
            _activeParticleSystems += newvalue;
            //var currentCount = numberRuns;
            return _activeParticleSystems;
        }
        public void GetActiveThrusters()
        {
            _activeThrusters++;

        }
        public void GetUpdates()
        {
            _thrusterUpdates++;

        }



        public override void LoadData()
        {
            // amogst the earliest execution points, but not everything is available at this point.

            // main entry point: MyAPIGateway
            // entry point for reading/editing definitions: MyDefinitionManager.Static
            // these can be used anywhere as they're types not fields.

            //Instance = this;
        }

        public override void BeforeStart()
        { 
            MyLog.Default.WriteLineAndConsole("[TFX] Version 1.4");
            Networking.Register();
            // executed before the world starts updating
        }
        
        protected override void UnloadData()
        {
            // executed when world is exited to unregister events and stuff
            DefExtensions?.UnloadData();
            MyLog.Default.WriteLineAndConsole("[TFX]Cleaning up...." + emittersToRemove.Count + " EmitterHandlers");
            Networking?.Unregister();
            Networking = null;
            RunEmitterCleanup();

            //remmoveEmitter();
            //var templist = new List<MWI_Thruster.MexEmitterHandler_v2>();
            for (int i = 0; i < emittersToRemove.Count; i++)
            {
                emittersToRemove[i].RemoveThis();
                emittersToRemove[i] = null;
            }
            emittersToRemove.Clear();

            for (int i = 0; i < emittersToRemove.Count; i++)
            {
                emittersToRemove[i].RemoveThis();
                emittersToRemove[i] = null;
            }
            emittersToRemove.Clear();
            emittersToRemove = null;
            gidsInAtmo.Clear();
            gidsInAtmo = null;
            gidsInRange.Clear();
            gidsInRange = null;
            blockSubTypes.Clear();
            blockSubTypes = null;
            gridCache.Clear();
            gridCache = null;
            AutoPilotGrids.Clear();
            ThrusterForce.Clear();
            Instance = null; // important for avoiding this object to remain allocated in memory
        }

        public override void HandleInput()
        {
            // gets called 60 times a second before all other update methods, regardless of framerate, game pause or MyUpdateOrder.
        }

        //public override void Up
        //Do this per subtype, culling  dist per subtype? Override client option for performance?
        

        public float WeBeTickin()
        {

            TickTime?.Invoke(5);
            return 5;
        }    

        public override void UpdateBeforeSimulation()
        {
            // executed every tick, 60 times a second, before physics simulation and only if game is not paused.
            //if (timer % 10 == 0)  MyLog.Default.WriteLineAndConsole("[TFX]Dis Server");
            ///////////ADD IS SERVER CHECK"!"""""!"!!"!"
            if (MyAPIGateway.Utilities.IsDedicated || (MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE && MyAPIGateway.Session.IsServer)) {
                if (timer >= 120)
                {
                    UpdateAutopilotList();
                    timer = 0;
                    //MyLog.Default.WriteLineAndConsole("[TFX] [Updating on Host Session]");
                }
                timer++;
                if(MyAPIGateway.Utilities.IsDedicated) return;
            }
            //MyAPIGateway.Utilities.ShowNotification("[Thrusters in list: "+ThrusterForce.Count+"]", 1, MyFontEnum.Blue);
            //MyAPIGateway.Utilities.ShowNotification("[Cache in list: " + gridCache.Count + "]", 1, MyFontEnum.Blue);
            if (timer % 10 == 0) {
                WeBeTickin();
                //UpdateBlockThrustServer();
            }
            //if (timer >= 600)
            //{
            //    MyLog.Default.WriteLineAndConsole("[TFX] [Thrusters in list: " + ThrusterForce.Count + "]");
            //    MyLog.Default.WriteLineAndConsole("[TFX] [Cache in list: " + gridCache.Count + "]");
            //}

            //var closestPlanet = MyGamePruningStructure.GetClosestPlanet(cameraPos);
            //if (closestPlanet != null)
            //{
            //    bool inR = false;
            //    float at = 0.0f;
            //    float alt = 0.0f;
            //    //If in side planet influence range
            //    inR = true;
            //    at = closestPlanet.GetAirDensity(cameraPos);
            //    alt = Vector3.Distance(cameraPos, closestPlanet.GetClosestSurfacePointGlobal(cameraPos));
                
            //    MyAPIGateway.Utilities.ShowNotification("[PlanetData: " + inR + " | " + alt+" | " + at + "]", 1, MyFontEnum.Blue);
            //}


                if (timer >= 60)
            {
               
                cameraPos = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                InrangeCheck();
                UpdateAutopilotList();
                //cameraPos.pla
                //eventtriggerCounter = 0;
                timer = 0;
                if (emittersToRemove.Count > 0)
                {
                    List<int> remov = new List<int>();
                    for (int i = 0; i < emittersToRemove.Count; i++)
                    {
                        //MyLog.Default.WriteLineAndConsole("[TFX] Checking GetThrusterData....");
                        //emittersToRemove[i].UpdateTheEmitter(0); ;// _isActive = false;
                        if (emittersToRemove[i].thrustData.Emitter == null)
                        {
                            //MyLog.Default.WriteLineAndConsole("[TFX]So Funny Removal....");
                            //remov.Add(i);
                            emittersToRemove[i].RemoveThis();
                        } 

                    }
                    //emittersToRemove.RemoveIndices(remov);
                }
            }
            timer++;
        }
    }
}
