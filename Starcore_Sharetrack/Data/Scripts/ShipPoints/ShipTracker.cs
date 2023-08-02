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

namespace klime.PointCheck
{

    [ProtoContract]
    public class ShipTracker
    {
        //Instance
        public IMyCubeGrid Grid { get; private set; }
        public IMyPlayer Owner { get; private set; }


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
        [ProtoMember(11)] public float TotalShieldStrength;
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
            try
            {

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

                        foreach (var grid in connectedGrids)
                        {
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
                                        if (subtype == "LargeEnhancer")
                                        {
                                            id = "Shield Enhancer";
                                        }
                                        else if (subtype == "EmitterL" || subtype == "EmitterLA")
                                        {
                                            id = "Shield Emitter";
                                        }
                                        else if (subtype == "LargeShieldModulator")
                                        {
                                            id = "Shield Modulator";
                                        }
                                        else if (subtype == "DSControlLarge" || subtype == "DSControlTable")
                                        {
                                            id = "Shield Controller";
                                        }
                                        else if (subtype == "AQD_LG_GyroBooster")
                                        {
                                            id = "Gyro Booster";
                                        }
                                        else if (subtype == "AQD_LG_GyroUpgrade")
                                        {
                                            id = "Large Gyro Booster";
                                        }
                                    }
                                    else if (block.FatBlock is IMyReactor)
                                    {
                                        if (subtype == "LargeBlockLargeGenerator" || subtype == "LargeBlockLargeGeneratorWarfare2")
                                        {
                                            id = "Large Reactor";
                                        }
                                        else if (subtype == "LargeBlockSmallGenerator" || subtype == "LargeBlockSmallGeneratorWarfare2")
                                        {
                                            id = "Small Reactor";
                                        }
                                    }
                                    else if (block.FatBlock is IMyGyro)
                                    {
                                        if (subtype == "LargeBlockGyro")
                                        {
                                            id = "Small Gyro";
                                        }
                                        else if (subtype == "AQD_LG_LargeGyro")
                                        {
                                            id = "Large Gyro";
                                        }
                                    }
                                    else if (block.FatBlock is IMyCameraBlock)
                                    {
                                        if (subtype == "MA_Buster_Camera")
                                        {
                                            id = "Buster Camera";
                                        }
                                        else if (subtype == "LargeCameraBlock")
                                        {
                                            id = "Camera";
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
                                        if (subtype.Contains("Heavy") && subtype.Contains("Armor"))
                                        {
                                            Heavyblocks++;
                                        }

                                        if (block.FatBlock != null && !(block.FatBlock is IMyMotorRotor) && !(block.FatBlock is IMyMotorStator) && !(subtype == "SC_SRB"))
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

                                foreach (var b in subgrid.GetFatBlocks())
                                {
                                    string id = b?.BlockDefinition?.Id.SubtypeId.ToString();
                                    if (!string.IsNullOrEmpty(id))
                                    {
                                        if (PointCheck.PointValues.ContainsKey(id))
                                        {
                                            Bpts += PointCheck.PointValues[id];
                                        }
                                    }
                                    else
                                    {

                                        if (b is IMyGravityGeneratorBase)
                                        {
                                            Bpts += PointCheck.PointValues.GetValueOrDefault("GravityGenerator", 0);
                                        }
                                        else if (b is IMySmallGatlingGun)
                                        {
                                            Bpts += PointCheck.PointValues.GetValueOrDefault("SmallGatlingGun", 0);
                                        }
                                        else if (b is IMyLargeGatlingTurret)
                                        {
                                            Bpts += PointCheck.PointValues.GetValueOrDefault("LargeGatlingTurret", 0);
                                        }
                                        else if (b is IMySmallMissileLauncher)
                                        {
                                            Bpts += PointCheck.PointValues.GetValueOrDefault("SmallMissileLauncher", 0);
                                        }
                                        else if (b is IMyLargeMissileTurret)
                                        {
                                            Bpts += PointCheck.PointValues.GetValueOrDefault("LargeMissileTurret", 0);
                                        }
                                    }
                                    //b counts
                                    if ((PointCheck.PointValues.ContainsKey(id) &&
                                          !(b is IMyTerminalBlock)) ||
                                            b is IMyGyro ||
                                            b is IMyReactor ||
                                            b is IMyBatteryBlock ||
                                            b is IMyCockpit ||
                                            b is IMyDecoy ||
                                            b is IMyShipDrill ||
                                            b is IMyGravityGeneratorBase ||
                                            b is IMyShipWelder ||
                                            b is IMyShipGrinder ||
                                            b is IMyRadioAntenna ||
                                            b is IMyThrust
                                            && !(b.BlockDefinition.Id.SubtypeName == "LargeCameraBlock")
                                            && !(b.BlockDefinition.Id.SubtypeName == "MA_Buster_Camera")
                                            && !(b.BlockDefinition.Id.SubtypeName == "BlinkDriveLarge"))
                                    {

                                        var typeID = b.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "");

                                        if (SBL.ContainsKey(typeID))
                                        {
                                            SBL[typeID] += 1;
                                        }
                                        else if (typeID != "Reactor" && typeID != "Gyro")
                                        {
                                            SBL.Add(typeID, 1);
                                        }

                                        if (b is IMyThrust)
                                        {
                                            InstalledThrust += (b as IMyThrust).MaxEffectiveThrust;
                                            hasThrust = true;
                                        }

                                        if (b is IMyCockpit && (b as IMyCockpit).CanControlShip)
                                        {
                                            hasCockpit = true;
                                        }

                                        if (b is IMyReactor || b is IMyBatteryBlock)
                                        {
                                            hasPower = true; CurrentPower += (b as IMyPowerProducer).MaxOutput;
                                        }

                                        if (b is IMyGyro)
                                        {

                                            hasGyro = true;
                                            CurrentGyro += ((MyDefinitionManager.Static.GetDefinition((b as IMyGyro).BlockDefinition) as MyGyroDefinition).ForceMagnitude * (b as IMyGyro).GyroStrengthMultiplier);
                                        }

                                        if (b is IMyCockpit)
                                        {
                                            var p = (b as IMyCockpit).ControllerInfo?.Controller?.ControlledEntity?.Entity;
                                            if (p is IMyCockpit)
                                            {
                                                controller = (p as IMyCockpit).Pilot.DisplayName;
                                            }
                                        }


                                    }
                                    else if ((PointCheck.PointValues.ContainsKey(id) && b is IMyTerminalBlock) &&
                                            !(b is IMyGyro) &&
                                            !(b is IMyReactor) &&
                                            !(b is IMyBatteryBlock) &&
                                            !(b is IMyCockpit) &&
                                            !(b is IMyDecoy) &&
                                            !(b is IMyShipDrill) &&
                                            !(b is IMyGravityGeneratorBase) &&
                                            !(b is IMyShipWelder) &&
                                            !(b is IMyShipGrinder) &&
                                            !(b is IMyThrust) &&
                                            !(b is IMyRadioAntenna) &&
                                            !(b is IMyUpgradeModule &&
                                            !(b.BlockDefinition.Id.SubtypeName == "BlinkDriveLarge")))
                                    {

                                        IMyTerminalBlock tBlock = b as IMyTerminalBlock;
                                        var t_N = tBlock.DefinitionDisplayNameText;
                                        var mCs = 0f;

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
                                            case "MRM-10 Modular Launcher 45":
                                            case "MRM-10 Modular Launcher 45 Reversed":
                                            case "MRM-10 Modular Launcher":
                                            case "MRM-10 Modular Launcher Middle":
                                            case "MRM-10 Launcher":
                                                t_N = "MRM-10 Launcher";
                                                mCs = 0.04f;
                                                break;
                                            case "LRM-5 Modular Launcher 45 Reversed":
                                            case "LRM-5 Modular Launcher 45":
                                            case "LRM-5 Modular Launcher Middle":
                                            case "LRM-5 Modular Launcher":
                                            case "LRM-5 Launcher":
                                                t_N = "LRM-5 Launcher";
                                                mCs = 0.0375f;
                                                break;
                                            case "Gimbal Laser T2 Armored":
                                            case "Gimbal Laser T2 Armored Slope 45":
                                            case "Gimbal Laser T2 Armored Slope 2":
                                            case "Gimbal Laser T2 Armored Slope":
                                            case "Gimbal Laser T2":
                                                t_N = "Gimbal Laser T2";
                                                mCs = 0f;
                                                break;
                                            case "Gimbal Laser Armored Slope 45":
                                            case "Gimbal Laser Armored Slope 2":
                                            case "Gimbal Laser Armored Slope":
                                            case "Gimbal Laser Armored":
                                            case "Gimbal Laser":
                                                t_N = "Gimbal Laser";
                                                mCs = 0f;
                                                break;
                                            case "BR-RT7 Punisher Slanted Burst Cannon":
                                            case "BR-RT7 Punisher 70mm Burst Cannon":
                                            case "Punisher":
                                                t_N = "Punisher";
                                                mCs = 0f;
                                                break;
                                            case "Slinger AC 150mm Sloped 30":
                                            case "Slinger AC 150mm Sloped 45":
                                            case "Slinger AC 150mm Gantry Style":
                                            case "Slinger AC 150mm Sloped 45 Gantry":
                                            case "Slinger AC 150mm":
                                            case "Slinger":
                                                t_N = "Slinger";
                                                mCs = 0f;
                                                break;
                                            case "Starcore Arrow-IV Launcher":
                                            case "SRM-8":
                                            case "M-1 Torpedo":
                                            case "Grimlock Launcher":
                                            case "MCRN Torpedo Launcher":
                                            case "OPA Heavy Torpedo Launcher":
                                            case "OPA Light Missile Launcher":
                                            case "UNN Heavy Torpedo Launcher":
                                            case "UNN Light Torpedo Launcher":
                                            case "200mm 'Thors Wrath' Missile System":
                                            case "Horizon Device - Placeholder":
                                            case "Tartarus VIII":
                                            case "Cocytus IX":
                                            case "M5D-2E HELIOS Plasma Pulser":
                                                mCs = 0.15f;
                                                break;
                                            case "Chiasm [Arc Emitter]":
                                                t_N = "Chiasm";
                                                mCs = 0.15f;
                                                break;
                                            case "Flares":
                                                mCs = 0.25f;
                                                break;

                                                //these names are from the block SBC, NOT the .cs file for the weapon
                                        }




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
                                    if (PointCheck.weaponsDictionary.TryGetValue(b.BlockDefinition.Id.SubtypeName, out hasWeapon) && hasWeapon)
                                    {
                                        offensiveBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                                        offensiveBpts += bonusBpts;

                                        // Check if the weapon is also a point defense weapon
                                        if (PointCheck.pdDictionary.TryGetValue(b.BlockDefinition.Id.SubtypeName, out isPointDefense) && isPointDefense)
                                        {
                                            // Increase point defense battlepoints if the weapon is a point defense weapon
                                            pdBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                                        }
                                    }
                                    else
                                    {
                                        string blockType = b.BlockDefinition.Id.SubtypeName;
                                        if (b is IMyThrust || b is IMyGyro || blockType == "BlinkDriveLarge" || blockType.Contains("Afterburner"))
                                        {
                                            movementBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                                        }
                                        else if (b is IMyReactor || b is IMyBatteryBlock)
                                        {
                                            powerBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                                        }
                                        else
                                        {
                                            MiscBpts += PointCheck.PointValues.GetValueOrDefault(id, 0);
                                        }
                                    }



                                }
                            }
                        }

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
                            TotalShieldStrength = PointCheck.SH_api.GetMaxHpCap(shield_block);
                            CurrentShieldStrength = PointCheck.SH_api.GetShieldPercent(shield_block);
                            ShieldHeat = PointCheck.SH_api.GetShieldHeat(shield_block);
                        }

