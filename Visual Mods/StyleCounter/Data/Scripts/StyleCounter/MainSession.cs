using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Input;

namespace StarCore.StyleCounter
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MainSession : MySessionComponentBase
    {
        internal StyleCounterHud Hud;

        #region Base Methods

        public override void LoadData()
        {
            RichHudClient.Init(ModContext.ModName, InitDraw, null);
        }

        protected override void UnloadData()
        {

        }

        public override void Draw()
        {
            if (!RichHudClient.Registered)
                return;

            Hud.UpdateStyleMeter();

            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Insert))
                Hud.StylePoints += 50;
        }

        #endregion

        private void InitDraw()
        {
            Hud = new StyleCounterHud(HudMain.HighDpiRoot);
        }
    }
}
