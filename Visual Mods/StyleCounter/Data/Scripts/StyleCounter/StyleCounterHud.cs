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

        internal readonly StyleRank[] Ranks = new[]
        {
            new StyleRank
            {
                Name = "DESTRUCTIVE",
                MinPoints = 0,
                MaxPoints = 200,
                DecayRate = 1.0f,
            },
            new StyleRank
            {
                Name = "CHAOTIC",
                MinPoints = 200,
                MaxPoints = 300,
                DecayRate = 1.25f,
            },
            new StyleRank
            {
                Name = "BRUTAL",
                MinPoints = 300,
                MaxPoints = 400,
                DecayRate = 1.5f,
            },
            new StyleRank
            {
                Name = "ANARCHIC",
                MinPoints = 400,
                MaxPoints = 500,
                DecayRate = 2.0f,
            },
            new StyleRank
            {
                Name = "SUPREME",
                MinPoints = 500,
                MaxPoints = 700,
                DecayRate = 3.0f,
            },
            new StyleRank
            {
                Name = "SSADISTIC",
                MinPoints = 700,
                MaxPoints = 850,
                DecayRate = 4.0f,
            },
            new StyleRank
            {
                Name = "SSSHITSTORM",
                MinPoints = 850,
                MaxPoints = 1000,
                DecayRate = 6.0f,
            },
            new StyleRank
            {
                Name = "ULTRACRAB",
                MinPoints = 1000,
                MaxPoints = 1500,
                DecayRate = 8.0f,
            },
        };

        public float StylePoints = 0;

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

        public void UpdateStyleMeter()
        {
            if (StylePoints <= 0)
            {
                Visible = false;
                StylePoints = 0;
                return;
            }

            if (StylePoints > Ranks[Ranks.Length - 1].MaxPoints)
                StylePoints = Ranks[Ranks.Length - 1].MaxPoints;

            Visible = true;

            StyleRank rank = Ranks[0];
            for (int i = Ranks.Length - 1; i >= 0; i--)
            {
                if (StylePoints < Ranks[i].MinPoints)
                    continue;
                rank = Ranks[i];
                break;
            }

            MyAPIGateway.Utilities.ShowNotification(rank.Name, 1000 / 60);
            float percent = (float)(StylePoints - rank.MinPoints) / (rank.MaxPoints - rank.MinPoints);

            _meterBar.Size = new Vector2(368 * percent, 30);
            _meterBar.Offset = new Vector2(-184 + 368 * percent / 2, 58);

            StylePoints -= 15 * rank.DecayRate / 60;
        }

        internal class StyleRank
        {
            public string Name;
            public int MinPoints;
            public int MaxPoints;
            public float DecayRate;
            public Material Texture;
        }
    }
}
