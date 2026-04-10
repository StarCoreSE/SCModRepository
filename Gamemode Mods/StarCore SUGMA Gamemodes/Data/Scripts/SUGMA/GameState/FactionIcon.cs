using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SC.SUGMA.API;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SC.SUGMA.GameState
{
    internal class FactionIcon : TexturedBox
    {
        public FactionIcon(IMyFaction faction, HudParentBase parent, bool small) : base(parent)
        {
            if (!faction.FactionIcon.HasValue)
                throw new Exception("Invalid faction icon!");

            MyStringId iconId;
            if (!FactionIconApi.TryGetIcon(faction, small, out iconId))
            {
                Color = faction.CustomColor.ColorMaskToRgb();
            }

            this.Material = new Material(iconId, small ? new Vector2(64, 64) : new Vector2(512, 512));
            Log.Info($"{faction.Tag} faction icon is: {iconId.String}");
        }
    }
}
