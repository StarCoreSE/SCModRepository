﻿using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace SC.GyroscopicGunsight.API.CoreSystems
{
    /// <summary>
    /// https://github.com/sstixrud/CoreSystems/blob/master/BaseData/Scripts/CoreSystems/Api/CoreSystemsApiBlocks.cs
    /// </summary>
    public partial class WcApi 
    {
        private Func<IMyTerminalBlock, IDictionary<string, int>, bool> _getBlockWeaponMap;

        public bool GetBlockWeaponMap(IMyTerminalBlock weaponBlock, IDictionary<string, int> collection) =>
            _getBlockWeaponMap?.Invoke(weaponBlock, collection) ?? false;
    }
}
