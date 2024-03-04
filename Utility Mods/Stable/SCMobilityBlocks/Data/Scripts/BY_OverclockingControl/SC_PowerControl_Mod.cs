namespace StarCore.PowerControl
{
    using StarCore.PowerControl.ModSystem;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using VRage;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using static BuYanMod.Utils.Utils;
    using System.Runtime.InteropServices;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "SC_PowerControlSystem_L", "SC_PowerControlSystem_S")]
    public class OverclockingControlBlock : MyGameLogicComponent
    {
        [ProtoContract]
        public class OverclockingControl
        {
            //[ProtoMember(1)] public float Reactor = 1;
            //[ProtoMember(2)] public float GasGenerator = 1;
            //[ProtoMember(3)] public float Gyro = 1;
            [ProtoMember(4)] public float Thrust = 1;
            //[ProtoMember(5)] public float Drill = 1;
        }

        private readonly Guid GUID = new Guid("f297034ec68e4af0948f3a70f108339c");
        private const string sID = "SC_PowerControlSystem";
        private static List<IMySlimBlock> GridBlocks = new List<IMySlimBlock>();
        protected IMyCollector Block => Entity as IMyCollector;
        private static bool controlIsCreated = false;
        private static bool ThreadLock = false;

        //public float Reactor { get; protected set; } = 1;
        //public float GasGenerator { get; protected set; } = 1;
        //public float Gyro { get; protected set; } = 1;
        public float Thrust { get; protected set; } = 1;
        //public float Drill { get; protected set; } = 1;

        public sealed override bool IsSerialized()
        {
            SaveConfig();
            return base.IsSerialized();
        }

        protected void SaveConfig()
        {
            LoadSaveDatas.Init(Block, GUID);
            LoadSaveDatas.Save(Block, LoadSaveDatas.ByteToString(MyAPIGateway.Utilities.SerializeToBinary(ReadValue())), GUID);
        }

        protected void LoadConfig()
        {
            LoadSaveDatas.Init(Block, GUID);
            var bytes = LoadSaveDatas.StringToByte(LoadSaveDatas.Load(Block, GUID));
            if (bytes == null || bytes.Length < 1) return;
            WriteValue(MyAPIGateway.Utilities.SerializeFromBinary<OverclockingControl>(bytes));
        }

        public OverclockingControl ReadValue()
        {
            return new OverclockingControl()
            {
                //Reactor = Reactor,
                //GasGenerator = GasGenerator,
                //Gyro = Gyro,
                Thrust = Thrust,
                //Drill = Drill,
            };
        }

        public void WriteValue(OverclockingControl value)
        {
            if (value == null) return;
            //Reactor = value.Reactor;
            //GasGenerator = value.GasGenerator;
            //Gyro = value.Gyro;
            Thrust = value.Thrust;
            //Drill = value.Drill;
        }

        public sealed override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public void AdjustTerminalControls_controls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.BlockDefinition.SubtypeName == Block.BlockDefinition.SubtypeName)
            {
                for (int i = controls.Count - 1; i >= 0; i--)
                {
                    if (controls[i].Id == "Recompile" || controls[i].Id == "TerminalRun" || controls[i].Id == "ConsoleCommand" || controls[i].Id == "Edit")
                    {
                        controls.RemoveAt(i);
                    }
                }
            }
        }

        public void AdjustTerminalControls_Action(IMyTerminalBlock block, List<IMyTerminalAction> controls)
        {
            if (block.BlockDefinition.SubtypeName == Block.BlockDefinition.SubtypeName)
            {
                for (int i = controls.Count - 1; i >= 0; i--)
                {
                    if (controls[i].Id == "RunWithDefaultArgument" || controls[i].Id == "Run")
                    {
                        controls.RemoveAt(i);
                    }
                }
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (Block.BlockDefinition.SubtypeId == "SC_PowerControlSystem")
            {
                //Reactor = 1;
            }
            BuildControl();
            LoadConfig();
            IsEnable();
            Block.AppendingCustomInfo += Block_AppendingCustomInfo;
            MyAPIGateway.TerminalControls.CustomControlGetter += AdjustTerminalControls_controls;
            MyAPIGateway.TerminalControls.CustomActionGetter += AdjustTerminalControls_Action;
            Block.CubeGrid.OnBlockAdded += CubeGrid_OnBlockAdded;
            Block.CubeGrid.OnGridMerge += CubeGrid_OnGridMerge;
            Block.CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public sealed override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (!ThreadLock)
            {
                MyAPIGateway.Parallel.StartBackground(IsEnable);
                ThreadLock = true;
            }
            Block.RefreshCustomInfo();
        }

        private void IsEnable()
        {
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            List<IMyCollector> Pbs = new List<IMyCollector>();
            Block.CubeGrid.GetBlocks(blocks, (b) => {
                if (b.BlockDefinition.Id.SubtypeName.Contains(sID)) return true;
                return false;
            });

            if (blocks.Count > 1)
            {
                int on = 0;
                int a = 0;
                foreach (IMySlimBlock block in blocks)
                {
                    IMyCollector b = (IMyCollector)block.FatBlock;
                    if (b.Enabled)
                    {
                        UpdateData();
                        on++;
                    }
                    a++;
                    if (a == blocks.Count && on == 0) Overclock.Clear(b.CubeGrid);
                }
            }
            else
            {
                if (Block.Enabled) UpdateData();
                if (!Block.Enabled) Overclock.Clear(Block.CubeGrid);
            }

            ThreadLock = false;
        }

        private void CubeGrid_OnBlockAdded(IMySlimBlock obj)
        {
            if (Block.Enabled)
            {
                //if (obj.FatBlock is IMyReactor) Overclock.Reactor((IMyTerminalBlock)obj.FatBlock, Reactor, false);
                //if (obj.FatBlock is IMyGasGenerator) Overclock.GasGenerator((IMyTerminalBlock)obj.FatBlock, GasGenerator, false);
                //if (obj.FatBlock is IMyGyro) Overclock.Gyro((IMyTerminalBlock)obj.FatBlock, Gyro, false);
                if (obj.FatBlock is IMyThrust) Overclock.Thrust((IMyTerminalBlock)obj.FatBlock, Thrust, false);
                //if (obj.FatBlock is IMyShipDrill) Overclock.Drill((IMyTerminalBlock)obj.FatBlock, Drill, false);
            }

            if (obj.BlockDefinition.Id.SubtypeName.Contains(sID))
            {
                Synchronize((IMyTerminalBlock)obj.FatBlock, false);
            }
        }

        private void CubeGrid_OnGridMerge(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            IsEnable();
        }

        private void CubeGrid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            GridBlocks.Clear();
            arg1.GetBlocks(GridBlocks, (b) => {
                if (b.BlockDefinition.Id.SubtypeName.Contains(sID)) return true;
                return false;
            });

            if (GridBlocks.Count == 0) Overclock.Clear(arg1);

            GridBlocks.Clear();
            arg2.GetBlocks(GridBlocks, (b) => {
                if (b.BlockDefinition.Id.SubtypeName.Contains(sID)) return true;
                return false;
            });

            if (GridBlocks.Count == 0) Overclock.Clear(arg2);
        }

        static void BuildControl()
        {
            if (controlIsCreated) return;

            TerminalRevise.CreateSeparator<IMyCollector>("A-", "Test", BlockConfirm, BlockConfirm);
            TerminalRevise.CreateLabel<IMyCollector>("BY-", "Label-Overclock Device", "Downclock Settings", BlockConfirm, BlockConfirm);


            TerminalRevise.CreateSlider<IMyCollector>("BY-", "Slider-Thrust", "Thrust Downclock", BlockConfirm, BlockConfirm, (Me) => {
                var logic = GetTerminal(Me);
                if (logic.Item1) return logic.Item2.Thrust;
                return 1;
            }, (Me, value) => {
                var logic = GetTerminal(Me);
                if (!logic.Item1) return;
                logic.Item2.Thrust = value;
                if (logic.Item2.Block.Enabled) Overclock.Thrust(Me, value, true);
                logic.Item2.SaveConfig();
                Synchronize(Me, true);
            }, (Me, value) => {
                var logic = GetTerminal(Me);
                value.Append(logic.Item2.Thrust + " " + "times");
            }, 0.2f, 1, 0.2f, TerminalRevise.SliderStyle.Log);


            controlIsCreated = true;


        }

        private void UpdateData()
        {
            //Overclock.Reactor(Block, Reactor, true);
            //Overclock.GasGenerator(Block, GasGenerator, true);
            //Overclock.Gyro(Block, Gyro, true);
            Overclock.Thrust(Block, Thrust, true);
            //Overclock.Drill(Block, Drill, true);
        }

        public static void Synchronize(IMyTerminalBlock block, bool IsSet)
        {
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            block.CubeGrid.GetBlocks(Blocks, (b) => {
                if (b.BlockDefinition.Id.SubtypeName.Contains("SC_PowerControlSystem")) return true;
                return false;
            });

            if (IsSet)
            {
                if (Blocks.Count > 1)
                {
                    var Logic1 = GetTerminal(block);
                    foreach (IMySlimBlock b in Blocks)
                    {
                        var Logic2 = GetTerminal((IMyTerminalBlock)b.FatBlock);
                        if (Logic2.Item1)
                        {
                            //Logic2.Item2.Reactor = Logic1.Item2.Reactor;
                            //Logic2.Item2.GasGenerator = Logic1.Item2.GasGenerator;
                            //Logic2.Item2.Gyro = Logic1.Item2.Gyro;
                            Logic2.Item2.Thrust = Logic1.Item2.Thrust;
                            //Logic2.Item2.Drill = Logic1.Item2.Drill;
                            Logic2.Item2.SaveConfig();
                        }
                    }
                }
            }
            else
            {
                if (Blocks.Count > 1)
                {
                    float Re = 1, GG = 1, Gy = 1, Th = 1, Dr = 1;
                    foreach (IMySlimBlock b in Blocks)
                    {
                        var Logic = GetTerminal((IMyTerminalBlock)b.FatBlock);
                        if (Logic.Item1)
                        {
                            //if (Logic.Item2.Reactor > Re) Re = Logic.Item2.Reactor;
                           //if (Logic.Item2.GasGenerator > GG) GG = Logic.Item2.GasGenerator;
                           //if (Logic.Item2.Gyro > Gy) Gy = Logic.Item2.Gyro;
                           if (Logic.Item2.Thrust > Th) Th = Logic.Item2.Thrust;
                           //if (Logic.Item2.Drill > Dr) Dr = Logic.Item2.Drill;
                        }
                    }
                    foreach (IMySlimBlock b in Blocks)
                    {
                        var Logic = GetTerminal((IMyTerminalBlock)b.FatBlock);
                        if (Logic.Item1)
                        {
                            //Logic.Item2.Reactor = Re;
                            //Logic.Item2.GasGenerator = GG;
                            //Logic.Item2.Gyro = Gy;
                            Logic.Item2.Thrust = Th;
                            //Logic.Item2.Drill = Dr;
                            Logic.Item2.SaveConfig();
                        }
                    }
                }
            }
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder arg2)
        {
            arg2.Append("\n" + "Info");
            arg2.Append("\n");
           //arg2.Append("\n" + "Reactor" + " :   " + Math.Round(Reactor, 2) + "  " + "times");
           //arg2.Append("\n" + "Gas Generator" + " :   " + Math.Round(GasGenerator, 2) + "  " + "times");
           //arg2.Append("\n" + "Gyro" + " :   " + Math.Round(Gyro, 2) + "  " + "times");
           arg2.Append("\n" + "Thrust" + " :   " + Math.Round(Thrust, 2) + "  " + "times");
           //arg2.Append("\n" + "Drill" + " :   " + Math.Round(Drill, 2) + "  " + "times");
            TerminalRevise.RefreshBlockTerminal(block);
        }

        public static MyTuple<bool, OverclockingControlBlock> GetTerminal(IMyTerminalBlock Me)
        {
            if (NullCheck.IsNull(Me)) return new MyTuple<bool, OverclockingControlBlock>(false, null);
            if (!Me.BlockDefinition.SubtypeId.Contains(sID)) return new MyTuple<bool, OverclockingControlBlock>(false, null);
            var Logic = Me?.GameLogic?.GetAs<OverclockingControlBlock>();
            if (Logic == null) return new MyTuple<bool, OverclockingControlBlock>(false, null);
            return new MyTuple<bool, OverclockingControlBlock>(true, Logic);
        }

        public static bool BlockConfirm(IMyTerminalBlock Me)
        {
            if (NullCheck.IsNull(Me)) return false;
            if (!Me.BlockDefinition.SubtypeId.Contains(sID)) return false;
            return Me?.GameLogic?.GetAs<OverclockingControlBlock>() != null;
        }

        public sealed override void Close()
        {
            base.Close();
            Overclock.Clear(Block.CubeGrid);
            Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
            MyAPIGateway.TerminalControls.CustomControlGetter -= AdjustTerminalControls_controls;
            MyAPIGateway.TerminalControls.CustomActionGetter -= AdjustTerminalControls_Action;
            Block.CubeGrid.OnBlockAdded -= CubeGrid_OnBlockAdded;
            Block.CubeGrid.OnGridMerge -= CubeGrid_OnGridMerge;
            Block.CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
        }
    }
}
