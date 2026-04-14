using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{
    public class MilitaryJobHandler_Raid : MilitaryJobHandler
    {
        public override void OnDeployed(WorldObjectComp_SettlementMilitary milComp, PlanetTile location, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.raidEnemySettlement);
            evt.customDescription = "settlementMilitaryForcesRaiding".Translate(milComp.WorldSettlement.Name, milComp.ReturnMilitaryTarget().Label);
            Settlement target = Find.WorldObjects.SettlementAt(location);
            Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentRaid".Translate(milComp.WorldSettlement.Name, target?.LabelCap ?? (TaggedString)""), LetterDefOf.NeutralEvent);
            evt.DefineEvent(factionfc, milComp.WorldSettlement.Tile, timeToFinish);
        }

        public override BattleResult OnResolved(WorldObjectComp_SettlementMilitary milComp)
        {
            FactionFC faction = FactionCache.FactionComp;

            Settlement target = Find.WorldObjects.SettlementAt(milComp.militaryLocation);
            if (target == null)
            {
                LogUtil.Warning("Military raid target at tile " + milComp.militaryLocation + " no longer exists");
                return new BattleResult();
            }

            BattleResult result = SimulateBattleFc.FightBattle(
                MilitaryForce.CreateMilitaryForceFromSettlement(milComp.WorldSettlement, true),
                MilitaryForce.CreateMilitaryForceFromFaction(milComp.militaryEnemy, false));

            if (result.AttackerVictory)
            {
                faction.AddExperienceToFactionLevel(5f);

                TechLevel tech = target.Faction.def.techLevel;
                int lootLevel;
                bool getSlaves = true;

                switch (tech)
                {
                    case TechLevel.Archotech:
                    case TechLevel.Ultra:
                    case TechLevel.Spacer:
                        lootLevel = 4;
                        break;
                    case TechLevel.Industrial:
                        lootLevel = 3;
                        break;
                    case TechLevel.Medieval:
                    case TechLevel.Neolithic:
                        lootLevel = 2;
                        break;
                    default:
                        lootLevel = 1;
                        break;
                }

                if (target.Faction.def.defName == "Insect")
                {
                    lootLevel = 3;
                    getSlaves = false;
                }

                List<Thing> loot = PaymentUtil.GenerateRaidLoot(lootLevel, tech);

                string text = "settlementDeliveringLoot".Translate();
                text = loot.Aggregate(text, (current, thing) => current + thing.LabelCap + " " + thing.stackCount + "x\n ");

                int num = new IntRange(0, 10).RandomInRange;
                if (num <= 4 && getSlaves)
                {
                    Pawn prisoner = PaymentUtil.GeneratePrisoner(milComp.militaryEnemy);
                    text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), milComp.WorldSettlement.Name);
                    milComp.WorldSettlement.AddPrisoner(prisoner);
                }

                Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                    "RaidEnemySettlementSuccess".Translate(
                        target.LabelCap) + "\n" + text,
                    LetterDefOf.PositiveEvent, new LookTargets(target));

                FCEvent eventParams = new FCEvent()
                {
                    location = Find.AnyPlayerHomeMap.Tile,
                    source = milComp.WorldSettlement.Tile,
                    goods = loot,
                    customDescription = text,
                    timeTillTrigger = Find.TickManager.TicksGame + TravelUtil.ReturnTicksToArrive(milComp.WorldSettlement.Tile, Find.AnyPlayerHomeMap.Tile)
                };

                DeliveryEvent.CreateDeliveryEvent(eventParams);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                    "RaidEnemySettlementFailure".Translate(
                        target.LabelCap), LetterDefOf.NegativeEvent,
                    new LookTargets(target));
            }

            return result;
        }
    }
}
