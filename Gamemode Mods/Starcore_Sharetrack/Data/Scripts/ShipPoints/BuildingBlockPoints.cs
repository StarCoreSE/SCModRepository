using System.Text;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using StarCore.ShareTrack.API;
using VRageMath;
using VRageRender;

namespace StarCore.ShareTrack
{
    internal class BuildingBlockPoints
    {
        internal string LastHeldSubtype;
        private HudAPIv2.HUDMessage _pointsMessage;

        public BuildingBlockPoints()
        {
            MasterSession.I.HudRegistered += () =>
            {
                _pointsMessage = new HudAPIv2.HUDMessage(scale: 1f, font: "BI_SEOutlined", Message: new StringBuilder(""),
                    origin: new Vector2D(-0.969, 0.57), blend: MyBillboard.BlendTypeEnum.PostPP);
            };
        }

        private int _ticks;
        public void Update()
        {
            if (_ticks++ % 10 != 0)
                return;

            if (LastHeldSubtype != MyHud.BlockInfo?.DefinitionId.SubtypeName)
            {
                LastHeldSubtype = MyHud.BlockInfo?.DefinitionId.SubtypeName;
                UpdateHud(MyHud.BlockInfo);
            }
        }

        private void UpdateHud(MyHudBlockInfo blockInfo)
        {
            if (_pointsMessage == null)
                return;

            int blockPoints;
            if (blockInfo == null || !AllGridsList.PointValues.TryGetValue(blockInfo.DefinitionId.SubtypeName, out blockPoints))
            {
                _pointsMessage.Visible = false;
                return;
            }

            string blockDisplayName = blockInfo.BlockName;

            float thisClimbingCostMult = 0;
            AllGridsList.ClimbingCostRename(ref blockDisplayName, ref thisClimbingCostMult);

            _pointsMessage.Message.Clear();
            _pointsMessage.Message.Append($"{blockDisplayName}:\n{blockPoints}bp");
            if (thisClimbingCostMult != 0)
                _pointsMessage.Message.Append($" +{(int)(blockPoints*thisClimbingCostMult)}bp/b");
            _pointsMessage.Visible = true;
        }
    }
}
