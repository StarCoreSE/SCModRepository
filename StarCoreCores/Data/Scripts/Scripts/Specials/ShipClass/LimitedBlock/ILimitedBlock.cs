using System.Collections.Generic;
using Sandbox.ModAPI;

namespace MIG.SpecCores
{
    public interface ILimitedBlock
    {
        bool IsDrainingPoints();
        void Disable(int reason);
        long EntityId();
        Limits GetLimits();
        bool CheckConditions(ISpecBlock specblock);
        IMyTerminalBlock GetBlock();
        LimitedBlockNetworking Component { get; }
        void Enable();
        bool Punish(Dictionary<int, bool> shouldPunish);
        float DisableOrder();
        void Destroy();
        bool WasInLimitLastTick { get; set; }
    }
}