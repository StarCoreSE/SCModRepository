using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Color = VRageMath.Color;

namespace klime.Visual
{
    /// <summary>
    /// Grid rendering class
    /// </summary>
    public class GridRendering
    {
        // Constants
        private const float FOV_THRESHOLD = 1.2f;
        private const float LOWER_SCALE_LIMIT = 0.03f;
        private const float UPPER_SCALE_LIMIT = 0.04f;
        private const float BILLBOARD_SCALE_FACTOR = 0.9f;
        private const float BILLBOARD_SCALE_MAX = 0.09f;
        private const float FOV_SCALE_FACTOR = 0.1f;
        private const float RENDER_SCALE_FACTOR = 0.15f;
        private const float BACK_OFFSET_FACTOR = 0.1f;
        private const float BACK_OFFSET_THRESHOLD = 0.001f;
        private const float RESCALE_CONSTANT = 1.35f;
        private const float HUD_SCALE_MIN = 0.0001f;
        private const float HUD_SCALE_MAX = 0.05f;
        private const float SCALE_MIN = 0.0001f;
        private const float SCALE_MAX = 0.0008f;
        private const float LARGE_GRID_RADIUS = 150f;
        private const float SMALL_GRID_SCALE_MIN = 0.0005f;
        private const float BILLBOARD_OPACITY = .5f;
        private const float BLOCK_TRANSPARENCY = 0.43f;

        // Fields
        public MyCubeGrid grid;
        public EntRender entRender;
        public MatrixD controlMatrix;
        public double scale;
        private Vector3D relTrans;
        public BoundingBoxD gridBox;
        public static MatrixD gridMatrix;
        public static MatrixD gridMatrixBackground;
        public static float billboardScaling;
        public Color BillboardRED;
        public Vector4 Billboardcolor;
        private MyStringId PaperDollBGSprite = MyStringId.TryGet("paperdollBG");
        private string ArmorMat = "Hazard_Armor";
        private string ArmorRecolorHex = "#FFF0E0";
        public bool runonce;
        public float oldFOV;
        public bool needsRescale;
        public static Vector3D GridBoxCenter;
        public static Vector3D GridBoxCenterGlobal;
        public static Vector3D hateVector;

        /// <summary>
        /// Initializes a new instance of the GridRendering class.
        /// </summary>
        /// <param name="grid">The MyCubeGrid to render.</param>
        /// <param name="entRender">Optional EntRender instance.</param>
        public GridRendering(MyCubeGrid grid, EntRender entRender = null)
        {
            this.grid = grid;
            this.entRender = entRender ?? new EntRender();
            this.scale = 0.8;
        }

