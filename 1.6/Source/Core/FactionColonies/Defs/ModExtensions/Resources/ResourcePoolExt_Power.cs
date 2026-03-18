using System;

namespace FactionColonies
{
    public class ResourcePoolExt_Power : ResourcePoolExtension
    {
        public override double CreatePool(double production, WorldSettlementFC settlement = null)
        {
            return Math.Round(production * 100);
        }
        public override bool ResetAtTaxTime()
        {
            return true;
        }
    }
}
