using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VRage.Game;
using VRage.ObjectBuilders;

namespace SC_BlockRestrictions
{
  public class ModPacket
  {
    public long SenderId;
    public List<SerializableDefinitionId> Definitions;

    public ModPacket() { }

    public ModPacket(long modId, List<SerializableDefinitionId> list)
    {
      SenderId = modId;
      Definitions = list;
    }
  }
}
