using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.ModAPI;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    public override void LoadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.ShowNotification("Mod Loaded", 2000, MyFontEnum.Red);
        }
    }

    public override void UpdateAfterSimulation()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyEntity controlledEntity = GetControlledGrid();

            if (controlledEntity != null)
            {
                string gridName = controlledEntity.DisplayName;
                MyAPIGateway.Utilities.ShowNotification($"You are controlling: {gridName}", 2000, MyFontEnum.Red);
            }
        }
    }

    private MyEntity GetControlledGrid()
    {
        if (MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity is IMyCockpit)
        {
            IMyCockpit cockpit = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as IMyCockpit;
            return cockpit.CubeGrid as MyEntity;
        }
        return null;
    }

    protected override void UnloadData()
    {
        if (!MyAPIGateway.Utilities.IsDedicated)
        {
            MyAPIGateway.Utilities.ShowNotification("Mod Unloaded", 2000, MyFontEnum.Red);
        }
    }
}
