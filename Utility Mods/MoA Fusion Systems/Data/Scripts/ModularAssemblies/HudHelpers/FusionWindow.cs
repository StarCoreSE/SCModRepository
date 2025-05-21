using Epstein_Fusion_DS.Communication;
using Epstein_Fusion_DS.FusionParts;
using Epstein_Fusion_DS.HeatParts;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using RichHudFramework.UI.Rendering.Client;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using RichHudFramework.UI.Client;
using VRage.Game.Entity;
using VRageMath;

namespace Epstein_Fusion_DS.HudHelpers
{
    internal class FusionWindow : CamSpaceNode
    {
        private readonly TexturedBox _backgroundBox, _foregroundBox, _heatBox, _storBox;
        private readonly Label _heatLabel, _storageLabel, _infoLabelLeft;

        private readonly GlyphFormat _stdTextFormat = new GlyphFormat(color: HudConstants.HudTextColor, alignment: TextAlignment.Center, font: FontManager.GetFont("Monospace"));
        private readonly GlyphFormat _stdTextFormatInfo = new GlyphFormat(color: HudConstants.HudTextColor, textSize: 0.6f, alignment: TextAlignment.Left, font: FontManager.GetFont("Monospace"));

        private static readonly Vector3D UnitTransformOffset = new Vector3D(-0.759375, -0.8, 0);

        public FusionWindow(HudParentBase parent) : base(parent)
        {
            RotationAxis = new Vector3(0, 1, 0);
            RotationAngle = 0.25f;

            //var invert = MatrixD.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), 16 / 9d,
            //    MyAPIGateway.Session.Camera.NearPlaneDistance, MyAPIGateway.Session.Camera.FarPlaneDistance);
            //ModularApi.Log(Vector3D.Transform(new Vector3D(-0.0675, -0.04, -0.05), invert).ToString());

            var fovMatrix = MatrixD.Invert(MatrixD.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(MyAPIGateway.Session.Camera.FieldOfViewAngle), HudMain.AspectRatio,
                MyAPIGateway.Session.Camera.NearPlaneDistance, MyAPIGateway.Session.Camera.FarPlaneDistance));

            TransformOffset = Vector3D.Transform(UnitTransformOffset, fovMatrix);


            _backgroundBox = new TexturedBox(this)
            {
                Material = new Material("WhiteSquare", HudConstants.HudSize),
                Size = HudConstants.HudSize,
                Color = HudConstants.HudBackgroundColor,
                IsMasking = true,
                ZOffset = sbyte.MinValue,
                Padding = Vector2.Zero,
            };
            _foregroundBox = new TexturedBox(this)
            {
                Material = new Material("HudBackground", new Vector2(400, 136)),
                Size = HudConstants.HudSize,
                ZOffset = sbyte.MaxValue,
            };

            _heatLabel = new Label(this)
            {
                Text = "00% HEAT",
                Offset = new Vector2(0, 0),
                Format = _stdTextFormat,
                ZOffset = sbyte.MaxValue,
            };
            _storageLabel = new Label(this)
            {
                Text = "00% STOR",
                Offset = new Vector2(0, -_backgroundBox.Size.Y/3),
                Format = _stdTextFormat,
                ZOffset = sbyte.MaxValue,
            };

            _infoLabelLeft = new Label(this)
            {
                Text = "100% INTEGRITY - ALL SYSTEMS NOMINAL",
                Offset = new Vector2(0, _backgroundBox.Size.Y/3),
                Format = _stdTextFormat,
                ZOffset = sbyte.MaxValue,
            };

            _heatBox = new TexturedBox(_backgroundBox)
            {
                Material = new Material("WhiteSquare", new Vector2(384, 38) * HudConstants.HudSizeRatio),
                Size = new Vector2(384, 38) * HudConstants.HudSizeRatio,
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Top | ParentAlignments.Inner,
                Offset = new Vector2(8, -49) * HudConstants.HudSizeRatio,
                ZOffset = 0,
                Color = Color.Red,
            };

            _storBox = new TexturedBox(_backgroundBox)
            {
                Material = new Material("WhiteSquare", new Vector2(384, 38) * HudConstants.HudSizeRatio),
                Size = new Vector2(384, 38) * HudConstants.HudSizeRatio,
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Top | ParentAlignments.Inner,
                Offset = new Vector2(8, -95) * HudConstants.HudSizeRatio,
                ZOffset = 0,
                Color = Color.Orange,
            };
        }


        private static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;
        private int _ticks = 0;
        private bool _shouldHide;
        private MyEntity3DSoundEmitter _soundEmitter = null;
        private readonly MySoundPair _alertSound = new MySoundPair("ArcSoundBlockAlert2");

