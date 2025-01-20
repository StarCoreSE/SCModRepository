﻿using GlyphFormatMembers = VRage.MyTuple<byte, float, VRageMath.Vector2I, VRageMath.Color>;

namespace RichHudFramework.UI.Client
{
    /// <summary>
    ///     Clickable button. Mimics the appearance of the terminal button in the SE terminal.
    /// </summary>
    public class TerminalButton : TerminalControlBase
    {
        public TerminalButton() : base(MenuControls.TerminalButton)
        {
        }
    }
}