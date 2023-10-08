using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using klime.FaceSmash;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Klime.FaceSmash
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ControlPanel), false,"FaceSmashPanelLarge","FaceSmashPanelSmall")]
    public class FaceSmashBlock : MyGameLogicComponent
    {
        IMyControlPanel faceSmashBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                faceSmashBlock = Entity as IMyControlPanel;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (faceSmashBlock.CubeGrid.Physics != null && FaceSmashSession.instance != null)
            {
                FaceSmashSession.instance.AddBlock(faceSmashBlock);
            }
        }

        public override void Close()
        {
            if (MyAPIGateway.Session.IsServer && faceSmashBlock != null && FaceSmashSession.instance != null)
            {
                FaceSmashSession.instance.RemoveBlock(faceSmashBlock);
            }
        }
    }
}