        public GridRendering(MyCubeGrid grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// Updates the rendering matrix for the grid.
        /// </summary>
        /// <param name="renderMatrix">The matrix to update.</param>
        public void UpdateMatrix(MatrixD renderMatrix)
        {
            var camera = MyAPIGateway.Session.Camera;
            float newFov = camera.FovWithZoom;

            // Check if FOV has changed
            if (Math.Abs(oldFOV - newFov) > float.Epsilon)
            {
                oldFOV = newFov;
                needsRescale = true;
            }

            // Calculate scaling factors
            float aspect = camera.ViewportSize.X / camera.ViewportSize.Y;
            float fovTan = (float)Math.Tan(newFov * 0.5f);
            float scaleFov = FOV_SCALE_FACTOR * fovTan;
            float scale = scaleFov * RENDER_SCALE_FACTOR;

            // Update grid matrix
            MatrixD clonedWorldMatrix = grid.WorldMatrix;
            Vector3D gridCenter = grid.PositionComp.LocalAABB.Center;
            clonedWorldMatrix.Translation += Vector3D.Transform(gridCenter, grid.PositionComp.WorldMatrixRef);
            renderMatrix.Translation += Vector3D.TransformNormal(relTrans, clonedWorldMatrix);

            // Calculate hate vector
            hateVector = renderMatrix.Translation - Vector3D.TransformNormal(relTrans, clonedWorldMatrix);

            // Calculate billboard scaling
            float lowerLimit = newFov < FOV_THRESHOLD ? LOWER_SCALE_LIMIT : UPPER_SCALE_LIMIT;
            float tempBillboardScaling = MathHelper.Clamp((float)grid.PositionComp.Scale * BILLBOARD_SCALE_FACTOR, lowerLimit, BILLBOARD_SCALE_MAX);
            billboardScaling = tempBillboardScaling;

            // Rescale if needed
            if (needsRescale)
            {
                float backOffset = (scale - BACK_OFFSET_THRESHOLD) * BACK_OFFSET_FACTOR;
                if (backOffset > 0)
                {
                    Vector3D moveVec = Vector3D.TransformNormal(-camera.WorldMatrix.Forward, MatrixD.Transpose(renderMatrix));
                    renderMatrix.Translation += moveVec * backOffset;
                }
                DoRescale();
            }

            // Update grid matrices
            grid.WorldMatrix = renderMatrix;
            gridMatrixBackground = renderMatrix;

            // Add billboard
            Vector3D left = camera.WorldMatrix.Left;
            Vector3D up = camera.WorldMatrix.Up;
            Color billboardColor = Color.OrangeRed * BILLBOARD_OPACITY;
            MyTransparentGeometry.AddBillboardOriented(PaperDollBGSprite, billboardColor.ToVector4(), hateVector, left, up, tempBillboardScaling, BlendTypeEnum.LDR);
        }

        /// <summary>
        /// Rescales the grid.
        /// </summary>
        public void DoRescale()
        {
            HandleException(() =>
            {
                var worldVolume = grid.PositionComp.WorldVolume;
                var worldMatrixRef = grid.PositionComp.WorldMatrixRef;
                var worldRadius = worldVolume.Radius;

                var camera = MyAPIGateway.Session.Camera;
                var newFov = camera.FovWithZoom;
                var fov = Math.Tan(newFov * 0.5);
                var scaleFov = FOV_SCALE_FACTOR * fov;

                float hudScale = (float)(RESCALE_CONSTANT / worldRadius);
                hudScale = MathHelper.Clamp(hudScale, HUD_SCALE_MIN, HUD_SCALE_MAX);

                var minScale = worldRadius > LARGE_GRID_RADIUS ? SCALE_MIN : SMALL_GRID_SCALE_MIN;
                var newScale = MathHelper.Clamp((float)(scaleFov * (hudScale * 0.23f)), minScale, SCALE_MAX);

                var modifiedCenter = Vector3D.Transform(GridBoxCenter, worldMatrixRef);
                controlMatrix *= MatrixD.CreateTranslation(-modifiedCenter) * worldMatrixRef;

                var localCenter = new Vector3D(worldVolume.Center);
                var trueCenter = Vector3D.Transform(localCenter, grid.WorldMatrix);

                grid.PositionComp.Scale = newScale;
                relTrans = Vector3D.TransformNormal(GridBoxCenter, MatrixD.Transpose(grid.WorldMatrix)) * newScale;
                GridBoxCenter = grid.PositionComp.LocalVolume.Center;
                relTrans = -GridBoxCenter;

                needsRescale = false;
            }, "rescaling grid");
        }

        /// <summary>
        /// Performs cleanup operations on the grid.
        /// </summary>
        public void DoCleanup()
        {
            ChangeOwnership();
            SetupRendering();
            ProcessBlocks();
        }

        /// <summary>
        /// Changes the ownership of the grid.
        /// </summary>
        private void ChangeOwnership()
        {
            HandleException(() =>
            {
                grid.ChangeGridOwnership(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);
            }, "changing grid ownership");
        }

        /// <summary>
        /// Sets up rendering properties for the grid.
        /// </summary>
        private void SetupRendering()
        {
            IMyCubeGrid iGrid = (IMyCubeGrid)grid;
            iGrid.Render.DrawInAllCascades = true;
            iGrid.Render.FastCastShadowResolve = true;
            iGrid.Render.MetalnessColorable = true;
        }

        /// <summary>
        /// Recolors the armor at the specified position.
        /// </summary>
        private void RecolorArmor(Vector3I pos)
        {
            Vector3 armorHSV = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(ArmorRecolorHex)));
            armorHSV = RoundVector(armorHSV, 2);
            MyCubeGrid iGrid = (MyCubeGrid)grid;
            var stringHash = MyStringHash.GetOrCompute(ArmorMat);
            iGrid.ChangeColorAndSkin(iGrid.GetCubeBlock(pos), armorHSV, stringHash);
        }

