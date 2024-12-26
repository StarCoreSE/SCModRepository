using System.Collections.Generic;
using System.Linq;
using Epstein_Fusion_DS.Communication;
using Epstein_Fusion_DS.HeatParts.Definitions;
using VRage.Game.ModAPI;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts
{
    internal class HeatSystem
    {
        /// <summary>
        /// Maps radiator blocks to their heat dissipation.
        /// </summary>
        private readonly Dictionary<IMyCubeBlock, float> _radiatorBlocks = new Dictionary<IMyCubeBlock, float>();

        public int AssemblyId;

        public GridHeatManager Parent;


        public HeatSystem(int assemblyId, GridHeatManager parent)
        {
            AssemblyId = assemblyId;
            Parent = parent;

            HeatDissipation = 0;
            HeatCapacity = 0;

            Parent.Grid.OnBlockAdded += UpdateLoS;
            Parent.Grid.OnBlockRemoved += UpdateLoS;
        }

        private static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;
        public int BlockCount { get; private set; }

        /// <summary>
        ///     Total heat dispersed per second by this assembly.
        /// </summary>
        public float HeatDissipation
        {
            get { return ModularApi.GetAssemblyProperty<float>(AssemblyId, "HeatDissipation"); }
            private set { ModularApi.SetAssemblyProperty(AssemblyId, "HeatDissipation", value); }
        }

        /// <summary>
        ///     Total heat able to be stored by this assembly.
        /// </summary>
        public float HeatCapacity
        {
            get { return ModularApi.GetAssemblyProperty<float>(AssemblyId, "HeatCapacity"); }
            private set { ModularApi.SetAssemblyProperty(AssemblyId, "HeatCapacity", value); }
        }

        public void OnPartAdd(IMyCubeBlock block)
        {
            var definition = HeatPartDefinitions.GetDefinition(block.BlockDefinition.SubtypeId);
            if (definition == null)
                return;

            if (definition.HeatCapacity != 0)
            {
                HeatCapacity += definition.HeatCapacity;
                Parent.HeatCapacity += definition.HeatCapacity;
            }

            if (definition.HeatDissipation != 0)
            {
                _radiatorBlocks.Add(block, 0);
                DoLoSCheck(block);
            }

            BlockCount++;
        }

        public void OnPartRemove(IMyCubeBlock block)
        {
            var definition = HeatPartDefinitions.GetDefinition(block.BlockDefinition.SubtypeId);
            if (definition == null)
                return;

            if (definition.HeatCapacity != 0)
            {
                HeatCapacity -= definition.HeatCapacity;
                Parent.HeatCapacity -= definition.HeatCapacity;
            }

            if (definition.HeatDissipation != 0)
            {
                HeatDissipation -= _radiatorBlocks[block];
                Parent.HeatDissipation -= _radiatorBlocks[block];
                _radiatorBlocks.Remove(block);
            }

            BlockCount--;
        }

        public void OnClose()
        {
            Parent.Grid.OnBlockAdded -= UpdateLoS;
            Parent.Grid.OnBlockRemoved -= UpdateLoS;
        }

        private void UpdateLoS(IMySlimBlock newBlock)
        {
            Quaternion radQuaternion;

            foreach (var radiator in _radiatorBlocks.Keys.ToArray())
            {
                radiator.Orientation.GetQuaternion(out radQuaternion);

                var offset = (Vector3I)(radQuaternion * (newBlock.Position - radiator.Position));
                var radiatorSize = (radiator.Max - radiator.Min + Vector3I.One).AbsMax();

                // If block is in front of radiator panel
                if (offset.X < radiatorSize && offset.Y < radiatorSize && offset.Z < radiatorSize) DoLoSCheck(radiator);
            }
        }

        private void DoLoSCheck(IMyCubeBlock radiatorBlock)
        {
            var definition = HeatPartDefinitions.GetDefinition(radiatorBlock.BlockDefinition.SubtypeId);
            if (definition == null)
                return;

            float currentDissipation = (definition.LoSCheck?.Invoke(radiatorBlock) ?? 1) * definition.HeatDissipation;
            float prevDissipation = _radiatorBlocks[radiatorBlock];

            if (currentDissipation == prevDissipation)
                return;

            HeatDissipation -= prevDissipation;
            Parent.HeatDissipation -= prevDissipation;

            HeatDissipation += currentDissipation;
            Parent.HeatDissipation += currentDissipation;

            _radiatorBlocks[radiatorBlock] = currentDissipation;
        }
    }
}