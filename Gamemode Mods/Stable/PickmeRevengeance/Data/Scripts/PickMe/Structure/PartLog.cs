using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickMe.Structure
{
    public class PartLog
    {
        public string SubtypeName = "";
        public string SubtypeId = "";
        public int Quantity = 0;

        public PartLog(Part part)
        {
            SubtypeName = part.Name;
            SubtypeId = part.SubtypeID;
        }

        public void Check(Part part)
        {
            if(part.SubtypeID == SubtypeId)
                Quantity++;
        }
    }
}
