using System;
using Digi;
using MIG.Shared.SE;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public static class TorchExtensions
    {
        public static Action<IMySlimBlock, float, long, MyInventoryBase, float, bool, MyOwnershipShareModeEnum, bool> IncreaseMountLevel =
                (
                    block,
                    welderMountAmount,
                    welderOwnerIdentId,
                    outputInventory,
                    maxAllowedBoneMovement,
                    isHelping,
                    sharing,
                    handWelded) =>
                {
                    block.IncreaseMountLevel(welderMountAmount, welderOwnerIdentId, (IMyInventory) outputInventory,
                        maxAllowedBoneMovement, isHelping, sharing);

                    if (OriginalSpecCoreSession.IsDebug)
                    {
                        Log.ChatError("Weld may work incorrectly in SP");
                    }
                };
        
        public static void Init()
        {
            ModConnection.Subscribe("MIG.APIExtender.IncreaseMountLevel", IncreaseMountLevel, (func) => IncreaseMountLevel = func);
        } 
    }
}