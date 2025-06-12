using ParallelTasks;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;
using Sandbox.Game;
using VRage;
using VRage.Input;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using Sandbox.Definitions;

namespace SC_BlockRestrictions
{
  public class GridCollection
  {
    internal MyCubeGrid BaseGrid;
    internal HashSet<IMyCubeGrid> CubeGridHash;

    internal bool IsStatic => ContainsStaticMember();

    public GridCollection(MyCubeGrid grid)
    {
      BaseGrid = grid;
      CubeGridHash = new HashSet<IMyCubeGrid>();
    }

    internal void Close()
    {
      CubeGridHash?.Clear();
      CubeGridHash = null;
      BaseGrid = null;
    }

    internal bool AddGrid(MyCubeGrid grid)
    {
      if (BaseGrid == null)
        BaseGrid = grid;

      if (CubeGridHash == null)
        CubeGridHash = new HashSet<IMyCubeGrid>();
      
      return CubeGridHash.Add(grid);
    }

    bool ContainsStaticMember()
    {
      if (CubeGridHash == null)
      {
        CubeGridHash = new HashSet<IMyCubeGrid>();
        return false;
      }

      foreach (var grid in CubeGridHash)
      {
        if (grid?.IsStatic == true)
          return true;
      }

      return false;
    }
  }
}
