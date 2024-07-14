using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using VRage.Game.Components;

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

        private int _ticks = 0;
        public override void Draw()
        {
            if (!RichHudClient.Registered)
                return;

            Hud.SetStyleMeter((_ticks % 120)/120f);
            _ticks++;
        }

        #endregion

        private void InitDraw()
        {
            Hud = new StyleCounterHud(HudMain.HighDpiRoot);
        }
    }
}
