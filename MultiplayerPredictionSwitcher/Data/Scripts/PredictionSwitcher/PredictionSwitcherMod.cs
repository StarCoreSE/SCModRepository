using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using System;
using VRage.Utils;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    private bool isPredictionDisabled = true;
    private MyEntity lastControlledEntity = null;

    public override void LoadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }
    }

    public override void UpdateAfterSimulation()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyEntity controlledEntity = GetControlledGrid();

            if (controlledEntity != null)
            {
                lastControlledEntity = controlledEntity;
                MyCubeGrid controlled = controlledEntity as MyCubeGrid;

                if (controlled != null)
                {
                    controlled.ForceDisablePrediction = isPredictionDisabled;
                    MyAPIGateway.Utilities.ShowNotification($"You are controlling: {controlledEntity.DisplayName}, ForceDisablePrediction: {isPredictionDisabled}", 2000, MyFontEnum.Red);
                }
            }
            else
            {
                lastControlledEntity = null;
            }
        }
    }

    private MyEntity GetControlledGrid()
    {
        try
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
            {
                return null;
            }

            var controlledEntity = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity;
            if (controlledEntity == null)
            {
                return null;
            }

            if (controlledEntity is IMyCockpit || controlledEntity is IMyRemoteControl)
            {
                return (controlledEntity as IMyCubeBlock).CubeGrid as MyEntity;
            }
        }
        catch (Exception e)
        {
            MyLog.Default.WriteLine($"Error in GetControlledGrid: {e}");
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
