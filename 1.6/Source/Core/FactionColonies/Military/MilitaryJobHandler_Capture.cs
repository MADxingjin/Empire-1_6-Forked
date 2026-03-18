using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
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
            Settlement target = Find.WorldObjects.SettlementAt(location);
            Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentCapture".Translate(milComp.WorldSettlement.Name, target?.LabelCap ?? (TaggedString)""), LetterDefOf.NeutralEvent);
            evt.DefineEvent(factionfc, milComp.WorldSettlement.Tile, timeToFinish);
        }

        public override BattleResult OnResolved(WorldObjectComp_SettlementMilitary milComp)
        {
            FactionFC faction = FactionCache.FactionComp;

            Settlement target = Find.WorldObjects.SettlementAt(milComp.militaryLocation);
            if (target == null)
            {
                LogUtil.Warning("Military capture target at tile " + milComp.militaryLocation + " no longer exists");
                return new BattleResult();
            }

            BattleResult result = SimulateBattleFc.FightBattle(
                militaryForce.CreateMilitaryForceFromSettlement(milComp.WorldSettlement, true),
                militaryForce.CreateMilitaryForceFromFaction(milComp.militaryEnemy, false));

            if (result.AttackerVictory)
            {
                faction.AddExperienceToFactionLevel(5f);

                string tmpName = target.LabelCap;
                TechLevel tech = target.Faction.def.techLevel;
                Faction tempFactionLink = target.Faction;
                target.Destroy();
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
                        worldsettlement.Name, milComp.WorldSettlement.settlementLevel),
                    LetterDefOf.PositiveEvent, new LookTargets(worldsettlement));
            }
            else if (result.DefenderVictory)
            {
                Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                    "CaptureEnemySettlementFailure".Translate(milComp.WorldSettlement.Name,
                        target.Name), LetterDefOf.NegativeEvent,
                    new LookTargets(target));
            }

            return result;
        }
    }
}
