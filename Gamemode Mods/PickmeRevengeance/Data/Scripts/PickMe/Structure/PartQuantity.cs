using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickMe.Structure
{
    class PartQuantity
    {
        public string SubtypeName = "";
        public int Quantity = 1;

        public PartQuantity(string subtypeName)
        {
            SubtypeName = subtypeName;
        }

        public void Increment()
        {
            Quantity++;
        }
    }
}