        /// <summary>
        /// Rounds a Vector3 to a specified number of decimal places.
        /// </summary>
        private static Vector3 RoundVector(Vector3 vec, int decimals)
        {
            return new Vector3(
                (float)Math.Round(vec.X, decimals),
                (float)Math.Round(vec.Y, decimals),
                (float)Math.Round(vec.Z, decimals)
            );
        }

        /// <summary>
        /// Processes all blocks in the grid.
        /// </summary>
        private void ProcessBlocks()
        {
            List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();
            IMyCubeGrid iGrid = (IMyCubeGrid)grid;
            iGrid.GetBlocks(allBlocks);

            foreach (var block in allBlocks)
            {
                var pos = block.Position;
                RecolorArmor(pos);
                DisableBlock(block.FatBlock as IMyFunctionalBlock);
                StopEffects(block.FatBlock as IMyExhaustBlock);
                SetTransparency(block, BLOCK_TRANSPARENCY);
            }
        }

        /// <summary>
        /// Sets the transparency of a block.
        /// </summary>
        private static void SetTransparency(IMySlimBlock cubeBlock, float transparency)
        {
            transparency = 0f - transparency;
            if (cubeBlock.Dithering != transparency || cubeBlock.CubeGrid.Render.Transparency != transparency)
            {
                cubeBlock.CubeGrid.Render.Transparency = transparency;
                cubeBlock.CubeGrid.Render.CastShadows = false;
                cubeBlock.Dithering = transparency;
                cubeBlock.UpdateVisual();

                MyCubeBlock fatBlock = cubeBlock.FatBlock as MyCubeBlock;
                if (fatBlock != null)
                {
                    fatBlock.Render.CastShadows = false;
                    SetTransparencyForSubparts(fatBlock, transparency);

                    if (fatBlock.UseObjectsComponent != null && fatBlock.UseObjectsComponent.DetectorPhysics != null)
                    {
                        fatBlock.UseObjectsComponent.DetectorPhysics.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the transparency for subparts of an entity.
        /// </summary>
        private static void SetTransparencyForSubparts(MyEntity renderEntity, float transparency)
        {
            renderEntity.Render.CastShadows = false;
            if (renderEntity.Subparts == null) return;

            foreach (var subpart in renderEntity.Subparts)
            {
                subpart.Value.Render.Transparency = transparency;
                subpart.Value.Render.CastShadows = false;
                subpart.Value.Render.RemoveRenderObjects();
                SetTransparencyForSubparts(subpart.Value, transparency);
            }
        }

        /// <summary>
        /// Disables a functional block.
        /// </summary>
        private void DisableBlock(IMyFunctionalBlock block)
        {
            if (block != null)
            {
                block.Enabled = false;
            }
        }

        /// <summary>
        /// Stops effects on an exhaust block.
        /// </summary>
        private void StopEffects(IMyExhaustBlock exhaust)
        {
            exhaust?.StopEffects();
        }

        /// <summary>
        /// Handles exceptions and logs errors.
        /// </summary>
        private void HandleException(Action action, string errorContext)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error {errorContext}: {e.Message}");
                MyAPIGateway.Utilities.ShowNotification($"An error occurred while {errorContext}. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }
    }
}