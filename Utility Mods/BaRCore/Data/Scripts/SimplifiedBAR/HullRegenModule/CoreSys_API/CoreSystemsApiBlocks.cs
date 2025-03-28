using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;

namespace CoreSystems.Api
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
