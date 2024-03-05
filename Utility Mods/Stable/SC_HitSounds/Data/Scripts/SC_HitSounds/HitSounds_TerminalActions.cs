using CoreSystems.Api;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Utils;

namespace Jnick_SCModRepository.SC_HitSounds.Data.Scripts.SC_HitSounds
{
    internal class HitSounds_TerminalActions
    {
        List<IMyConveyorSorter> validSorterWeapons = new List<IMyConveyorSorter>();

        public void CreateTerminalActions(WcApi wcApi)
        {
            var SoundToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyConveyorSorter>("HitSounds_HitSoundEnabled");
            SoundToggle.Title = MyStringId.GetOrCompute("HitSoundEnabled");
            SoundToggle.Tooltip = MyStringId.GetOrCompute("Toggles whether this weapon should have hit sounds.");
            SoundToggle.SupportsMultipleBlocks = true; // wether this control should be visible when multiple blocks are selected (as long as they all have this control).
                                                       // callbacks to determine if the control should be visible or not-grayed-out(Enabled) depending on whatever custom condition you want, given a block instance.
                                                       // optional, they both default to true.

            //SoundToggle.Visible = (block) => wcApi.HasCoreWeapon((MyEntity) block);
            SoundToggle.OnText = MySpaceTexts.SwitchText_On;
            SoundToggle.OffText = MySpaceTexts.SwitchText_Off;
            SoundToggle.Getter = (block) => validSorterWeapons.Contains((IMyConveyorSorter) block);
            SoundToggle.Setter = (block, value) =>
            {
                bool hasBlock = validSorterWeapons.Contains((IMyConveyorSorter) block);
                if (value && !hasBlock)
                {
                    validSorterWeapons.Add((IMyConveyorSorter) block);
                }
                else if (hasBlock)
                {
                    validSorterWeapons.Remove((IMyConveyorSorter) block);
                }
            };

            MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(SoundToggle);
        }
    }
}
