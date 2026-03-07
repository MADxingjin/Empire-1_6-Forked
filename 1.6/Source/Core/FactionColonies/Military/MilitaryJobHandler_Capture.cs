using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class MilitaryJobHandler_Capture : MilitaryJobHandler
    {
        public override void OnDeployed(WorldObjectComp_SettlementMilitary milComp, PlanetTile location, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.captureEnemySettlement);
            evt.customDescription = "settlementMilitaryForcesCapturing".Translate(milComp.WorldSettlement.Name, milComp.ReturnMilitaryTarget().Label);
            Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentCapture".Translate(milComp.WorldSettlement.Name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
            evt.DefineEvent(factionfc, milComp.WorldSettlement.Tile, timeToFinish);
        }

        public override bool OnResolved(WorldObjectComp_SettlementMilitary milComp)
        {
            FactionFC faction = FactionCache.FactionComp;
            int winner = SimulateBattleFc.FightBattle(
                militaryForce.CreateMilitaryForceFromSettlement(milComp.WorldSettlement, true),
                militaryForce.CreateMilitaryForceFromFaction(milComp.militaryEnemy, false));

            if (winner == 0)
            {
                faction.AddExperienceToFactionLevel(5f);

                string tmpName = Find.WorldObjects.SettlementAt(milComp.militaryLocation).LabelCap;
                TechLevel tech = Find.WorldObjects.SettlementAt(milComp.militaryLocation).Faction.def.techLevel;
                Faction tempFactionLink = Find.WorldObjects.SettlementAt(milComp.militaryLocation).Faction;
                Find.WorldObjects.SettlementAt(milComp.militaryLocation).Destroy();
                WorldSettlementFC worldsettlement = ColonyUtil.CreatePlayerColonySettlement(milComp.militaryLocation, WorldSettlementDefOf.WorldSettlementDef_Surface);
                worldsettlement.Name = tmpName;

                int upgradeTimes;

                switch (tech)
                {
                    case TechLevel.Archotech:
                    case TechLevel.Ultra:
                    case TechLevel.Spacer:
                        upgradeTimes = 2;
                        break;
                    case TechLevel.Industrial:
                        upgradeTimes = 1;
                        break;
                    default:
                        upgradeTimes = 0;
                        break;
                }

                milComp.WorldSettlement.UpgradeSettlement(upgradeTimes);

                milComp.WorldSettlement.loyalty = 15;
                milComp.WorldSettlement.happiness = 25;
                milComp.WorldSettlement.unrest = 20;
                milComp.WorldSettlement.prosperity = 70;

                bool defeated = !Find.WorldObjects.Settlements.Any(settlement => settlement.Faction != null
                    && settlement.Faction == tempFactionLink);

                if (defeated)
                {
                    tempFactionLink.defeated = true;
                }

                Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                    "CaptureEnemySettlementSuccess".Translate(milComp.WorldSettlement.Name,
                        Find.WorldObjects.SettlementAt(milComp.militaryLocation).Name, milComp.WorldSettlement.settlementLevel),
                    LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(milComp.militaryLocation)));
            }
            else if (winner == 1)
            {
                Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                    "CaptureEnemySettlementFailure".Translate(milComp.WorldSettlement.Name,
                        Find.WorldObjects.SettlementAt(milComp.militaryLocation).Name), LetterDefOf.NegativeEvent,
                    new LookTargets(Find.WorldObjects.SettlementAt(milComp.militaryLocation)));
            }

            return winner == 0;
        }
    }
}
