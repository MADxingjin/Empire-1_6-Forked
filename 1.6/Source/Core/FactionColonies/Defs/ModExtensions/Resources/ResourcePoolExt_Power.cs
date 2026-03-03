using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Noise;

namespace FactionColonies
{
    public class ResourcePoolExt_Power : ResourcePoolExtension
    {
        public override double createPool(double production, WorldSettlementFC settlement = null)
        {
            return Math.Round(production * 100);
        }
        public override bool resetAtTaxTime()
        {
            return true;
        }
    }
}
