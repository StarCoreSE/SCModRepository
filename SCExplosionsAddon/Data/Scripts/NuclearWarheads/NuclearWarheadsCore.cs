using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace NuclearWarheads
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class NuclearWarheadsCore : MySessionComponentBase
    {
        public static NuclearWarheadsCore Instance;

        public const long EXPLOSION_MOD_MESSAGE_ID = 4508338751823;
        private const string CONFIG_FILE_NAME = "Config.xml";
        public static NuclearWarheadsSettingsConfig ConfigData;
        public static double MAX_RADIUS_SQR;
        public static double NUCLEAR_EXPLOSION_RADIUS_CUBED;
        public static double THERMONUCLEAR_EXPLOSION_RADIUS_CUBED;

        // Networking
        public readonly Networking Network = new Networking(41838);

        public static readonly MyDefinitionId nuclearMissileAmmoId = MyVisualScriptLogicProvider.GetDefinitionId("AmmoMagazine", "Missile200mm_Nuclear");

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(CONFIG_FILE_NAME, typeof(NuclearWarheadsSettingsConfig)))
                {
                    MyAPIGateway.Parallel.Start(() =>
                    {
                        using (var sw = MyAPIGateway.Utilities.WriteFileInWorldStorage(CONFIG_FILE_NAME,
                            typeof(NuclearWarheadsSettingsConfig))) sw.Write(MyAPIGateway.Utilities.SerializeToXML<NuclearWarheadsSettingsConfig>(new NuclearWarheadsSettingsConfig()));
                    });
                }

                try
                {
                    ConfigData = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(CONFIG_FILE_NAME, typeof(NuclearWarheadsSettingsConfig));
                    string configcontents = reader.ReadToEnd();
                    ConfigData = MyAPIGateway.Utilities.SerializeFromXML<NuclearWarheadsSettingsConfig>(configcontents);

                    byte[] bytes = MyAPIGateway.Utilities.SerializeToBinary(ConfigData);
                    string encodedConfig = Convert.ToBase64String(bytes);

                    MyAPIGateway.Utilities.SetVariable("HSCSettings_Config_xml", encodedConfig);

                    //MyLog.Default.WriteLineAndConsole($"EXPANDED!: " + encodedConfig);
                }
                catch (Exception exc)
                {
                    ConfigData = new NuclearWarheadsSettingsConfig();
                    //MyLog.Default.WriteLineAndConsole($"ERROR: {exc.Message} : {exc.StackTrace} : {exc.InnerException}");
                }
            }
            else
            {
                try
                {
                    string str;
                    MyAPIGateway.Utilities.GetVariable("HSCSettings_Config_xml", out str);

                    byte[] bytes = Convert.FromBase64String(str);
                    ConfigData = MyAPIGateway.Utilities.SerializeFromBinary<NuclearWarheadsSettingsConfig>(bytes);
                }
                catch
                {
                    ConfigData = new NuclearWarheadsSettingsConfig();
                }
            }

            MAX_RADIUS_SQR = ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius * ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius;
            NUCLEAR_EXPLOSION_RADIUS_CUBED = ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius * ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius * ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius;
            THERMONUCLEAR_EXPLOSION_RADIUS_CUBED = ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearExplosionRadius * ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearExplosionRadius * ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearExplosionRadius;

            Network.Register();

            NuclearWarheadBlockComponent.Init();

            if (ConfigData.VanillaWarheadBlockConfig.OverrideVanillaWarhead)
            {
                var warheadSmallDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_small);
                if (warheadSmallDef != null)
                {
                    (warheadSmallDef as MyWarheadDefinition).ExplosionRadius = 0.01f;
                    (warheadSmallDef as MyWarheadDefinition).WarheadExplosionDamage = 1f;
                }

                var warheadLargeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_large);
                if (warheadLargeDef != null)
                {
                    (warheadLargeDef as MyWarheadDefinition).ExplosionRadius = 0.01f;
                    (warheadLargeDef as MyWarheadDefinition).WarheadExplosionDamage = 1f;
                }
            }

            var miniNukeWarheadDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_mininuke);
            if (miniNukeWarheadDef != null)
            {
                if (!ConfigData.MiniNukeBlockConfig.PlayerCanBuildMiniNuke)
                    miniNukeWarheadDef.AvailableInSurvival = false;

                if (ConfigData.MiniNukeBlockConfig.MiniNukeUraniumComponentRequirement >= 0)
                {
                    if (ConfigData.MiniNukeBlockConfig.MiniNukeUraniumComponentRequirement == 0f)
                    {
                        var pp = new List<MyCubeBlockDefinition.Component>(miniNukeWarheadDef.Components);
                        pp.RemoveAt(5);
                        miniNukeWarheadDef.Components = pp.ToArray();
                    }
                    else
                    {
                        miniNukeWarheadDef.Components[5].Count = ConfigData.MiniNukeBlockConfig.MiniNukeUraniumComponentRequirement;
                    }
                }
            }

            var nuclearWarheadDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_nuclear);
            var nuclearWarheadSmallDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_nuclearsmall);
            if (nuclearWarheadDef != null && nuclearWarheadSmallDef != null)
            {
                if (!ConfigData.NuclearWarheadBlockConfig.PlayerCanBuildNuclearWarhead)
                {
                    nuclearWarheadDef.AvailableInSurvival = false;
                    nuclearWarheadSmallDef.AvailableInSurvival = false;
                }

                if (ConfigData.NuclearWarheadBlockConfig.NuclearWarheadUraniumComponentRequirement >= 0)
                {
                    if (ConfigData.NuclearWarheadBlockConfig.NuclearWarheadUraniumComponentRequirement == 0f)
                    {
                        var pp = new List<MyCubeBlockDefinition.Component>(nuclearWarheadDef.Components);
                        pp.RemoveAt(5);
                        nuclearWarheadDef.Components = pp.ToArray();

                        var pp2 = new List<MyCubeBlockDefinition.Component>(nuclearWarheadSmallDef.Components);
                        pp.RemoveAt(5);
                        nuclearWarheadSmallDef.Components = pp.ToArray();
                    }
                    else
                    {
                        nuclearWarheadDef.Components[5].Count = ConfigData.NuclearWarheadBlockConfig.NuclearWarheadUraniumComponentRequirement;
                        nuclearWarheadSmallDef.Components[5].Count = ConfigData.NuclearWarheadBlockConfig.NuclearWarheadUraniumComponentRequirement;
                    }
                }
            }

            var thermoWarheadDef = MyDefinitionManager.Static.GetCubeBlockDefinition(NuclearWarheadBlockComponent.warhead_thermonuclear);
            if (thermoWarheadDef != null)
            {
                if (!ConfigData.ThermoNuclearWarheadBlockConfig.PlayerCanBuildThermoNuclearWarhead)
                    thermoWarheadDef.AvailableInSurvival = false;

                if (ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadUraniumComponentRequirement >= 0)
                {
                    if (ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadUraniumComponentRequirement == 0f)
                    {
                        var pp = new List<MyCubeBlockDefinition.Component>(thermoWarheadDef.Components);
                        pp.RemoveAt(5);
                        thermoWarheadDef.Components = pp.ToArray();
                    }
                    else
                    {
                        thermoWarheadDef.Components[5].Count = ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadUraniumComponentRequirement;
                    }
                }

                if (ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadSuperconductorComponentRequirement >= 0)
                {
                    if (ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadUraniumComponentRequirement == 0f)
                    {
                        var pp = new List<MyCubeBlockDefinition.Component>(thermoWarheadDef.Components);
                        pp.RemoveAt(3);
                        thermoWarheadDef.Components = pp.ToArray();
                    }
                    else
                    {
                        thermoWarheadDef.Components[3].Count = ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearWarheadSuperconductorComponentRequirement;
                    }
                }
            }

            if (!ConfigData.NuclearMissileLauncherConfig.PlayerCanBuildNuclearMissileLauncher)
            {
                var missileLauncherDef = MyDefinitionManager.Static.GetCubeBlockDefinition(MyVisualScriptLogicProvider.GetDefinitionId("SmallMissileLauncher", "LargeMissileLauncher_Nuclear"));
                if (missileLauncherDef != null)
                    missileLauncherDef.AvailableInSurvival = false;

                var missileLauncherSmallDef = MyDefinitionManager.Static.GetCubeBlockDefinition(MyVisualScriptLogicProvider.GetDefinitionId("SmallMissileLauncherReload", "SmallRocketLauncherReload_Nuclear"));
                if (missileLauncherSmallDef != null)
                    missileLauncherSmallDef.AvailableInSurvival = false;
            }

            if (!ConfigData.NuclearMissileLauncherConfig.PlayerCanFireNuclearMissileWithHandheldLauncher)
            {
                var handheldLauncherDef = MyDefinitionManager.Static.GetWeaponDefinition(MyVisualScriptLogicProvider.GetDefinitionId("WeaponDefinition", "AdvancedHandHeldLauncherGun"));
                if (handheldLauncherDef != null && handheldLauncherDef.AmmoMagazinesId.Length > 1)
                {
                    var ammoList = new List<MyDefinitionId>(handheldLauncherDef.AmmoMagazinesId);
                    for (int i = 0; i < ammoList.Count; ++i)
                    {
                        if (ammoList[i].SubtypeName == "Missile200mm_Nuclear")
                        {
                            ammoList.RemoveAt(i);
                            break;
                        }
                    }

                    handheldLauncherDef.AmmoMagazinesId = ammoList.ToArray();
                }
            }

            if (ConfigData.NuclearMissileLauncherConfig.NuclearMissileMaxDistance > 0)
            {
                var nuclearMissileAmmo = MyDefinitionManager.Static.GetAmmoDefinition(MyVisualScriptLogicProvider.GetDefinitionId("AmmoDefinition", "Missile_Nuclear"));
                if (nuclearMissileAmmo != null)
                {
                    nuclearMissileAmmo.MaxTrajectory = ConfigData.NuclearMissileLauncherConfig.NuclearMissileMaxDistance;
                }
            }

            var uraniumDefClass = MyDefinitionManager.Static.GetBlueprintClass("Components");
            var uraniumDef = MyDefinitionManager.Static.GetBlueprintDefinition(MyVisualScriptLogicProvider.GetDefinitionId("BlueprintDefinition", "EnrichedUraniumCoreComponent"));
            if (uraniumDef != null)
            {
                if (uraniumDefClass != null && ConfigData.ComponentsConfig.PlayerCanMakeEnrichedUraniumCore)
                    uraniumDefClass.AddBlueprint(uraniumDef);

                if (ConfigData.ComponentsConfig.EnrichedUraniumCoreProductionTimeMultiplier > 0f)
                    uraniumDef.BaseProductionTimeInSeconds *= ConfigData.ComponentsConfig.EnrichedUraniumCoreProductionTimeMultiplier;

                if (ConfigData.ComponentsConfig.EnrichedUraniumCoreIngotRequirementMultiplier > 0)
                    uraniumDef.Prerequisites[0].Amount *= ConfigData.ComponentsConfig.EnrichedUraniumCoreIngotRequirementMultiplier;
            }

            var missileDefClass = MyDefinitionManager.Static.GetBlueprintClass("EliteConsumables");
            var missileDef = MyDefinitionManager.Static.GetBlueprintDefinition(MyVisualScriptLogicProvider.GetDefinitionId("BlueprintDefinition", "Missile200mm_Nuclear"));
            if (missileDef != null)
            {
                if (missileDefClass != null && ConfigData.ComponentsConfig.PlayerCanMakeNuclearMissileAmmo)
                    missileDefClass.AddBlueprint(missileDef);

                if (ConfigData.ComponentsConfig.NuclearMissileProductionTimeMultiplier > 0f)
                    missileDef.BaseProductionTimeInSeconds *= ConfigData.ComponentsConfig.NuclearMissileProductionTimeMultiplier;

                if (ConfigData.ComponentsConfig.NuclearMissileAmmoMagnesiumRequirementMultiplier >= 0)
                {
                    if (ConfigData.ComponentsConfig.NuclearMissileAmmoMagnesiumRequirementMultiplier == 0)
                    {
                        var pp = new List<MyBlueprintDefinitionBase.Item>(missileDef.Prerequisites);
                        pp.RemoveAt(5);
                        missileDef.Prerequisites = pp.ToArray();
                    }
                    else
                    {
                        missileDef.Prerequisites[5].Amount *= ConfigData.ComponentsConfig.NuclearMissileAmmoMagnesiumRequirementMultiplier;
                    }
                }

                if (ConfigData.ComponentsConfig.NuclearMissileAmmoPlatinumRequirementMultiplier >= 0)
                {
                    if (ConfigData.ComponentsConfig.NuclearMissileAmmoPlatinumRequirementMultiplier == 0)
                    {
                        var pp = new List<MyBlueprintDefinitionBase.Item>(missileDef.Prerequisites);
                        pp.RemoveAt(4);
                        missileDef.Prerequisites = pp.ToArray();
                    }
                    else
                    {
                        missileDef.Prerequisites[4].Amount *= ConfigData.ComponentsConfig.NuclearMissileAmmoPlatinumRequirementMultiplier;
                    }
                }

                if (ConfigData.ComponentsConfig.NuclearMissileAmmoUraniumRequirementMultiplier >= 0)
                {
                    if (ConfigData.ComponentsConfig.NuclearMissileAmmoUraniumRequirementMultiplier == 0)
                    {
                        var pp = new List<MyBlueprintDefinitionBase.Item>(missileDef.Prerequisites);
                        pp.RemoveAt(4);
                        missileDef.Prerequisites = pp.ToArray();
                    }
                    else
                    {
                        missileDef.Prerequisites[3].Amount *= ConfigData.ComponentsConfig.NuclearMissileAmmoUraniumRequirementMultiplier;
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            Network.Unregister();
        }

        public override void BeforeStart()
        {
            SetUpdateOrder(MyUpdateOrder.BeforeSimulation);
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            }

            Instance = this;
        }

        private void Entities_OnEntityAdd(IMyEntity obj)
        {
            if (obj is IMyDestroyableObject && (obj as IMyDestroyableObject).Integrity == 1f)
            {
                var builder = obj.GetObjectBuilder() as MyObjectBuilder_Missile;
                if (builder != null && builder.AmmoMagazineId == nuclearMissileAmmoId)
                {
                    //MyAPIGateway.Utilities.ShowNotification($"ENT: {obj} : {(obj as IMyDestroyableObject).Integrity} : {obj.GetType()} : {builder.AmmoMagazineId}", 10000);
                    var newMissileData = new NuclearMissileInfo(obj as IMyEntity);
                    missileData.Add(newMissileData);

                    Network.SendNewMissileInfo(newMissileData);
                }
            }
        }

        private int clientRefreshCtr;
        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                for (int i = 0; i < missileData.Count; ++i)
                {
                    if (missileData[i].Update())
                    {
                        missileData.RemoveAt(i--);
                    }
                }

                clientRefreshCtr = (clientRefreshCtr + 1) % 600;
                if (clientRefreshCtr == 0 && missileData.Count > 0)
                {
                    Network.SendAllMissileInfo(missileData);
                }
            }
            else
            {
                for (int i = 0; i < missileData.Count; ++i)
                {
                    if (missileData[i].UpdateClient())
                    {
                        missileData.RemoveAt(i--);
                    }
                }
            }
        }

        public List<NuclearMissileInfo> missileData = new List<NuclearMissileInfo>();

        public class NuclearMissileInfo
        {
            public IMyEntity entity;
            public Vector3D gravity;
            public Vector3D velocity;
            public Vector3D position;

            private Vector3D forward;
            private Vector3D up;
            private int updateCtr;
            private double startSpeed;
            private IMyVoxelBase voxelMap;

            public NuclearMissileInfo(IMyEntity entity)
            {
                this.entity = entity;
                this.velocity = entity.Physics.LinearVelocity + entity.WorldMatrix.Forward * (ConfigData.NuclearMissileLauncherConfig.NuclearMissileLaunchSpeed - 100);
                startSpeed = velocity.Length();
                this.position = entity.WorldMatrix.Translation;

                //MyAPIGateway.Utilities.ShowNotification($"STARTSPEED: {entity.Physics.LinearVelocity.Length()}", 10000);

                forward = entity.WorldMatrix.Forward;
                up = entity.WorldMatrix.Up;

                if (ConfigData.NuclearMissileLauncherConfig.NuclearMissileGravityMultiplier > 0F)
                {
                    MyPlanet p = MyGamePruningStructure.GetClosestPlanet(entity.GetPosition());
                    gravity = p != null ? p.Components.Get<MyGravityProviderComponent>().GetWorldGravity(entity.GetPosition()) * ConfigData.NuclearMissileLauncherConfig.NuclearMissileGravityMultiplier : Vector3.Zero;
                }
            }

            public NuclearMissileInfo(IMyEntity entity, Vector3D position, Vector3D velocity, Vector3D gravity)
            {
                this.entity = entity;
                this.position = position;
                this.velocity = velocity;
                this.gravity = gravity;
            }

            public bool Update()
            {
                if (entity.InScene)
                {
                    updateCtr = (updateCtr + 1) % 600;
                    if (updateCtr == 0)
                    {
                        if (ConfigData.NuclearMissileLauncherConfig.NuclearMissileGravityMultiplier > 0F)
                        {
                            MyPlanet p = MyGamePruningStructure.GetClosestPlanet(entity.GetPosition());
                            gravity = p != null ? p.Components.Get<MyGravityProviderComponent>().GetWorldGravity(entity.GetPosition()) * ConfigData.NuclearMissileLauncherConfig.NuclearMissileGravityMultiplier : Vector3.Zero;
                        }

                        BoundingSphereD sph = new BoundingSphereD(position + velocity * 0.1, 5);
                        voxelMap = MyAPIGateway.Session.VoxelMaps.GetOverlappingWithSphere(ref sph);
                    }
                    
                    if (updateCtr % 20 == 0)
                    {
                        velocity += gravity * (MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 20);
                        double speed = velocity.Length();
                        forward = velocity / speed;
                        up = Vector3D.Cross(forward, Vector3D.One);

                        //Vector4 color = Color.Red;
                        //MySimpleObjectDraw.DrawLine(position + forward, position + (forward * (1 + speed * 0.5)), MyStringId.GetOrCompute("WeaponLaser"), ref color, 5f);

                        if (voxelMap != null)
                        {
                            Vector3D checkPos = position + velocity * 0.1;
                            try
                            {
                                if (voxelMap.DoOverlapSphereTest(1, checkPos))
                                {
                                    Vector3D checkOffset = velocity * -MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS_HALF;
                                    for (int i = 0; i < 40; ++i)
                                    {
                                        checkPos += checkOffset;
                                        if (!voxelMap.DoOverlapSphereTest(1, checkPos))
                                        {
                                            //MyAPIGateway.Utilities.ShowNotification($"VOXEL: {i}", 10000);
                                            break;
                                        }
                                    }

                                    (entity as IMyDestroyableObject).DoDamage(1, MyStringHash.NullOrEmpty, true);

                                    Detonate(checkPos, up);

                                    return true;
                                }
                            }
                            catch (Exception e)
                            {
                                MyLog.Default.WriteLineAndConsole($"NuclearWeapons::Error: {e.Message}");
                                //MyAPIGateway.Utilities.ShowNotification($"NuclearWeapons::Error: {e.Message}", 2000);

                                Detonate(checkPos, up);
                                return true;
                            }
                        }
                    }

                    position += velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                    entity.WorldMatrix = MatrixD.CreateWorld(position, forward, up);

                    //MyAPIGateway.Utilities.ShowNotification($"VEL: {entity.Physics.LinearVelocity.Length()} : {velocity.Length()}", 5);

                    return false;
                }

                //MyAPIGateway.Utilities.ShowNotification($"CRASH!", 10000);

                Detonate(position, up);

                return true;
            }

            public bool UpdateClient()
            {
                if (entity.InScene)
                {
                    updateCtr = (updateCtr + 1) % 20;
                    if (updateCtr == 0)
                    {
                        velocity += gravity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 20;
                        double speed = velocity.Length();
                        forward = velocity / speed;
                        up = Vector3D.Cross(forward, Vector3D.One);
                    }

                    position += velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                    entity.WorldMatrix = MatrixD.CreateWorld(position, forward, up);

                    return false;
                }

                return true;
            }

            private void Detonate(Vector3D position, Vector3D up)
            {
                BoundingSphereD sph = new BoundingSphereD(position, ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius);
                List<MyEntity> ents = new List<MyEntity>();
                MyGamePruningStructure.GetAllEntitiesInSphere(ref sph, ents, MyEntityQueryType.Both);
                List<IMySlimBlock> wBlocks = new List<IMySlimBlock>();
                foreach (var ent in ents)
                {
                    if (ent is IMyCubeGrid && (ent as IMyCubeGrid).GridSizeEnum == VRage.Game.MyCubeSize.Large)
                    {
                        (ent as IMyCubeGrid).GetBlocks(wBlocks, (IMySlimBlock block) => { return block.FatBlock is IMyWarhead && (block.FatBlock as IMyWarhead).IsArmed && (position - block.FatBlock.GetPosition()).LengthSquared() < MAX_RADIUS_SQR; });
                        foreach (var wBlock in wBlocks)
                        {
                            if (wBlock.FatBlock.InScene)
                            {
                                (wBlock.FatBlock as IMyWarhead).Detonate();
                            }
                        }
                    }
                }

                MyAPIUtilities.Static.SendModMessage(EXPLOSION_MOD_MESSAGE_ID, string.Join(";", position.X, position.Y, position.Z, up.X, up.Y, up.Z, 0f, 0f, 0f, 100F));
            }
        }
    }
}