                        OriginalIntegrity = OriginalIntegrity == -1 ? CurrentIntegrity : OriginalIntegrity;
                        OriginalPower = OriginalPower == -1 ? CurrentPower : OriginalPower;

                    }

                }
            }
            catch { }

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
                nametag.Message.Clear();
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

                    Vector3D targetHudPos = MyAPIGateway.Session.Camera.WorldToScreen(ref pos);
                    Vector2D newOrigin = new Vector2D(targetHudPos.X, targetHudPos.Y);


                    nametag.InitialColor = new Color(FactionColor);

                    Vector3D cameraForward = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
                    Vector3D toTarget = pos - MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                    float fov = MyAPIGateway.Session.Camera.FieldOfViewAngle;
                    var angle = GetAngleBetweenDegree(toTarget, cameraForward);

                    bool stealthed = false;
                    if (((uint)e.Flags & 0x1000000) > 0)
                    {
                        stealthed = true;
                    }

                    bool visible = !(newOrigin.X > 1 || newOrigin.X < -1 || newOrigin.Y > 1 || newOrigin.Y < -1) && angle <= fov && !stealthed;


                    var distance = Vector3D.Distance(MyAPIGateway.Session.Camera.WorldMatrix.Translation, pos);
                    nametag.Scale = 1 - MathHelper.Clamp(distance / 20000, 0, 1) + (30 / Math.Max(60, angle * angle * angle));
                    nametag.Origin = new Vector2D(targetHudPos.X, targetHudPos.Y + (MathHelper.Clamp(-0.000125 * distance + 0.25, 0.05, 0.25)));
                    nametag.Visible = PointCheck.NameplateVisible && visible;

                    nametag.Message.Clear();

                    if (IsFunctional)
                    {
                        if (PointCheck.viewstat == 0 || PointCheck.viewstat == 2)
                        {
                            nametag.Message.Append(OwnerName);
                        }
                        if (PointCheck.viewstat == 1)
                        {
                            nametag.Message.Append(GridName);
                        }
                        if (PointCheck.viewstat == 2)
                        {
                            nametag.Message.Append("\n" + GridName);
                        }
                    }
                    else
                    {
                        //nametag.Message.Clear();


                        if (PointCheck.viewstat == 0 || PointCheck.viewstat == 2)
                        {
                            nametag.Message.Append(OwnerName + "<color=white>:[Dead]");
                        }
                        if (PointCheck.viewstat == 1)
                        {
                            nametag.Message.Append(GridName + "<color=white>:[Dead]");
                        }
                        if (PointCheck.viewstat == 2)
                        {
                            nametag.Message.Append("\n" + GridName + "<color=white>:[Dead]");
                        }

                    }






                    nametag.Offset = -nametag.GetTextLength() / 2;

                }

            }
            catch (Exception)
            {

            }
        }

        private double GetAngleBetweenDegree(Vector3D vectorA, Vector3D vectorB)
        {
            vectorA.Normalize(); vectorB.Normalize();
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
            Bpts = 0;
            InstalledThrust = 0;
            Mass = 0;
            Heavyblocks = 0;
            BlockCount = 0;
            TotalShieldStrength = 0;
            CurrentShieldStrength = 0;
            CurrentIntegrity = 0;
            CurrentPower = 0;
            PCU = 0;
            //DPS = 0;
        }

    }
}
