using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    private bool isPredictionDisabled = false;

    public override void LoadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }
    }

    private void OnMessageEntered(string messageText, ref bool sendToOthers)
    {
        if (!MyAPIGateway.Utilities.IsDedicated && messageText.Equals("/toggleprediction"))
        {
            isPredictionDisabled = !isPredictionDisabled;
            MyCubeGrid controlled = MySession.Static?.ControlledGrid;
            if (controlled != null)
            {
                controlled.ForceDisablePrediction = isPredictionDisabled;
            }
            MyAPIGateway.Utilities.ShowNotification($"Prediction disabled: {isPredictionDisabled}", 2000, MyFontEnum.Red);
            sendToOthers = false;
        }
    }

    public override void UpdateAfterSimulation()
    {
        if (!MyAPIGateway.Utilities.IsDedicated && isPredictionDisabled)
        {
            MyCubeGrid controlled = MySession.Static?.ControlledGrid;
            if (controlled != null)
            {
                controlled.ForceDisablePrediction = true;
            }
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