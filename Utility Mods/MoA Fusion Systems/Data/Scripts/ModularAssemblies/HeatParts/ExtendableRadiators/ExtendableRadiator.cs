using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Epstein_Fusion_DS.Communication;
using Epstein_Fusion_DS.HudHelpers;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts.ExtendableRadiators
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "ExtendableRadiatorBase")]
    internal class ExtendableRadiator : MyGameLogicComponent
    {
        public static readonly Guid RadiatorGuid = new Guid("e6b87818-5fd8-47a6-a480-3365e20214e1");
        public static readonly string[] ValidPanelSubtypes =
        {
            "RadiatorPanel",
        };


        public IMyCubeBlock Block;
        internal StoredRadiator[] StoredRadiators = Array.Empty<StoredRadiator>();
        internal RadiatorAnimation Animation;

        private bool _isExtended = true;
        public bool IsExtended
        {
            get
            {
                return _isExtended;
            }
            set
            {
                if (Animation.IsActive)
                    return;

                if (value)
                    ExtendPanels();
                else
                    RetractPanels();
                _isExtended = value;
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            RadiatorControls.DoOnce();
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null)
                return;

            LoadSettings();

            try
            {
                SaveSettings();
            }
            catch (Exception ex)
            {
                ModularDefinition.ModularApi.Log(ex.ToString());
            }

            Animation = new RadiatorAnimation(this);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            //NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // This is stupid, but prevents the mod profiler cost from being incurred every tick per block when inactive
            if (Animation.IsActive)
                Animation.UpdateTick();
            else
                MakePanelsVisible();
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
            }
            catch (Exception ex)
            {
                ModularDefinition.ModularApi.Log(ex.ToString());
            }

            return base.IsSerialized();
        }

        internal void SaveSettings()
        {
            if (Block == null)
                return; // called too soon or after it was already closed, ignore

            if (StoredRadiators == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            Block.Storage.SetValue(RadiatorGuid, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(StoredRadiators)));
        }

        internal void LoadSettings()
        {
            if (Block.Storage == null)
                return;
            
            string rawData;
            if (!Block.Storage.TryGetValue(RadiatorGuid, out rawData))
                return;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<StoredRadiator[]>(Convert.FromBase64String(rawData)) ?? Array.Empty<StoredRadiator>();

                StoredRadiators = loadedSettings;
                _isExtended = StoredRadiators.Length == 0;

                for (int i = 0; i < StoredRadiators.Length; i++)
                {
                    if (Block.LocalMatrix == StoredRadiators[i].BaseLocalMatrix)
                        break;

                    var matrix = Matrix.Invert(StoredRadiators[i].BaseLocalMatrix) * Block.LocalMatrix;

                    StoredRadiators[i].ObjectBuilder.Min = Block.Position + (Vector3I)Block.LocalMatrix.Up * (i+1);
                    StoredRadiators[i].LocalMatrix *= matrix;
                    StoredRadiators[i].ObjectBuilder.Orientation = Quaternion.CreateFromRotationMatrix(StoredRadiators[i].LocalMatrix);

                    var matrix2 = StoredRadiators[i].LocalMatrix;
                    StoredRadiators[i].ObjectBuilder.BlockOrientation = new MyBlockOrientation(ref matrix2);

                    StoredRadiators[i].BaseLocalMatrix = Block.LocalMatrix;
                }
            }
            catch (Exception e)
            {
                ModularDefinition.ModularApi.Log(e.ToString());
            }
        }

        public void ExtendPanels()
        {
            if (_isExtended || Animation.IsActive)
                return;

            Vector3I nextPosition = Block.Position;

            try
            {
                // TODO move this to clientside
                for (int i = 0; i < StoredRadiators.Length; i++)
                {
                    nextPosition += (Vector3I)(Block.LocalMatrix.Up * (i + 1));

                    if (Block.CubeGrid.CubeExists(nextPosition))
                    {
                        MyAPIGateway.Utilities.ShowNotification("Block already exists at position!");
                        DebugDraw.AddGridPoint(nextPosition, Block.CubeGrid, Color.Red, 4);
                        _isExtended = false;
                        return;
                    }
                }

                for (int i = 0; i < StoredRadiators.Length; i++)
                {
                    StoredRadiators[i].ObjectBuilder.Name = null;

                    var newBlock = Block.CubeGrid.AddBlock(StoredRadiators[i].ObjectBuilder, true);
                    if (newBlock?.FatBlock != null)
                        newBlock.FatBlock.Visible = false;
                    else
                        ModularDefinition.ModularApi.Log($"Stored radiator panel is null!\n    Builder: {StoredRadiators[i].ObjectBuilder == null}\n    Slimblock: {newBlock == null}\n    Fatblock: {newBlock?.FatBlock == null}");
                }

                Animation.StartExtension();
            }
            catch (Exception ex)
            {
                ModularDefinition.ModularApi.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Panels start invisible for the animation to play. This makes them visible again.
        /// </summary>
        public void MakePanelsVisible()
        {
            IMyCubeBlock nextBlock;
            int idx = 1;

            while (GetNextPanel(idx, out nextBlock))
            {
                if (nextBlock.Visible)
                    break;
                nextBlock.Visible = true;
                idx++;
            }

            StoredRadiators = Array.Empty<StoredRadiator>();
        }

        public void RetractPanels()
        {
            if (!_isExtended)
                return;

            IMyCubeBlock nextBlock;
            List<StoredRadiator> builders = new List<StoredRadiator>();
            int idx = 1;

            while (GetNextPanel(idx, out nextBlock))
            {
                var builder = nextBlock.GetObjectBuilderCubeBlock(true);

                builder.BlockOrientation = nextBlock.Orientation;

                Matrix matrix;
                builders.Add(new StoredRadiator(builder, nextBlock.LocalMatrix, nextBlock.CalculateCurrentModel(out matrix), Block.LocalMatrix));

                nextBlock.CubeGrid.RemoveBlock(nextBlock.SlimBlock, true);
                idx++;
            }

            StoredRadiators = builders.ToArray();

            Animation.StartRetraction();
        }

        internal bool GetNextPanel(int idx, out IMyCubeBlock next)
        {
            IMySlimBlock block = Block.CubeGrid.GetCubeBlock((Vector3I)(Block.Position + Block.LocalMatrix.Up * idx));
            if (block == null || !ValidPanelSubtypes.Contains(block.BlockDefinition.Id.SubtypeName))
            {
                next = null;
                return false;
            }

            next = block.FatBlock;
            return true;
        }

        [ProtoContract]
        internal struct StoredRadiator
        {
            [ProtoMember(1)] public MyObjectBuilder_CubeBlock ObjectBuilder;
            [ProtoMember(2)] public Matrix LocalMatrix;
            [ProtoMember(4)] public Matrix BaseLocalMatrix;
            [ProtoMember(3)] public string Model;


            public StoredRadiator(MyObjectBuilder_CubeBlock objectBuilder, Matrix localMatrix, string model, Matrix baseLocalMatrix)
            {
                ObjectBuilder = objectBuilder;
                LocalMatrix = localMatrix;
                Model = model;
                BaseLocalMatrix = baseLocalMatrix;
            }
        }
    }
}
