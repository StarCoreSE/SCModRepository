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

        public override void Draw()
        {
            if (!RichHudClient.Registered)
                return;


        }

        #endregion

        private void InitDraw()
        {
            Hud = new StyleCounterHud(HudMain.HighDpiRoot);
        }
    }
}
