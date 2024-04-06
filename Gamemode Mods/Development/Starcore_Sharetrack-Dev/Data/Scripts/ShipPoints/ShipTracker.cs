using Draygo.API;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Utils;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using CoreSystems.Api;
using CoreSystems;
using VRage.Audio;
using static CoreSystems.Api.WcApiDef.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using DefenseShields;

namespace klime.PointCheck
{

    [ProtoContract]
    public class ShipTracker
    {
        //Instance
        public IMyCubeGrid Grid { get; private set; }
        public IMyPlayer Owner { get; private set; }
        private int lastHash = 0; // Add this as a class field to store the hash value from the last update.


        //passable
        [ProtoMember(1)] public string GridName;
        [ProtoMember(2)] public string FactionName;
        [ProtoMember(3)] public int LastUpdate;
        [ProtoMember(4)] public long GridID;
        [ProtoMember(5)] public long OwnerID;
        [ProtoMember(6)] public int Bpts;
        [ProtoMember(7)] public float InstalledThrust;
        [ProtoMember(8)] public float Mass;
        [ProtoMember(9)] public float Heavyblocks;
        [ProtoMember(10)] public int BlockCount;
        [ProtoMember(11)] public float ShieldStrength;
        [ProtoMember(12)] public float CurrentShieldStrength;
        [ProtoMember(13)] public int PCU;
        //[ProtoMember(14)] public float DPS;
        [ProtoMember(16)] public Dictionary<string, int> GunL = new Dictionary<string, int>();

        [ProtoMember(17)] public string OwnerName;
        [ProtoMember(18)] public bool IsFunctional = false;
        [ProtoMember(19)] public float CurrentIntegrity;
        [ProtoMember(20)] public float OriginalIntegrity = -1;
        [ProtoMember(21)] public int ShieldHeat;
        [ProtoMember(22)] public Vector3 Position;
        [ProtoMember(23)] public Vector3 FactionColor = Vector3.One;
        [ProtoMember(24)] public float OriginalPower = -1;
        [ProtoMember(25)] public float CurrentPower;
        [ProtoMember(26)] public Dictionary<string, int> SBL = new Dictionary<string, int>();
        [ProtoMember(27)] public float CurrentGyro;

        [ProtoMember(28)] public int movementPercentage = 0;
        [ProtoMember(29)] public int powerPercentage = 0;
        [ProtoMember(30)] public int offensivePercentage = 0;
        [ProtoMember(31)] public int miscPercentage = 0;
        [ProtoMember(32)] public int pdPercentage = 0;
        [ProtoMember(33)] public int pdInvest = 0;
        [ProtoMember(34)] public int MovementBps = 0;
        [ProtoMember(35)] public int PowerBps = 0;
        [ProtoMember(36)] public int OffensiveBps = 0;
        [ProtoMember(37)] public int MiscBps = 0;
        [ProtoMember(38)] public Vector3 OriginalFactionColor = Vector3.One;
        [ProtoMember(39)] public bool hasShield;

        [ProtoMember(40)] public Dictionary<string, int> SubgridGunL = new Dictionary<string, int>();
        public ShipTracker() { }

        public ShipTracker(IMyCubeGrid grid)
        {
            this.Grid = grid;
            this.GridID = grid.EntityId;

            grid.OnClose += OnClose;
            Update();
        }

