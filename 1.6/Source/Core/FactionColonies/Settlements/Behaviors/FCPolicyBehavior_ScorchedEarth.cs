using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Scorched Earth doctrine behavior: on defensive victory, each occupied building slot
    /// has a chance of being destroyed as collateral damage.
    /// </summary>
    public class FCPolicyBehavior_ScorchedEarth : FCPolicyBehavior
    {
        public override void OnBattleResolved(FactionFC faction, WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result)
        {
            if (!victory) return;
            if (job != MilitaryJobDefOf.DefendFriendlySettlement) return;
            if (settlement?.BuildingsComp == null) return;

            List<int> damaged = new List<int>();
            for (int k = 0; k < 4; k++)
            {
                if (!settlement.BuildingsComp.BuildingSlotIsBuilding(k)) continue;
                // ~18% chance per occupied slot
                if (new IntRange(0, 10).RandomInRange >= 9)
                {
                    damaged.Add(k);
                }
            }

            if (damaged.Count == 0) return;

            // Sort so dependent buildings are demolished first (same logic as LoseBattle)
            damaged.Sort((a, b) =>
            {
                BuildingFCDef defA = settlement.BuildingsComp.GetBuildingInSlot(a);
                BuildingFCDef defB = settlement.BuildingsComp.GetBuildingInSlot(b);
                bool aRequiresB = defA.requiredBuildings != null && defA.requiredBuildings.Contains(defB);
                bool bRequiresA = defB.requiredBuildings != null && defB.requiredBuildings.Contains(defA);
                if (aRequiresB) return -1;
                if (bRequiresA) return 1;
                int aReqCount = defA.requiredBuildings?.Count ?? 0;
                int bReqCount = defB.requiredBuildings?.Count ?? 0;
                return bReqCount.CompareTo(aReqCount);
            });

            string msg = "ScorchedEarthCollateralHeader".Translate(settlement.Name);
            foreach (int k in damaged)
            {
                msg += "\n  - " + "BuildingDestroyedInRaid".Translate(settlement.BuildingsComp.BuildingLabel(k));
                settlement.DeconstructBuilding(k);
            }

            Find.LetterStack.ReceiveLetter(
                "ScorchedEarthCollateralTitle".Translate(),
                msg,
                LetterDefOf.NegativeEvent,
                new LookTargets(settlement));
        }
    }
}
