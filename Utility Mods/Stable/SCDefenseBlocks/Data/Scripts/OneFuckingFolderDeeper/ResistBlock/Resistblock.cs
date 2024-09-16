using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using VRage.Game.ObjectBuilders.ComponentSystem;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using System.Text;
using Sandbox.Game;
using VRage.Game;
using VRageRender;

namespace Starcore.ResistBlock
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "FieldGen_Core")]
    public class ResistBlock : MyGameLogicComponent
    {
        private IMyCollector block;
        private ResistSettings Settings;
        public MySync<bool, SyncDirection.BothWays> IsActiveSync;
        public MySync<float, SyncDirection.BothWays> ResistanceStrengthSync;

        private Vector3D hitPosition; // Store the hit position
        private Vector3D attackerPosition; // Store the attacker's position
        private bool isFlashing;
        private int flashDuration = 120; // Flash for 120 frames (2 seconds at 60 FPS)
        private int flashCounter;
        private IMyCubeGrid coreBlockGrid; // Store the grid containing the core block

        private static bool m_controlsCreated = false;

        public readonly Guid ResistSettingsGUID = new Guid("160803f9-9800-4515-9619-e5385d5208fb");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCollector)Entity;

            // Store the grid that contains the core block
            coreBlockGrid = block?.CubeGrid;

            // Register the global damage handler, but the logic will only apply to the core block's grid
            if (MyAPIGateway.Session != null)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
            }

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        private IMySlimBlock hitSlimBlock; // Store the reference to the block that was hit

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            CreateTerminalControls(); // Ensure terminal controls are created only once and filtered

            if (block == null || block.CubeGrid?.Physics == null)
                return;

            Settings = new ResistSettings(this);

            IsActiveSync.ValueChanged += IsActive_ValueChanged;
            ResistanceStrengthSync.ValueChanged += ResistanceStrength_ValueChanged;

            LoadSettings();
            SaveSettings();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void ResistanceStrength_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            // Handle resistance strength change if needed
        }

        private void IsActive_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            if (IsActiveSync.Value)
                StartResistance();
            else
                StopResistance();
        }

        private void StartResistance()
        {
            // Apply grid-wide resistance based on the current slider value
            float resistanceStrength = Settings.ResistanceStrength;
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1f - resistanceStrength); // Adjusting resistance
        }

        private void StopResistance()
        {
            // Reset the grid's damage modifier to normal when resistance is off
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1.0f); // Reset to normal damage
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (isFlashing && hitSlimBlock != null)
            {
                if (flashCounter > 0)
                {
                    // Use the stored hit block for flashing
                    DrawFlashingSquare();
                    flashCounter--;
                }
                else
                {
                    isFlashing = false; // Stop flashing after the duration
                    hitSlimBlock = null; // Clear the reference to the hit block
                }
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (Settings.IsActive)
            {
                ApplyResistance();
            }
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            try
            {
                // Ensure the block and its grid are initialized
                if (block == null || block.CubeGrid == null)
                    return;

                // Ensure the target is a valid slim block
                var slimBlock = target as IMySlimBlock;
                if (slimBlock == null || slimBlock.CubeGrid == null)
                    return;

                // Only process damage if it is occurring on the core block's grid
                if (slimBlock.CubeGrid != coreBlockGrid)
                    return;

                // Check if settings and the resistance are active
                if (Settings != null && Settings.IsActive)
                {
                    // Ensure the hit position can be calculated
                    hitPosition = slimBlock?.CubeGrid?.GridIntegerToWorld(slimBlock.Position) ?? Vector3D.Zero;
                    if (hitPosition == Vector3D.Zero)
                        return;

                    // Start the flashing effect
                    hitSlimBlock = slimBlock;
                    isFlashing = true;
                    flashCounter = flashDuration;
                }
            }
            catch (Exception ex)
            {
                // Log the exception to understand why it failed
                MyLog.Default.WriteLineAndConsole($"Exception in DamageHandler: {ex}");
            }
        }

        private void DrawFlashingSquare()
        {
            try
            {
                if (block == null || block.CubeGrid == null || !Settings.IsActive)
                    return;

                // Only draw the billboard if this is the core block's grid
                if (block.CubeGrid != coreBlockGrid)
                    return;

                // Get resistance strength (1.0 = full resistance, 0.0 = no resistance)
                float resistanceStrength = Settings.ResistanceStrength;

                // Calculate transparency based on resistance (lower resistance = more transparent)
                float transparency = 1f - resistanceStrength;

                // Interpolate color between green (high resistance) and red (low resistance)
                Color color = Color.Lerp(Color.Red, Color.Green, resistanceStrength); // Green at 1, Red at 0

                // Adjust alpha channel based on transparency
                color = new Color(color.R, color.G, color.B, (byte)(255 * transparency));

                // Get the grid's bounding box
                BoundingBoxD gridBoundingBox = block?.CubeGrid?.PositionComp?.WorldAABB ?? default(BoundingBoxD);
                if (gridBoundingBox == default(BoundingBoxD))
                    return;

                // Get the center of mass of the grid
                Vector3D centerOfMass = block.CubeGrid.Physics?.CenterOfMassWorld ?? gridBoundingBox.Center;

                // Calculate the direction from the center of mass to the hit position
                Vector3D hitDirection = Vector3D.Normalize(hitPosition - centerOfMass);

                // Transform hitDirection to the grid's local space
                MatrixD gridOrientation = block.CubeGrid.WorldMatrix;
                Vector3D localHitDirection = Vector3D.TransformNormal(hitDirection, MatrixD.Transpose(gridOrientation));

                // Determine the closest axis-aligned face based on the local hit direction
                Vector3D hitFaceNormal = Vector3D.Zero;
                if (Math.Abs(localHitDirection.X) > Math.Abs(localHitDirection.Y) && Math.Abs(localHitDirection.X) > Math.Abs(localHitDirection.Z))
                {
                    hitFaceNormal = localHitDirection.X > 0 ? Vector3D.Right : Vector3D.Left;
                }
                else if (Math.Abs(localHitDirection.Y) > Math.Abs(localHitDirection.X) && Math.Abs(localHitDirection.Y) > Math.Abs(localHitDirection.Z))
                {
                    hitFaceNormal = localHitDirection.Y > 0 ? Vector3D.Up : Vector3D.Down;
                }
                else
                {
                    hitFaceNormal = localHitDirection.Z > 0 ? Vector3D.Forward : Vector3D.Backward;
                }

                // Transform the hitFaceNormal back to world space for billboard orientation
                hitFaceNormal = Vector3D.TransformNormal(hitFaceNormal, gridOrientation);

                // Determine the size of the face (width and height) based on the normal
                double faceWidth, faceHeight;
                if (Vector3D.Abs(hitFaceNormal) == Vector3D.Forward || Vector3D.Abs(hitFaceNormal) == Vector3D.Backward)
                {
                    faceWidth = gridBoundingBox.Size.X;
                    faceHeight = gridBoundingBox.Size.Y;
                }
                else if (Vector3D.Abs(hitFaceNormal) == Vector3D.Left || Vector3D.Abs(hitFaceNormal) == Vector3D.Right)
                {
                    faceWidth = gridBoundingBox.Size.Y;
                    faceHeight = gridBoundingBox.Size.Z;
                }
                else
                {
                    faceWidth = gridBoundingBox.Size.X;
                    faceHeight = gridBoundingBox.Size.Z;
                }

                // Position the square on the appropriate face of the bounding box
                Vector3D faceCenter = gridBoundingBox.Center + hitFaceNormal * gridBoundingBox.HalfExtents.AbsMax();

                // Calculate the left and up vectors for the billboard in world space
                Vector3 leftVector = Vector3.Cross(Vector3.Up, hitFaceNormal); // Left vector perpendicular to up and face normal
                Vector3 upVector = Vector3.Cross(hitFaceNormal, leftVector);   // Ensure up vector is perpendicular to both

                // Set default UV offset and other missing parameters
                Vector2 uvOffset = Vector2.Zero;
                int customViewProjection = -1;
                float reflection = 0f;
                List<MyBillboard> billboards = null;

                // Draw the square billboard to take up the entire face of the bounding box
                MyTransparentGeometry.AddBillboardOriented(
                    material: MyStringId.GetOrCompute("Square"),
                    color: color.ToVector4(),
                    origin: faceCenter,
                    leftVector: leftVector,
                    upVector: upVector,
                    width: (float)faceWidth,
                    height: (float)faceHeight,
                    uvOffset: uvOffset,
                    blendType: MyBillboard.BlendTypeEnum.AdditiveTop,
                    customViewProjection: customViewProjection,
                    reflection: reflection,
                    persistentBillboards: billboards);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Exception in DrawFlashingSquare: {ex}");
            }
        }

        // Function to determine which face of the bounding box is hit
        private Vector3D GetHitFaceNormal(BoundingBoxD boundingBox, Vector3D hitDirection)
        {
            // We need to find the face that aligns most closely with the hit direction
            Vector3D[] normals = new Vector3D[]
            {
        Vector3D.Forward,  Vector3D.Backward,
        Vector3D.Left,     Vector3D.Right,
        Vector3D.Up,       Vector3D.Down
            };

            // Find the face whose normal is most aligned with the hit direction
            Vector3D closestNormal = Vector3D.Zero;
            double maxDot = -1; // Start with a very low dot product

            foreach (var normal in normals)
            {
                // Calculate dot product to determine how closely aligned the normal is with the hit direction
                double dot = Vector3D.Dot(hitDirection, normal);

                // We want the normal that is most aligned (i.e., the largest dot product)
                if (dot > maxDot)
                {
                    maxDot = dot;
                    closestNormal = normal;
                }
            }

            return closestNormal;
        }

        private void ApplyResistance()
        {
            float resistanceStrength = Settings.ResistanceStrength;

            // oh lmao it made space drag that's hilarius, wrong kind of resistance but might be cool later
            // block.CubeGrid.Physics.LinearDamping = Math.Max(block.CubeGrid.Physics.LinearDamping, resistanceStrength);

            //update grid damage resistance dynamically
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1f - resistanceStrength);
        }

        private static void CreateTerminalControls()
        {
            if (m_controlsCreated)
                return;

            m_controlsCreated = true;

            // Visible only for blocks with ResistBlock logic

            // Create Activate Switch
            var activateSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCollector>("ResistBlock_Terminal_Activate");
            activateSwitch.Title = MyStringId.GetOrCompute("Activate Resistance");
            activateSwitch.OnText = MyStringId.GetOrCompute("On");
            activateSwitch.OffText = MyStringId.GetOrCompute("Off");
            activateSwitch.Visible = CustomVisibleCondition;

            activateSwitch.Getter = (b) => (b.GameLogic.GetAs<ResistBlock>()?.Settings.IsActive) ?? false;
            activateSwitch.Setter = (b, v) =>
            {
                var resistBlock = b.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.IsActive = v;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(activateSwitch);

            // Create Resistance Strength Slider
            var strengthSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>("ResistBlock_Terminal_StrengthSlider");
            strengthSlider.Title = MyStringId.GetOrCompute("Resistance Strength");
            strengthSlider.Tooltip = MyStringId.GetOrCompute("Adjust the resistance strength.");
            strengthSlider.SetLimits(0, 1);
            strengthSlider.Visible = CustomVisibleCondition;

            strengthSlider.Writer = (b, sb) =>
            {
                sb.Append(Math.Round(b.GameLogic.GetAs<ResistBlock>()?.Settings.ResistanceStrength ?? 0, 2).ToString("F2"));
            };
            strengthSlider.Getter = (b) => (b.GameLogic.GetAs<ResistBlock>()?.Settings.ResistanceStrength) ?? 0;
            strengthSlider.Setter = (b, v) =>
            {
                var resistBlock = b.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.ResistanceStrength = v;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(strengthSlider);

            // Adding an Action for Cockpits/Control Stations
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("ResistBlock_Action_Activate");
            action.Name = new StringBuilder("Toggle Resistance");
            action.Writer = (block, stringBuilder) =>
            {
                var resistBlock = block.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    stringBuilder.Append("Resistance: ").Append(resistBlock.Settings.IsActive ? "On" : "Off");
                }
            };
            action.Action = (block) =>
            {
                var resistBlock = block.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.IsActive = !resistBlock.Settings.IsActive;
                }
            };
            action.ValidForGroups = true;
            action.Enabled = CustomVisibleCondition; // Only for blocks with this game logic

            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(action);
        }

        private static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<ResistBlock>() != null;
        }

        internal bool LoadSettings()
        {
            string rawData;
            if (block == null || block.Storage == null || !block.Storage.TryGetValue(ResistSettingsGUID, out rawData))
                return false;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ResistSettings>(Convert.FromBase64String(rawData));
                if (loadedSettings != null)
                {
                    Settings.ResistanceStrength = loadedSettings.ResistanceStrength;
                    Settings.IsActive = loadedSettings.IsActive;
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to load resist settings: {e}");
            }

            return false;
        }

        internal void SaveSettings()
        {
            try
            {
                if (block == null || Settings == null)
                    return;

                if (block.Storage == null)
                    block.Storage = new MyModStorageComponent();

                string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));
                block.Storage[ResistSettingsGUID] = rawData;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to save resist settings: {e}");
            }
        }

        public override bool IsSerialized()
        {
            try
            {
                // Ensure the block and settings are valid
                if (block == null || Settings == null)
                    return false;

                // Safely save the settings if block and settings are properly initialized
                SaveSettings();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to save resist settings in IsSerialized: {e}");
            }

            return base.IsSerialized();
        }

        public override void Close()
        {
            if (IsActiveSync != null)
            {
                IsActiveSync.ValueChanged -= IsActive_ValueChanged;
            }

            // Clean up: Unsubscribe from the global damage handler
            if (MyAPIGateway.Session != null)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, null); // Unregister handler
            }

            base.Close();
        }
    }
}
