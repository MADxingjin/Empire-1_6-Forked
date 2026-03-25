using System;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Innovative : FCPolicyBehavior
    {
        private const double ProfitToResearchRate = 0.05;

        public override void OnTaxCollected(FactionFC faction, WorldSettlementFC settlement)
        {
            double profit = settlement.totalProfit;
            if (profit <= 0.0) return;

            ResourcePool pool = faction.resourcePools.Find(p => p.resource == ResourceTypeDefOf.RTD_Research);
            if (pool == null) return;
            double researchPoints = profit * ProfitToResearchRate;
            pool.pool += researchPoints;
            
            Messages.Message("InnovativeMessage".Translate(settlement.Name, Math.Round(researchPoints)), MessageTypeDefOf.PositiveEvent);
        }
    }
}
