using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace Invalid.MetalFoam
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Decoy), false, "LargeDecoy_MetalFoam")]
    public class MetalFoamGenerator : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCubeBlock block;  // This is the declaration you need
        private const int sphereRadius = 4;  // This makes sphereRadius available to the whole class


        private int nextLayerTick = 0;
        private int currentLayer = 0;
        private Vector3I center;
        private int radius;

        MySync<bool, SyncDirection.BothWays> foam_clientSync = null;
        static bool m_controlsCreated = false;


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            // The sync variables are already set by the time we get here.
            // Hook the ValueChanged event, so we can do something when the data changes.
            foam_clientSync.ValueChanged += foaming_ValueChanged;

            // This is a test of SyncExtentions whitelist, however this will execute if you call m_clientSync.ValidateAndSet().
            foam_clientSync.AlwaysReject();

            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;


            block = (IMyCubeBlock)Entity;
            block.SlimBlock.ComponentStack.IsFunctionalChanged += OnBlockDamaged;
        }


        private void foaming_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // This is where you handle the switch's value change.
            // If it's on, start generating foam; if it's off, stop generating foam.
            if (obj.Value)
            {
                // Assuming obj.Value = true means the switch is turned ON

                // Initiate foam generation by setting up the next tick and layer
                center = block.Position;  // Set the center for sphere generation
                radius = sphereRadius;  // Set the radius
                currentLayer = 0;  // Start from the first layer
                nextLayerTick = MyAPIGateway.Session.GameplayFrameCounter;  // Start immediately
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;  // Begin updates

                MyAPIGateway.Utilities.ShowNotification("Foam generation started!", 2000, MyFontEnum.Green);

                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("MetalFoamSmoke", block.GetPosition());
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("MetalFoamSound", block.GetPosition());
            }
            else
            {
                // Stop foam generation
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_100TH_FRAME;  // Stop updating
                MyAPIGateway.Utilities.ShowNotification("Foam generation stopped!", 2000, MyFontEnum.Red);
            }

        }

        static void CreateTerminalControls()
        {
            if (!m_controlsCreated)
            {
                m_controlsCreated = true;

                var clientSyncTestOnOff = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyDecoy>("Gwindalmir.Sync.TestClient");
                clientSyncTestOnOff.Enabled = (b) => true;
                clientSyncTestOnOff.Visible = (b) => true;
                clientSyncTestOnOff.Title = MyStringId.GetOrCompute("RELEASE THE FOAM");
                clientSyncTestOnOff.Getter = (b) => b.GameLogic.GetAs<MetalFoamGenerator>().foam_clientSync;
                clientSyncTestOnOff.Setter = (b, v) => b.GameLogic.GetAs<MetalFoamGenerator>().foam_clientSync.Value = v;
                clientSyncTestOnOff.OnText = MyStringId.GetOrCompute("On");
                clientSyncTestOnOff.OffText = MyStringId.GetOrCompute("Off");
                MyAPIGateway.TerminalControls.AddControl<IMyDecoy>(clientSyncTestOnOff);

                // Add an action for cockpits/control stations
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyDecoy>("ToggleFoamGeneration");
                action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; // Path to your icon or a default icon
                action.Name = new StringBuilder("Toggle Foam Generation");
                action.Writer = (block, stringBuilder) => stringBuilder.Append("Foam: ").Append(block.GameLogic.GetAs<MetalFoamGenerator>().foam_clientSync.Value ? "On" : "Off");
                action.Action = (block) =>
                {
                    var metalFoamGenerator = block.GameLogic.GetAs<MetalFoamGenerator>();
                    metalFoamGenerator.foam_clientSync.Value = !metalFoamGenerator.foam_clientSync.Value; // Toggle the foam generation
                };
                action.Enabled = (block) => true; // You can put conditions here if some blocks shouldn't have this action
                action.ValidForGroups = false; // Set true if you want this action to be available when selecting a group of blocks

                MyAPIGateway.TerminalControls.AddAction<IMyDecoy>(action);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            CreateTerminalControls();

            (Entity as IMyDecoy).EnabledChanged += MetalFoam_EnabledChanged;
        }

        private void MetalFoam_EnabledChanged(IMyCubeBlock obj)
        {


        }

        public override void UpdateBeforeSimulation100()
        {
            // This method is called approximately every 100 ticks (~1.66 seconds)
            if (MyAPIGateway.Session.GameplayFrameCounter >= nextLayerTick)
            {
                // Ensure currentLayer starts from 0 (center) and expands outwards
                if (currentLayer <= sphereRadius)
                {
                    AddLayer(center, sphereRadius, currentLayer); // Start from center and expand
                    currentLayer++; // Move to the next layer
                    nextLayerTick = MyAPIGateway.Session.GameplayFrameCounter + (180 / (sphereRadius + 1)); // Adjust timing as needed
                }
                else
                {
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_100TH_FRAME; // Stop updating after complete
                    RemoveBlock(); // Remove the block after foam generation is complete
                }
            }
        }

        // Handle damage event
        private void OnBlockDamaged()
        {
            if (!block.SlimBlock.IsDestroyed) // Replace with appropriate check for your scenario
            {

                // Play particle and sound effects at the block's position
                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("MetalFoamSmoke", block.GetPosition());
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("MetalFoamSound", block.GetPosition());

                // Proceed with foam generation
                center = block.Position; // Set the center for sphere generation
                radius = sphereRadius; // Set the radius
                currentLayer = 0; // Start from the first layer
                nextLayerTick = MyAPIGateway.Session.GameplayFrameCounter; // Start immediately
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME; // Begin updates

            }
        }



        // This method will handle adding a single layer at a time
        private void AddLayer(Vector3I center, int radius, int layerIndex)
        {
            var grid = block.CubeGrid;
            Vector3I pos;

            // Calculate the bounds for this layer
            int layerRadius = layerIndex;  // Directly use layerIndex as radius

            for (int x = -layerRadius; x <= layerRadius; x++)
            {
                for (int y = -layerRadius; y <= layerRadius; y++)
                {
                    for (int z = -layerRadius; z <= layerRadius; z++)
                    {
                        pos = new Vector3I(x, y, z) + center;
                        double distance = Vector3D.Distance(new Vector3D(pos), new Vector3D(center));
                        // Adjust the distance check to only include the outer shell of the sphere for each layer
                        if (distance <= layerRadius && distance > layerRadius - 1)
                        {
                            AddArmorBlock("LargeBlockArmorBlock", pos);
                        }
                    }
                }
            }
        }


        private void AddArmorBlock(string subtypeName, Vector3I position)
        {
            var grid = block.CubeGrid; // Get the grid the block is part of

            // Define the block to be added
            var armorBlockBuilder = new MyObjectBuilder_CubeBlock
            {
                SubtypeName = subtypeName, // "LargeBlockArmorBlock" for large grid light armor block
                Min = position, // Position where the block will be placed
                ColorMaskHSV = new SerializableVector3(0, -1, 0), // Default color, change as needed
            };

            // Set the block orientation (facing up by default here)
            armorBlockBuilder.BlockOrientation = new MyBlockOrientation(
                Base6Directions.Direction.Forward,
                Base6Directions.Direction.Up);

            // Create the block on the grid
            grid.AddBlock(armorBlockBuilder, false);

            // Optionally, check for success and perform actions or notifications
        }

        private void RemoveBlock()
        {
            var grid = block.CubeGrid; // Get the grid the block is part of
            var slimBlock = block.SlimBlock; // Get the slim version of the block

            // Remove the block from the grid
            grid.RemoveBlock(slimBlock, true);
        }


        public override void Close()
        {
            base.Close();
            if (block != null)
            {
                // Unregister the damage handler
                block.SlimBlock.ComponentStack.IsFunctionalChanged -= OnBlockDamaged;
            }
        }

    }
}
