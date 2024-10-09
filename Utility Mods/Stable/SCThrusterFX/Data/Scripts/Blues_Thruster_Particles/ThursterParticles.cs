//Sytems
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Collections.Concurrent;
//Sandboxs
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using Sandbox.Definitions;
//Vrage
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.ModAPI.Network;
using VRage.Sync;

namespace Blues_Thruster_Particles
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class Thrusters : MyGameLogicComponent
    {
        public static Thrusters Instance;

        public Guid guid = new Guid("C8DAD855-2F60-401A-A5D4-09D0C6D14BD6");

        public static bool IsClient => !(IsServer && IsDedicated);
        public static bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;
        public static bool IsServer => MyAPIGateway.Multiplayer.IsServer;
        public static bool IsActive => MyAPIGateway.Multiplayer.MultiplayerActive;

        private MySync<bool, SyncDirection.BothWays> requiresUpdate;

        private ConcurrentDictionary<long, ThrusterData> thrusterCache = new ConcurrentDictionary<long, ThrusterData>();
        private const int CacheUpdateInterval = 60; // Update cache every 60 frames

        string particleeffect = "";

        private string BlockSizeAdjuster = "";
        private float ParticleSizeAdjuster;
        //My Thrusters 
        private IMyThrust CoreBlock;
        public MyThrust MyCoreBlock;
        public MyThrustDefinition MyCoreBlockDefinition;
        IMyTerminalBlock terminalBlock;

        private MatrixD particle_matrix = MatrixD.Identity;
        private Vector3D particle_position = Vector3D.Zero;
        private MyParticleEffect ParticleEmitter;

        public string ParticleEffectToGenerate;
        public Vector4 FlameColor;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Instance = this;
            //Update Every Frame
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            //Grab MyThruster
            CoreBlock = Entity as IMyThrust;
            MyCoreBlock = CoreBlock as MyThrust;
            MyCoreBlockDefinition = MyCoreBlock.BlockDefinition;
            terminalBlock = Entity as IMyTerminalBlock;

            ParticleEffectToGenerate = "";

            //Adapt for block size
            string SubtypeId = CoreBlock.BlockDefinition.SubtypeId;

            if ((SubtypeId.Contains("FocusDrive")) || (SubtypeId.Contains("IonHeavy") || (SubtypeId.Contains("HugeHydro"))))
            {
                BlockSizeAdjuster = " LgHb";
                ParticleSizeAdjuster = 4.5f;
            }

            if ((SubtypeId.Contains("LargeBlock") && SubtypeId.Contains("BlockLarge")) || SubtypeId.Contains("LG_FusionDrive") || SubtypeId.Contains("LG_HydroThrusterL"))
            {
                BlockSizeAdjuster = " LgLb";
                ParticleSizeAdjuster = 2.8f;
            }

            if ((SubtypeId.Contains("LargeBlock") && SubtypeId.Contains("BlockSmall")) || SubtypeId.Contains("SG_FusionDrive") || SubtypeId.Contains("LG_HydroThrusterS"))
            {
                BlockSizeAdjuster = " LgSb";
                ParticleSizeAdjuster = 1f;
            }

            if ((SubtypeId.Contains("SmallBlock") && SubtypeId.Contains("BlockLarge")) || SubtypeId.Contains("SG_HydroThrusterL"))
            {
                BlockSizeAdjuster = " SgLb";
                ParticleSizeAdjuster = 0.5f;
            }

            if ((SubtypeId.Contains("SmallBlock") && SubtypeId.Contains("BlockSmall")) || SubtypeId.Contains("SG_HydroThrusterS"))
            {
                BlockSizeAdjuster = " SgSb";
                ParticleSizeAdjuster = 0.1f;
            }
            if (SubtypeId.Contains("STR_DISABLED"))
            {
                //Small Grid
                if (SubtypeId.Contains("50") && !SubtypeId.Contains("550")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 50"; }
                if (SubtypeId.Contains("100")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 100"; }
                if (SubtypeId.Contains("200")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 200"; }
                if (SubtypeId.Contains("300")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 300"; }
                //LargeGrid
                if (SubtypeId.Contains("350")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 350"; }
                if (SubtypeId.Contains("400")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 400"; }
                if (SubtypeId.Contains("500")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 500"; }
                if (SubtypeId.Contains("550")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 550"; }
                if (SubtypeId.Contains("600")) { ParticleSizeAdjuster = 1f; BlockSizeAdjuster = " 600"; }
            }
            if (IsDedicated) { return; }
            LoadCustomData();
            UpdateCustomData();
            requiresUpdate.ValidateAndSet(true);
            thrusterCache.TryAdd(Entity.EntityId, new ThrusterData(ParticleEffectToGenerate, FlameColor, CoreBlock.Enabled, CoreBlock.CurrentThrust, CoreBlock.MaxThrust));
        }

        public override void UpdateAfterSimulation()
        {
            if (IsDedicated) return;

            CustomControls.AddControls(ModContext);

            if (requiresUpdate.Value)
            {
                try
                {
                    requiresUpdate.ValidateAndSet(false);
                    MyCoreBlockDefinition.FlameFullColor = FlameColor;
                    MyCoreBlockDefinition.FlameIdleColor = FlameColor;
                    (MyCoreBlock.Render).UpdateFlameAnimatorData();
                }
                catch { MyLog.Default.WriteLine("Unable to adjust thruster flame"); }
            }

            ThrusterData cachedData;
            if (thrusterCache.TryGetValue(Entity.EntityId, out cachedData))
            {
                if (MyAPIGateway.Session.GameplayFrameCounter % CacheUpdateInterval == 0 || cachedData.ParticleEffect != ParticleEffectToGenerate || cachedData.FlameColor != FlameColor || cachedData.Enabled != CoreBlock.Enabled || cachedData.CurrentThrust != CoreBlock.CurrentThrust || cachedData.MaxThrust != CoreBlock.MaxThrust)
                {
                    cachedData.ParticleEffect = ParticleEffectToGenerate;
                    cachedData.FlameColor = FlameColor;
                    cachedData.Enabled = CoreBlock.Enabled;
                    cachedData.CurrentThrust = CoreBlock.CurrentThrust;
                    cachedData.MaxThrust = CoreBlock.MaxThrust;
                    thrusterCache[Entity.EntityId] = cachedData;
                }

                float ThrusterOutput = cachedData.CurrentThrust / cachedData.MaxThrust;
                float particleSize = ThrusterOutput * ParticleSizeAdjuster;

                if (cachedData.ParticleEffect != "Vanilla" || cachedData.ParticleEffect != "" || !cachedData.Enabled || ThrusterOutput < 0.049)
                {
                    string particleToCreate;
                    float particleRadius = 1f;

                    try
                    {
                        particleToCreate = Globals.ParticleEffectsList[cachedData.ParticleEffect] + BlockSizeAdjuster;
                    }
                    catch
                    {
                        particleToCreate = "Blueshift" + BlockSizeAdjuster;
                    }

                    if (ParticleEmitter == null)
                    {
                        particle_matrix = CoreBlock.WorldMatrix;
                        particle_position = particle_matrix.Translation;

                        if (MyParticlesManager.TryCreateParticleEffect(particleToCreate, ref particle_matrix, ref particle_position, uint.MaxValue, out ParticleEmitter))
                        {
                            particleeffect = cachedData.ParticleEffect;
                            ParticleEmitter.UserRadiusMultiplier = particleRadius;
                            ParticleEmitter.UserScale = particleSize;
                        }
                    }
                    else
                    {
                        if (particleeffect != cachedData.ParticleEffect)
                        {
                            ParticleEmitter.UserScale = 0.0f;
                            ParticleEmitter.StopLights();
                            ParticleEmitter.StopEmitting();
                            ParticleEmitter = null;
                        }
                        else
                        {
                            ParticleEmitter.WorldMatrix = CoreBlock.WorldMatrix;
                            ParticleEmitter.UserRadiusMultiplier = particleRadius;
                            ParticleEmitter.UserScale = particleSize;
                            ParticleEmitter.Play();
                        }
                    }
                }
                else
                {
                    try
                    {
                        ParticleEmitter.Stop();
                        ParticleEmitter.StopLights();
                        ParticleEmitter.StopEmitting();
                        ParticleEmitter = null;
                    }
                    catch { MyLog.Default.WriteLine("Unable to close particle effects"); }
                }
            }
        }

        public override void UpdateAfterSimulation10()
        {

        }

        public static Thrusters GetInstance()
        {
            return Instance;
        }

        public void UpdateCustomData()
        {
            requiresUpdate.ValidateAndSet(true);
            if (ParticleEffectToGenerate == "") { ParticleEffectToGenerate = "Vanilla"; }
            CoreBlock.CustomData = $"{ParticleEffectToGenerate}:{FlameColor.X}:{FlameColor.Y}:{FlameColor.Z}:{FlameColor.W}";
        }

        public void LoadCustomData()
        {
            var parts = CoreBlock.CustomData.Split(':');
            if (parts.Length != 5)
            {
                ParticleEffectToGenerate = "";
                if (MyCoreBlockDefinition.FuelConverter.FuelId == Globals.HydrogenId)
                { FlameColor = new Vector4(1f, 0.7f, 0.0f, 0.5f); }
                else if (CoreBlock.BlockDefinition.SubtypeId.ToLower().Contains("atmo"))
                { FlameColor = new Vector4(1f, 1f, 1f, 0.5f); }
                else
                { FlameColor = new Vector4(0.60f, 1.40f, 2.55f, 0.5f); }
            }
            else
            {
                try
                {
                    var vector = new Vector4(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
                    ParticleEffectToGenerate = parts[0];
                    if (ParticleEffectToGenerate == "") { ParticleEffectToGenerate = "Vanilla"; }
                    FlameColor = vector;
                }
                catch (Exception x)
                {
                    ParticleEffectToGenerate = "Vanilla";
                    FlameColor = Color.Red;
                }
            }
            UpdateCustomData();
        }

        public override void Close()
        {
            if (ParticleEmitter != null)
            {
                ParticleEmitter.Stop();
            }
            Instance = null;
        }
    }

    public class ThrusterData
    {
        public string ParticleEffect;
        public Vector4 FlameColor;
        public bool Enabled;
        public float CurrentThrust;
        public float MaxThrust;

        public ThrusterData(string particleEffect, Vector4 flameColor, bool enabled, float currentThrust, float maxThrust)
        {
            ParticleEffect = particleEffect;
            FlameColor = flameColor;
            Enabled = enabled;
            CurrentThrust = currentThrust;
            MaxThrust = maxThrust;
        }
    }
}