        public void OnClose(IMyEntity e)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                PointCheck.Sending.Remove(e.EntityId);
                PointCheck.Data.Remove(e.EntityId);
                DisposeHud();
            }
            e.OnClose -= OnClose;
        }
        private List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();
        private List<IMySlimBlock> tmpBlocks = new List<IMySlimBlock>();

        public void Update()
        {

            for (int j = 0; j < tmpBlocks.Count; j++)
            {
                var slim = tmpBlocks[j];
                if (slim?.CubeGrid == null || slim.IsDestroyed || slim.FatBlock == null)
                    continue;
            }
            LastUpdate = 5;
            if (Grid != null && Grid.Physics != null)
            {
                Reset();
                connectedGrids.Clear();
                MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical, connectedGrids);
                if (connectedGrids.Count > 0)
                {
                    Mass = (Grid as MyCubeGrid).GetCurrentMass();
                    bool hasPower = false, hasCockpit = false, hasThrust = false, hasGyro = false;
                    float movementBpts = 0, powerBpts = 0, offensiveBpts = 0, MiscBpts = 0;
                    int bonusBpts = 0, pdBpts = 0; // Initial value for point defense battlepoints
                    string controller = null;

                    TempBlockCheck(ref hasPower, ref hasCockpit, ref hasThrust, ref hasGyro, ref movementBpts, ref powerBpts, ref offensiveBpts, ref MiscBpts, ref bonusBpts, ref pdBpts, ref controller);

                    // pre-calculate totalBpts and totalBptsInv
                    float totalBpts = movementBpts + powerBpts + offensiveBpts + MiscBpts;
                    float totalBptsInv = totalBpts > 0 ? 100f / totalBpts : 100f / (totalBpts + .1f);

                    // pre-calculate offensiveBptsInv for point defense percentage
                    float offensiveBptsInv = offensiveBpts > 0 ? 100f / offensiveBpts : 100f / (offensiveBpts + .1f);

                    // calculate percentage of Bpts for each block type
                    movementPercentage = (int)(movementBpts * totalBptsInv + 0.5f);
                    powerPercentage = (int)(powerBpts * totalBptsInv + 0.5f);
                    offensivePercentage = (int)(offensiveBpts * totalBptsInv + 0.5f);
                    miscPercentage = (int)(MiscBpts * totalBptsInv + 0.5f);

                    // calculate percentage of point defense Bpts of offensive Bpts
                    pdPercentage = (int)(pdBpts * offensiveBptsInv + 0.5f);

                    pdInvest = (int)pdBpts;
                    MiscBps = (int)MiscBpts;
                    PowerBps = (int)powerBpts;
                    OffensiveBps = (int)offensiveBpts;
                    MovementBps = (int)movementBpts;

                    IMyCubeGrid mainGrid = connectedGrids[0];
                    FactionName = "None";
                    OwnerName = "Unowned";

                    IsFunctional = hasPower && hasCockpit && hasGyro;

                    if (mainGrid.BigOwners != null && mainGrid.BigOwners.Count > 0)
                    {
                        OwnerID = mainGrid.BigOwners[0];
                        Owner = PointCheck.GetOwner(OwnerID);
                        OwnerName = controller ?? Owner?.DisplayName ?? GridName;

                        if (!string.IsNullOrEmpty(OwnerName) && OwnerName != GridName)
                        {
                            //OwnerName = OwnerName.Substring(1);
                            OwnerName = OwnerName;

                        }

                        var f = MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(OwnerID);
                        if (f != null)
                        {
                            FactionName = f.Tag ?? FactionName;
                            FactionColor = ColorMaskToRGB(f.CustomColor);
                            OriginalFactionColor = f.CustomColor;
                            //MyAPIGateway.Utilities.ShowNotification("RealFac " + f.CustomColor);
                        }


                    }

                    GridName = Grid.DisplayName;
                    Position = Grid.Physics.CenterOfMassWorld;


                    IMyTerminalBlock shield_block = null;
                    foreach (var g in connectedGrids)
                    {
                        if ((shield_block = PointCheck.SH_api.GetShieldBlock(g)) != null)
                        {
                            break;
                        }
                    }

                    if (shield_block != null)
                    {
                        ShieldStrength = PointCheck.SH_api.GetMaxHpCap(shield_block);
                        CurrentShieldStrength = PointCheck.SH_api.GetShieldPercent(shield_block);
                        ShieldHeat = PointCheck.SH_api.GetShieldHeat(shield_block);
                    }

                    OriginalIntegrity = OriginalIntegrity == -1 ? CurrentIntegrity : OriginalIntegrity;
                    OriginalPower = OriginalPower == -1 ? CurrentPower : OriginalPower;

                }

            }

        }

        private void TempBlockCheck(ref bool hasPower, ref bool hasCockpit, ref bool hasThrust, ref bool hasGyro, ref float movementBpts, ref float powerBpts, ref float offensiveBpts, ref float MiscBpts, ref int bonusBpts, ref int pdBpts, ref string controller)
        {
            bool hasCTC = false;

            for (int i = 0; i < connectedGrids.Count; i++)
            {
                IMyCubeGrid grid = connectedGrids[i];
                if (grid != null && grid.Physics != null)
                {
                    MyCubeGrid subgrid = grid as MyCubeGrid;
                    BlockCount += subgrid.BlocksCount;
                    PCU += subgrid.BlocksPCU;
                    tmpBlocks.Clear();
                    grid.GetBlocks(tmpBlocks);

                    foreach (var block in tmpBlocks)
                    {
                        string subtype = block.BlockDefinition?.Id.SubtypeName;
                        string id = "";

                        if (block.FatBlock is IMyOxygenGenerator)
                        {
                            id = "H2O2Generator";
                        }
                        else if (block.FatBlock is IMyGasTank)
                        {
                            id = "HydrogenTank";
                        }
                        else if (block.FatBlock is IMyMotorStator && subtype == "SubgridBase")
                        {
                            id = "Invincible Subgrid";
                        }
                        else if (block.FatBlock is IMyUpgradeModule)
                        {
                            switch (subtype)
                            {
                                case "LargeEnhancer":
                                    id = "Shield Enhancer";
                                    break;
                                case "EmitterL":
                                case "EmitterLA":
                                    id = "Shield Emitter";
                                    break;
                                case "LargeShieldModulator":
                                    id = "Shield Modulator";
                                    break;
                                case "DSControlLarge":
                                case "DSControlTable":
                                    id = "Shield Controller";
                                    break;
                                case "AQD_LG_GyroBooster":
                                    id = "Gyro Booster";
                                    break;
                                case "AQD_LG_GyroUpgrade":
                                    id = "Large Gyro Booster";
                                    break;
                            }
                        }
                        else if (block.FatBlock is IMyReactor)
                        {
                            switch (subtype)
                            {
                                case "LargeBlockLargeGenerator":
                                case "LargeBlockLargeGeneratorWarfare2":
                                    id = "Large Reactor";
                                    break;
                                case "LargeBlockSmallGenerator":
                                case "LargeBlockSmallGeneratorWarfare2":
                                    id = "Small Reactor";
                                    break;
                            }
                        }
                        else if (block.FatBlock is IMyGyro)
                        {
                            switch (subtype)
                            {
                                case "LargeBlockGyro":
                                    id = "Small Gyro";
                                    break;
                                case "AQD_LG_LargeGyro":
                                    id = "Large Gyro";
                                    break;
                            }
                        }
                        else if (block.FatBlock is IMyCameraBlock)
                        {
                            switch (subtype)
                            {
                                case "MA_Buster_Camera":
                                    id = "Buster Camera";
                                    break;
                                case "LargeCameraBlock":
                                    id = "Camera";
                                    break;
                            }
                        }

                        if (!string.IsNullOrEmpty(id))
                        {
                            if (SBL.ContainsKey(id))
                            {
                                SBL[id] += 1;
                            }
                            else
                            {
                                SBL.Add(id, 1);
                            }
                        }

                        if (block.BlockDefinition != null && !string.IsNullOrEmpty(subtype))
                        {
                            if (subtype.IndexOf("Heavy", StringComparison.OrdinalIgnoreCase) >= 0 && subtype.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Heavyblocks++;
                            }
                            if (block.FatBlock != null && !(block.FatBlock is IMyMotorRotor) && !(block.FatBlock is IMyMotorStator) && subtype != "SC_SRB")
                            {
                                CurrentIntegrity += block.Integrity;
                            }
                        }

                        if (block.FatBlock is IMyThrust || block.FatBlock is IMyGyro)
                        {
                            movementBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                        }
                        else if (block.FatBlock is IMyReactor || block.FatBlock is IMyBatteryBlock)
                        {
                            powerBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                        }
                        else
                        {
                            offensiveBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                        }
                    }
                    FatHandling(ref hasPower, ref hasCockpit, ref hasThrust, ref hasGyro, ref hasCTC, ref movementBpts, ref powerBpts, ref offensiveBpts, ref MiscBpts, ref bonusBpts, ref pdBpts, ref controller, subgrid);
                }
            }

            if (hasCTC)
            {
                foreach (KeyValuePair<string, int> weapon in SubgridGunL)
                {
                    // Currently takes a global 20% extra cost wich is not multiplicative with clibing cost
                    int bonusWeaponBP = (int)(PointCheck.PointValues.GetValueOrDefault(weapon.Key, 0) * weapon.Value * 0.2);
                    offensiveBpts += bonusWeaponBP;
                    Bpts += bonusWeaponBP;
                }
            }
            
        }

        private void FatHandling(ref bool hasPower, ref bool hasCockpit, ref bool hasThrust, ref bool hasGyro, ref bool hasCTC, ref float movementBpts, ref float powerBpts, ref float offensiveBpts, ref float MiscBpts, ref int bonusBpts, ref int pdBpts, ref string controller, MyCubeGrid subgrid)
        {
            // Variables used for extra cost on subgrid weapons (rotorturrets)
            bool isMainGrid = false;
            Dictionary<string, int> tempGuns = new Dictionary<string, int>();

            VRage.Collections.ListReader<MyCubeBlock> blocklist = subgrid.GetFatBlocks();
            for (int i1 = 0; i1 < blocklist.Count; i1++)
            {
                MyCubeBlock block = blocklist[i1];
                string id = block?.BlockDefinition?.Id.SubtypeId.ToString();

                if (!string.IsNullOrEmpty(id))
                {
                    if (PointCheck.PointValues.ContainsKey(id))
                    {
                        Bpts += PointCheck.PointValues[id];
                    }
                }
                else
                {
                    if (block is IMyGravityGeneratorBase)
                    {
                        Bpts += PointCheck.PointValues.GetValueOrDefault("GravityGenerator", 0);
                    }
                    else if (block is IMySmallGatlingGun)
                    {
                        Bpts += PointCheck.PointValues.GetValueOrDefault("SmallGatlingGun", 0);
                    }
                    else if (block is IMyLargeGatlingTurret)
                    {
                        Bpts += PointCheck.PointValues.GetValueOrDefault("LargeGatlingTurret", 0);
                    }
                    else if (block is IMySmallMissileLauncher)
                    {
                        Bpts += PointCheck.PointValues.GetValueOrDefault("SmallMissileLauncher", 0);
                    }
                    else if (block is IMyLargeMissileTurret)
                    {
                        Bpts += PointCheck.PointValues.GetValueOrDefault("LargeMissileTurret", 0);
                    }
                }

                bool isTerminalBlock = block is IMyTerminalBlock;

                if ((PointCheck.PointValues.ContainsKey(id) && !isTerminalBlock) || block is IMyGyro || block is IMyReactor || block is IMyBatteryBlock || block is IMyCockpit || block is IMyDecoy || block is IMyShipDrill || block is IMyGravityGeneratorBase || block is IMyShipWelder || block is IMyShipGrinder || block is IMyRadioAntenna || (block is IMyThrust && !(block.BlockDefinition.Id.SubtypeName == "LargeCameraBlock") && !(block.BlockDefinition.Id.SubtypeName == "MA_Buster_Camera") && !(block.BlockDefinition.Id.SubtypeName == "BlinkDriveLarge")))
                {
                    var typeId = block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "");

                    if (SBL.ContainsKey(typeId))
                    {
                        SBL[typeId] += 1;
                    }
                    else if (typeId != "Reactor" && typeId != "Gyro")
                    {
                        SBL.Add(typeId, 1);
                    }

                    bool hasShield = PointCheck.SH_api.GetShieldBlock(block) != null;

                    if (block is IMyThrust)
                    {
                        InstalledThrust += (block as IMyThrust).MaxEffectiveThrust;
                        hasThrust = true;
                    }

                    if (block is IMyCockpit && (block as IMyCockpit).CanControlShip)
                    {
                        if (hasCockpit && !isMainGrid)
                        {
                            // Prevent players from placing Cockpits on subgrids to circumvent BP increase of weapons on subgrids
                            MyAPIGateway.Utilities.ShowNotification("Illegal Cockpit placement on subgrid", 1000, font: "Red");
                        }

                        hasCockpit = true;
                        isMainGrid = true;
                    }

                    if (block is IMyReactor || block is IMyBatteryBlock)
                    {
                        hasPower = true;
                        CurrentPower += (block as IMyPowerProducer).MaxOutput;
                    }

                    if (block is IMyGyro)
                    {
                        hasGyro = true;
                        CurrentGyro += ((MyDefinitionManager.Static.GetDefinition((block as IMyGyro).BlockDefinition) as MyGyroDefinition).ForceMagnitude * (block as IMyGyro).GyroStrengthMultiplier);
                    }

                    if (block is IMyCockpit)
                    {
                        var pilot = (block as IMyCockpit).ControllerInfo?.Controller?.ControlledEntity?.Entity;

                        if (pilot is IMyCockpit)
                        {
                            controller = (pilot as IMyCockpit).Pilot.DisplayName;
                        }
                    }
                }
                else if ((PointCheck.PointValues.ContainsKey(id) && isTerminalBlock) && !(block is IMyGyro) && !(block is IMyReactor) && !(block is IMyBatteryBlock) && !(block is IMyCockpit) && !(block is IMyDecoy) && !(block is IMyShipDrill) && !(block is IMyGravityGeneratorBase) && !(block is IMyShipWelder) && !(block is IMyShipGrinder) && !(block is IMyThrust) && !(block is IMyRadioAntenna) && !(block is IMyUpgradeModule && !(block.BlockDefinition.Id.SubtypeName == "BlinkDriveLarge")))
                {
                    IMyTerminalBlock tBlock = block as IMyTerminalBlock;
                    var t_N = tBlock.DefinitionDisplayNameText;
                    var mCs = 0f;

                    ClimbingCostRename(ref t_N, ref mCs);

                    if (GunL.ContainsKey(t_N))
                    {
                        GunL[t_N] += 1;
                    }
                    else
                    {
                        GunL.Add(t_N, 1);
                    }

                    if ((mCs > 0) && GunL[t_N] > 1)
                    {
                        bonusBpts = (int)(PointCheck.PointValues[id] * ((GunL[t_N] - 1) * mCs));
                        Bpts += bonusBpts;
                    }
                }

                bool hasWeapon;
                bool isPointDefense;
                bool isWeapon;
                if (PointCheckHelpers.weaponsDictionary.TryGetValue(block.BlockDefinition.Id.SubtypeName, out isWeapon) && isWeapon)
                {
                    offensiveBpts += PointCheck.PointValues.GetValueOrDefault(id, 0) + bonusBpts;

                    if (tempGuns.ContainsKey(id))
                    {
                        tempGuns[id] += 1;
                    }
                    else
                    {
                        tempGuns.Add(id, 1);
                    }

                    // isPointDefense;
                    if (PointCheckHelpers.pdDictionary.TryGetValue(block.BlockDefinition.Id.SubtypeName, out isPointDefense) && isPointDefense)
                    {
                        pdBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                    }
                }
                else
                {
                    string blockType = block.BlockDefinition.Id.SubtypeName;

                    if (block is IMyThrust || block is IMyGyro || blockType == "BlinkDriveLarge" || blockType.Contains("Afterburner"))
                    {
                        movementBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                    }
                    else if (block is IMyReactor || block is IMyBatteryBlock)
                    {
                        powerBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                    }
                    else
                    {
                        MiscBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                    }
                }

                if (id == "LargeTurretControlBlock")
                { 
                    hasCTC = true;
                }
            }

            // Adding extra points to guns when they are not on the main grid
            if (!isMainGrid && connectedGrids.Count != 1)
            {
                foreach (KeyValuePair<string, int> weapon in tempGuns)
                {
                    if (SubgridGunL.ContainsKey(weapon.Key))
                    {
                        SubgridGunL[weapon.Key] += 1;
                    }
                    else
                    {
                        SubgridGunL.Add(weapon.Key, 1);
                    }
                }
            }

        }

        private static void ClimbingCostRename(ref string t_N, ref float mCs)
        {
            switch (t_N)
            {
                case "Blink Drive Large":
                    t_N = "Blink Drive";
                    mCs = 0.15f;
                    break;
                case "Project Pluto (SLAM)":
                case "SLAM":
                    t_N = "SLAM";
                    mCs = 0.25f;
                    break;
                case "[BTI] MRM-10 Modular Launcher 45":
                case "[BTI] MRM-10 Modular Launcher 45 Reversed":
                case "[BTI] MRM-10 Modular Launcher":
                case "[BTI] MRM-10 Modular Launcher Middle":
                case "[BTI] MRM-10 Launcher":
                    t_N = "MRM-10 Launcher";
                    mCs = 0.04f;
                    break;
                case "[BTI] LRM-5 Modular Launcher 45 Reversed":
                case "[BTI] LRM-5 Modular Launcher 45":
                case "[BTI] LRM-5 Modular Launcher Middle":
                case "[BTI] LRM-5 Modular Launcher":
                case "[BTI] LRM-5 Launcher":
                    t_N = "LRM-5 Launcher";
                    mCs = 0.10f;
                    break;
                case "[MA] Gimbal Laser T2 Armored":
                case "[MA] Gimbal Laser T2 Armored Slope 45":
                case "[MA] Gimbal Laser T2 Armored Slope 2":
                case "[MA] Gimbal Laser T2 Armored Slope":
                case "[MA] Gimbal Laser T2":
                    t_N = "Gimbal Laser T2";
                    mCs = 0f;
                    break;
                case "[MA] Gimbal Laser Armored Slope 45":
                case "[MA] Gimbal Laser Armored Slope 2":
                case "[MA] Gimbal Laser Armored Slope":
                case "[MA] Gimbal Laser Armored":
                case "[MA] Gimbal Laser":
                    t_N = "Gimbal Laser";
                    mCs = 0f;
                    break;
                case "[ONYX] BR-RT7 Afflictor Slanted Burst Cannon":
                case "[ONYX] BR-RT7 Afflictor 70mm Burst Cannon":
                case "[ONYX] Afflictor":
                    t_N = "Afflictor";
                    mCs = 0f;
                    break;
                case "[MA] Slinger AC 150mm Sloped 30":
                case "[MA] Slinger AC 150mm Sloped 45":
                case "[MA] Slinger AC 150mm Gantry Style":
                case "[MA] Slinger AC 150mm Sloped 45 Gantry":
                case "[MA] Slinger AC 150mm":
                case "[MA] Slinger":
                    t_N = "Slinger";
                    mCs = 0f;
                    break;
                case "[ONYX] Heliod Plasma Pulser":
                    t_N = "Heliod Plasma Pulser";
                    mCs = 0.15f;
                    break;
                case "[MA] UNN Heavy Torpedo Launcher":
                    t_N = "UNN Heavy Torpedo Launcher";
                    mCs = 0.15f;
                    break;
                case "[BTI] SRM-8":
                    t_N = "SRM-8";
                    mCs = 0.15f;
                    break;
                case "[BTI] Starcore Arrow-IV Launcher":
                    t_N = "Starcore Arrow-IV Launcher";
                    mCs = 0.15f;
                    break;
                case "[HAS] Tartarus VIII":
                    t_N = "Tartarus VIII";
                    mCs = 0.15f;
                    break;
                case "[HAS] Cocytus IX":
                    t_N = "Cocytus IX";
                    mCs = 0.15f;
                    break;
                case "[MA] MCRN Torpedo Launcher":
                    t_N = "MCRN Torpedo Launcher";
                    mCs = 0.15f;
                    break;
                case "Flares":
                    t_N = "Flares";
                    mCs = 0.25f;
                    break;
                case "[EXO] Chiasm [Arc Emitter]":
                    t_N = "Chiasm [Arc Emitter]";
                    mCs = 0.15f;
                    break;
                case "[BTI] Medium Laser":
                case "[BTI] Large Laser":
                    t_N = " Laser";
                    mCs = 0.15f;
                    break;
                case "Reinforced Blastplate":
                case "Active Blastplate":
                case "Standard Blastplate A":
                case "Standard Blastplate B":
                case "Standard Blastplate C":
                case "Elongated Blastplate":
                case "7x7 Basedplate":
                    t_N = "Reinforced Blastplate";
                    mCs = 1.00f;
                    break;
                case "[EXO] Taiidan":
                case "[EXO] Taiidan Fighter Launch Rail":
                case "[EXO] Taiidan Bomber Launch Rail":
                case "[EXO] Taiidan Fighter Hangar Bay":
                case "[EXO] Taiidan Bomber Hangar Bay":
                case "[EXO] Taiidan Bomber Hangar Bay Medium":
                case "[EXO] Taiidan Fighter Small Bay":
                    t_N = "Taiidan";
                    mCs = 0.25f;
                    break;
                case "[40k] Gothic Torpedo":
                    t_N = "Gothic Torpedo";
                    mCs = 0.15f;
                    break;
            }
        }


        private static Vector3 ColorMaskToRGB(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }

        private HudAPIv2.HUDMessage nametag;
        public void CreateHud()
        {
            nametag = new HudAPIv2.HUDMessage(new StringBuilder(OwnerName), Vector2D.Zero, Font: "BI_SEOutlined", Blend: BlendTypeEnum.PostPP, HideHud: false, Shadowing: true);
            UpdateHud();
        }

        public void UpdateHud()
        {
            try
            {
                if (nametag != null)
                {
                    nametag.Message.Clear();
                    var camera = MyAPIGateway.Session.Camera;
                    var distanceThreshold = 20000;
                    var maxAngle = 60; // Adjust this angle as needed

                    if (nametag != null)
                    {
                        var e = MyEntities.GetEntityById(GridID);
                        Vector3D pos;

                        if (e != null && e is IMyCubeGrid)
                        {
                            var g = e as IMyCubeGrid;
                            pos = g.Physics.CenterOfMassWorld;
                        }
                        else
                        {
                            pos = Position;
                        }

                        Vector3D targetHudPos = camera.WorldToScreen(ref pos);
                        Vector2D newOrigin = new Vector2D(targetHudPos.X, targetHudPos.Y);

                        nametag.InitialColor = new Color(FactionColor);
                        Vector3D cameraForward = camera.WorldMatrix.Forward;
                        Vector3D toTarget = pos - camera.WorldMatrix.Translation;
                        float fov = camera.FieldOfViewAngle;
                        var angle = GetAngleBetweenDegree(toTarget, cameraForward);

                        bool stealthed = ((uint)e.Flags & 0x1000000) > 0;
                        bool visible = !(newOrigin.X > 1 || newOrigin.X < -1 || newOrigin.Y > 1 || newOrigin.Y < -1) && angle <= fov && !stealthed;

                        var distance = Vector3D.Distance(camera.WorldMatrix.Translation, pos);
                        nametag.Scale = 1 - MathHelper.Clamp(distance / distanceThreshold, 0, 1) + (30 / Math.Max(maxAngle, angle * angle * angle));
                        nametag.Origin = new Vector2D(targetHudPos.X, targetHudPos.Y + (MathHelper.Clamp(-0.000125 * distance + 0.25, 0.05, 0.25)));
                        nametag.Visible = PointCheckHelpers.NameplateVisible && visible;
                        nametag.Message.Clear();

                        if (IsFunctional)
                        {
                            string nameText = PointCheck.viewstat == 0 || PointCheck.viewstat == 2 ? OwnerName : GridName;
                            nametag.Message.Append(nameText);

                            if (PointCheck.viewstat == 2)
                            {
                                nametag.Message.Append("\n" + GridName);
                            }
                        }
                        else
                        {
                            string nameText = PointCheck.viewstat == 0 || PointCheck.viewstat == 2 ? OwnerName + "<color=white>:[Dead]" : GridName + "<color=white>:[Dead]";
                            nametag.Message.Append(nameText);

                            if (PointCheck.viewstat == 2)
                            {
                                nametag.Message.Append("\n" + GridName + "<color=white>:[Dead]");
                            }
                        }

                        nametag.Offset = -nametag.GetTextLength() / 2;
                    }
                }
            }
            catch (Exception)
            {
                // Handle exceptions here, or consider logging them.
            }
        }

        private double GetAngleBetweenDegree(Vector3D vectorA, Vector3D vectorB)
        {
            vectorA.Normalize();
            vectorB.Normalize();
            return Math.Acos(MathHelper.Clamp(vectorA.Dot(vectorB), -1, 1)) * (180.0 / Math.PI);
        }

        public void DisposeHud()
        {
            if (nametag != null)
            {
                nametag.Visible = false;
                nametag.Message.Clear();
                nametag.DeleteMessage();
            }
            nametag = null;
        }

        private void Reset()
        {
            SBL.Clear();
            GunL.Clear();
            SubgridGunL.Clear();
            Bpts = 0;
            InstalledThrust = 0;
            Mass = 0;
            Heavyblocks = 0;
            BlockCount = 0;
            ShieldStrength = 0;
            CurrentShieldStrength = 0;
            CurrentIntegrity = 0;
            CurrentPower = 0;
            PCU = 0;
            //DPS = 0;
        }

    }
}