        public void Update()
        {
            _ticks++;
            var playerCockpit = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity as IMyShipController;

            // Pulling the current HudState is SLOOOOWWWW, so we only pull it when tab is just pressed.
            //if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Tab))
            //    _shouldHide = MyAPIGateway.Session?.Config?.HudState != 1;

            // Hide HUD element if the player isn't in a cockpit
            if (playerCockpit == null || _shouldHide)
            {
                if (Visible) Visible = false;

                if (_soundEmitter != null)
                {
                    _soundEmitter.StopSound(true);
                    _soundEmitter = null;
                }
                return;
            }

            var playerGrid = playerCockpit.CubeGrid;

            float totalFusionCapacity = 0;
            float totalFusionGeneration = 0;
            float totalFusionStored = 0;
            float reactorIntegrity = 0;
            int reactorCount = 0;

            foreach (var system in SFusionManager.I.FusionSystems)
            {
                if (playerGrid != ModularApi.GetAssemblyGrid(system.Key))
                    continue;

                totalFusionCapacity += system.Value.MaxPowerStored;
                totalFusionGeneration += system.Value.PowerGeneration;
                totalFusionStored += system.Value.PowerStored;
                foreach (var reactor in system.Value.Reactors)
                {
                    reactorIntegrity += reactor.Block.SlimBlock.Integrity/reactor.Block.SlimBlock.MaxIntegrity;
                    reactorCount++;
                }
                foreach (var thruster in system.Value.Thrusters)
                {
                    reactorIntegrity += thruster.Block.SlimBlock.Integrity/thruster.Block.SlimBlock.MaxIntegrity;
                    reactorCount++;
                }
            }
            reactorIntegrity /= reactorCount;

            // Hide HUD element if the grid has no fusion systems (capacity is always >0 for a fusion system)
            if (totalFusionCapacity == 0)
            {
                if (Visible) Visible = false;
                return;
            }

            // Show otherwise
            if (!Visible) Visible = true;

            var heatPct = HeatManager.I.GetGridHeatLevel(playerGrid);

            _heatBox.Width = 384 * HudConstants.HudSizeRatio.X * heatPct;
            _heatBox.Color = new Color(heatPct, 1-heatPct, 0, 0.75f);

            _storBox.Width = 384 * HudConstants.HudSizeRatio.X * (totalFusionStored / totalFusionCapacity);

            _infoLabelLeft.Text = new RichText
            {
                {(reactorIntegrity*100).ToString("N0") + "%", _stdTextFormatInfo.WithColor(reactorIntegrity > 0.6 ? Color.White : Color.Red)},
                {" INTEGRITY - ", _stdTextFormatInfo},
                {GetNoticeText(heatPct, reactorIntegrity), GetNoticeFormat(heatPct, reactorIntegrity)},
            };

            _heatLabel.Text = $"{heatPct*100:N0}% HEAT";
            _storageLabel.Text = $"{(totalFusionStored / totalFusionCapacity) * 100:N0}% STOR";

            if (heatPct > 0.8f)
            {
                if (_soundEmitter == null)
                {
                    _soundEmitter = new MyEntity3DSoundEmitter((MyEntity) playerCockpit.Entity)
                    {
                        CanPlayLoopSounds = true
                    };
                    _soundEmitter.PlaySound(_alertSound);
                }
            }
            else
            {
                if (_soundEmitter != null)
                {
                    _soundEmitter.StopSound(true);
                    _soundEmitter = null;
                }
            }
        }

        private int _errRemainingTicks = 0;
        private float _lastIntegrityPct = 1;
        private string _lastErrText = "";
        private string GetNoticeText(float heatPct, float integrityPct)
        {
            if (integrityPct < _lastIntegrityPct && _errRemainingTicks == 0)
                _errRemainingTicks = Utils.Random.Next(60, 120);
            //_lastIntegrityPct = integrityPct;

            string baseText = "ALL SYSTEMS NOMINAL";
            char[] errArray = new[]
            {
                '?',
                '░',
                '▒',
                '▓',
                '█',
                '*',
                '%',
                '@',
            };

            if (integrityPct < 0.1)
                baseText = "  I DON'T WANT TO DIE. ";
            else if (integrityPct < 0.5)
                baseText = " -! REACTOR FAILURE !- ";
            else if (integrityPct < 0.6)
                baseText = "-! SHUTDOWN IMMINENT !-";
            else if (heatPct > 0.8)
                baseText = "  ! THERMAL DAMAGE !   ";
            
            if (_errRemainingTicks > 0 || integrityPct < 0.6)
            {
                if (_errRemainingTicks % 4 == 0)
                {
                    var chars = baseText.ToCharArray();
                    for (int i = Utils.Random.Next(0, baseText.Length/4); i < baseText.Length; i += Utils.Random.Next(1, baseText.Length/2))
                        chars[i] = errArray[Utils.Random.Next(0, errArray.Length - 1)];
                    baseText = new string(chars);
                    _lastErrText = baseText;
                }
                else
                {
                    baseText = _lastErrText;
                }
                _errRemainingTicks--;
            }

            return baseText;
        }

        private GlyphFormat GetNoticeFormat(float heatPct, float integrityPct)
        {
            if (integrityPct < 0.6 || heatPct > 0.8)
                return _stdTextFormatInfo.WithColor(Color.Red);
            else if (_errRemainingTicks > 0)
                return _stdTextFormatInfo.WithColor(Color.Yellow);
            return _stdTextFormatInfo;
        }
    }
}
