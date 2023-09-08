using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    private bool isPredictionDisabled = true;

    public override void LoadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }
    }

    private MyEntity lastControlledEntity = null;

    public override void UpdateAfterSimulation()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyEntity controlledEntity = GetControlledGrid();

            if (controlledEntity != null && !controlledEntity.Equals(lastControlledEntity))
            {
                lastControlledEntity = controlledEntity; // Update the last controlled entity
                MyCubeGrid controlled = controlledEntity as MyCubeGrid;

                if (controlled != null)
                {
                    controlled.ForceDisablePrediction = true;  // Disable prediction here
                    MyAPIGateway.Utilities.ShowNotification($"You are controlling: {controlledEntity.DisplayName}, IsClientPredicted {controlled.IsClientPredicted}", 2000, MyFontEnum.Red);
                }
            }
            else if (controlledEntity == null)
            {
                lastControlledEntity = null; // Reset if no entity is being controlled
            }
        }
    }



    private MyEntity GetControlledGrid()
    {
        var controlledEntity = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity;

        if (controlledEntity is IMyCockpit || controlledEntity is IMyRemoteControl)
        {
            return (controlledEntity as IMyCubeBlock).CubeGrid as MyEntity;
        }

        return null;
    }


    private void OnMessageEntered(string messageText, ref bool sendToOthers)
    {
        if (messageText.Equals("/toggleprediction"))
        {
            isPredictionDisabled = !isPredictionDisabled;
            MyAPIGateway.Utilities.ShowNotification($"ForceDisablePrediction: {isPredictionDisabled}", 2000, MyFontEnum.Red);
            sendToOthers = false;
        }
    }

    protected override void UnloadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
        }
    }
}
