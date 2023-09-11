using System;
using System.Collections.Generic;
using Digi;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using SpaceEngineers.Game.ModAPI;

namespace MIG.SpecCores
{
    public class SpecBlockGUIIniter2
    {
        public bool m_inited = false;
        private Action m_init;

        public SpecBlockGUIIniter2(Action Init)
        {
            m_init = Init;
        }

        public void CreateGui(IMyTerminalBlock entity)
        {
            lock (this)
            {
                if (m_inited) return;
                m_inited = true;
                m_init.Invoke();
            }
        }
    }

    public class SpecBlockGUIIniter : GUIIniter
    {
        protected override void InitControls<T>()
        {
            SpecBlock.InitControls<T>();
        }
    }
    
    public class LimitedBlockGUIIniter : GUIIniter
    {
        protected override void InitControls<Z>()
        {
            LimitedBlock.InitControls<Z>();
        }
    }

    public class GuiIniter
    {
        public GUIIniter Initer;
        public SpecBlockGUIIniter2 Initer2;

        public GuiIniter(GUIIniter initer, SpecBlockGUIIniter2 initer2)
        {
            Initer = initer;
            Initer2 = initer2;
        }

        public void CreateGui(IMyTerminalBlock entity)
        {
            Initer.CreateGui(entity);
            //Initer2.CreateGui(entity);
        }
    }
    
    public class GUI
    {
        public static GuiIniter SpecBlockGui = new GuiIniter( new SpecBlockGUIIniter(), new SpecBlockGUIIniter2(SpecBlock.InitControls<IMyTerminalBlock>));
        public static GuiIniter LimitedBlockGui = new GuiIniter( new LimitedBlockGUIIniter(), new SpecBlockGUIIniter2(LimitedBlock.InitControls<IMyTerminalBlock>));

        public static void InitSpecBlockGui(GUIClasses classes)
        {
            var gui = SpecBlockGui.Initer;
            InitGui(classes, gui);
            GuiControlDuplicateRemover.Init("LimitedBlock", "SpecBlock");
        }


        public static void InitLimitedBlockGui(GUIClasses classes)
        {
            var gui = LimitedBlockGui.Initer;
            InitGui(classes, gui);
        }

        private static void InitGui(GUIClasses classes, GUIIniter gui)
        {
            if (classes.HasFlag(GUIClasses.Basic))
            {
                gui.AddType<IMyGasTank>();
                gui.AddType<IMyGyro>();
                gui.AddType<IMyUpgradeModule>();
                gui.AddType<IMyCargoContainer>();
                gui.AddType<IMyThrust>();
                gui.AddType<IMyAirVent>();
                gui.AddType<IMyConveyorSorter>();
                gui.AddType<IMyCollector>();
            }

            if (classes.HasFlag(GUIClasses.EnergyAndProduction))
            {
                gui.AddType<IMyBatteryBlock>();
                gui.AddType<IMySolarPanel>();
                gui.AddType<IMyReactor>();
                gui.AddType<IMyAssembler>();
                gui.AddType<IMyGasGenerator>();
                gui.AddType<IMyOxygenFarm>();
                gui.AddType<IMyRefinery>();
                gui.AddType<IMyProductionBlock>();
            }

            if (classes.HasFlag(GUIClasses.RotorsAndPistons))
            {
                gui.AddType<IMyExtendedPistonBase>();
                gui.AddType<IMyPistonBase>();
                gui.AddType<IMyMotorAdvancedStator>();
                gui.AddType<IMyMotorSuspension>();
                gui.AddType<IMyMotorBase>();
            }

            if (classes.HasFlag(GUIClasses.ShipControl))
            {
                gui.AddType<IMyProgrammableBlock>();
                gui.AddType<IMyRemoteControl>();
                gui.AddType<IMyCameraBlock>();
                gui.AddType<IMyLandingGear>();
                gui.AddType<IMyParachute>();
                gui.AddType<IMyShipMergeBlock>();
                gui.AddType<IMyCryoChamber>();
                gui.AddType<IMyCockpit>();
                gui.AddType<IMyShipController>();
                gui.AddType<IMyRadioAntenna>();
                gui.AddType<IMyBeacon>();
                gui.AddType<IMyLaserAntenna>();
                gui.AddType<IMyJumpDrive>();
            }

            if (classes.HasFlag(GUIClasses.Tools))
            {
                gui.AddType<IMyShipDrill>();
                gui.AddType<IMyShipGrinder>();
                gui.AddType<IMyShipWelder>();
            }

            if (classes.HasFlag(GUIClasses.Weapons))
            {
                gui.AddType<IMyLargeGatlingTurret>();
                gui.AddType<IMyLargeInteriorTurret>();
                gui.AddType<IMySmallGatlingGun>();
                gui.AddType<IMySmallMissileLauncherReload>();
                gui.AddType<IMySmallMissileLauncher>();
                gui.AddType<IMyDecoy>();
                gui.AddType<IMyWarhead>();
                gui.AddType<IMyLargeTurretBase>();
                gui.AddType<IMyTurretControlBlock>();
                gui.AddType<IMyUserControllableGun>();
            }

            if (classes.HasFlag(GUIClasses.Other))
            {
                gui.AddType<IMySafeZoneBlock>();
                gui.AddType<IMyMedicalRoom>();
                gui.AddType<IMyHeatVent>();
            }

            if (classes.HasFlag(GUIClasses.Strange))
            {
                gui.AddType<IMyGravityGenerator>();
                gui.AddType<IMyGravityGeneratorSphere>();
                gui.AddType<IMySpaceBall>();
                gui.AddType<IMyReflectorLight>();
                gui.AddType<IMyInteriorLight>();
                gui.AddType<IMySoundBlock>();
                gui.AddType<IMyTimerBlock>();
                gui.AddType<IMyButtonPanel>();
                gui.AddType<IMyControlPanel>();
                gui.AddType<IMyShipConnector>();
                gui.AddType<IMyAdvancedDoor>();
                gui.AddType<IMyAirtightHangarDoor>();
                gui.AddType<IMyAirtightSlideDoor>();
                gui.AddType<IMyStoreBlock>();
                gui.AddType<IMyTargetDummyBlock>();
                gui.AddType<IMyTextPanel>();
                gui.AddType<IMyLightingBlock>();
                gui.AddType<IMyOreDetector>();
                gui.AddType<IMySensorBlock>();
                gui.AddType<IMyDoor>();
            }
        }

    }
}