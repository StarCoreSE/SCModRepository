using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using VRageMath;
using VRageRender;

namespace StarCore.StyleCounter
{
    internal class StyleCounterHud : CamSpaceNode
    {
        // TODO: Only show if Style Meter:tm: is above 0

        private readonly TexturedBox _backgroundBox;
        private readonly TexturedBox _meterBar;

        public StyleCounterHud(HudParentBase parent) : base(parent)
        {
            _backgroundBox = new TexturedBox(this)
            {
                Material = new Material("CounterBackground", new Vector2(500, 300)),
                Size = new Vector2(400, 240),
            };
            _meterBar = new TexturedBox(this)
            {
                Size = new Vector2(368, 30),
                Color = Color.White,
                Offset = new Vector2(0, 58),
            };

            RotationAxis = new Vector3(0, -1, 0);
            RotationAngle = 0.4f;
            TransformOffset = new Vector3D(0.06, -0.03, -0.05);
        }

        public void SetStyleMeter(float percent)
        {
            _meterBar.Size = new Vector2(368 * percent, 30);
            _meterBar.Offset = new Vector2(-184 + 368 * percent/2, 58);
        }
    }
}
